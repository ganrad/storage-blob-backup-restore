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
 * This class represents a Storage Table repository for asynchronous restore requests. Contains methods for 
 * manipulating entities in the restore request storage table.
 * 
 * Author: GR @Microsoft
 * Dated: 05-26-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Implementations
{
    /// <summary>
    /// Table Repository
    /// https://docs.microsoft.com/en-us/azure/visual-studio/vs-storage-aspnet5-getting-started-tables
    /// </summary>
    public class RestoreTableRepository : IRestoreTableRepository
    {
        private readonly ILogger<TableRepository> _logger;

        /// <summary>
        /// Restore Table Repository
        /// </summary>
        /// <param name="logger"></param>
        public RestoreTableRepository(ILogger<TableRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get Azure Storage Table reference
        /// </summary>
        /// <returns></returns>
        private CloudTable GetCloudTable()
        {
            string _storageAccountConnectionString = 
	      Environment.GetEnvironmentVariable("STORAGE_RESTORE_TABLE_CONN");

            string _storageTableName = 
	      Environment.GetEnvironmentVariable("STORAGE_RESTORE_TABLE_NAME");

            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageAccountConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //Get the table reference
            CloudTable table = tableClient.GetTableReference(_storageTableName);

            return table;
        }

        /// <summary>
        /// Insert restore request entity in the storage table
        /// </summary>
        /// <param name="restoreRequest"></param>
        /// <returns></returns>
        public async Task InsertRestoreRequest(RestoreReqResponse restoreRequest)
        {
            CloudTable restoreReqTable = GetCloudTable();

	    IRestoreReqEntity restoreEntity = new RestoreReqEntity(restoreRequest);

            // Create the TableOperation object that inserts the restore request entity.
            TableOperation insertOperation = TableOperation.Insert(restoreEntity);

            // Execute the insert operation.
            await restoreReqTable.ExecuteAsync(insertOperation);
        }
    }
}
