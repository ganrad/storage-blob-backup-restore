// 
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using backup.core.Interfaces;
using backup.core.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/**
 * Description:
 * This class represents a Storage Table repository for the replay log entity. Contains methods for 
 * manipulating entities.
 * 
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Implementations
{
    /// <summary>
    /// Table Repository
    /// https://docs.microsoft.com/en-us/azure/visual-studio/vs-storage-aspnet5-getting-started-tables
    /// </summary>
    public class LogTableRepository : ILogTableRepository
    {
        private readonly ILogger<LogTableRepository> _logger;

        /// <summary>
        /// Table Repository
        /// </summary>
        /// <param name="logger"></param>
        public LogTableRepository(ILogger<LogTableRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get Cloud Table
        /// </summary>
        /// <returns></returns>
        private CloudTable GetCloudTable()
        {
            string _storageAccountConnectionString = 
	      Environment.GetEnvironmentVariable("STORAGE_LOG_TABLE_CONN");

            string _storageTableName = 
	      Environment.GetEnvironmentVariable("STORAGE_LOG_TABLE_NAME");

            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageAccountConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //Get the table reference
            CloudTable table = tableClient.GetTableReference(_storageTableName);

            return table;
        }

        /// <summary>
        /// Returns the blob events from the replay log table based on year, weeknumber, startdate and endDate
        /// </summary>
        /// <param name="weekNumber"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public async Task<List<IEventData>> GetBLOBEvents(int year, int weekNumber, DateTime startDate, DateTime endDate)
        {
            //Get the table reference
            CloudTable replayAuditTable = GetCloudTable();

            var startDateTimeTicks = string.Format("{0:D19}", startDate.Ticks) + "_" + "ID";

            var endDateTimeTicks = string.Format("{0:D19}", endDate.Ticks) + "_" + "ID";

            var whereCondition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, $"{year.ToString()}_{weekNumber.ToString()}");

            var lessThanCondition = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, endDateTimeTicks);

            var greaterThanCondition = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, startDateTimeTicks);

            string query = TableQuery.CombineFilters(whereCondition, TableOperators.And, lessThanCondition);

            query = TableQuery.CombineFilters(query, TableOperators.And, greaterThanCondition);

            TableQuery<EventData> rangeQuery = new TableQuery<EventData>().Where(query);

            List<IEventData> blobEvents = new List<IEventData>();

            // Retrieve entities from the replay log storage table
            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<EventData> resultSegment = await replayAuditTable.ExecuteQuerySegmentedAsync(rangeQuery, token);

                token = resultSegment.ContinuationToken;

                foreach (EventData entity in resultSegment.Results)
                {
                    if (!string.IsNullOrEmpty(entity.DestinationBlobInfoJSON))
                    {
                        entity.DestinationBlobInfo = JsonConvert.DeserializeObject<DestinationBlobInfo>(entity.DestinationBlobInfoJSON);
                    }

                    entity.ReceivedEventData = IBlobEvent.ParseBlobEvent(entity.ReceivedEventDataJSON);

                    blobEvents.Add(entity);
                }

            } while (token != null);

            return blobEvents;

        }

        /// <summary>
        /// Insert Blob Event in replay log table
        /// </summary>
        /// <param name="blobEvent"></param>
        /// <returns></returns>
        public async Task InsertBLOBEvent(IEventData blobEvent)
        {
            CloudTable eventsTable = GetCloudTable();

            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(blobEvent);

            // Execute the insert operation.
            await eventsTable.ExecuteAsync(insertOperation);
        }
    }
}
