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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/**
 * Description:
 * This class represents a Storage Queue repository and contains methods for manipulating queue messages.
 * 
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Implementations
{
    /// <summary>
    /// Storage Queue Repository
    /// </summary>
    public class StorageQueueRepository: IStorageQueueRepository
    {
        private readonly ILogger<StorageQueueRepository> _logger;

        /// <summary>
        /// StorageQueueRepository
        /// </summary>
        /// <param name="logger">An ILogger</param>
        public StorageQueueRepository(ILogger<StorageQueueRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// GetCloudQueue
        /// </summary>
        /// <returns>Returns a CloudQueue</returns>
        private CloudQueue GetCloudQueue()
        {
            string _storageAccountConnectionString =
		Environment.GetEnvironmentVariable("EVT_STORAGE_ACCOUNT_CONN");

            string _storageQueueName = Environment.GetEnvironmentVariable("EVENT_QUEUE_NAME");

            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageAccountConnectionString);

            // Create the table client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            //Get the table reference
            CloudQueue queue = queueClient.GetQueueReference(_storageQueueName);

            return queue;
        }

        /// <summary>
        /// Returns an enumerable list of blob events
        /// </summary>
        /// <returns>Returns an IEnumerable<CloudQueueMessage></returns>
        public async Task<IEnumerable<CloudQueueMessage>> GetBLOBEvents()
        {
            int queueVisibilityTimeOutInMS = 
	      int.Parse(Environment.GetEnvironmentVariable("QueueVisibilityTimeOutInMS"));

            int queueMessageCountToRead = 
	      int.Parse(Environment.GetEnvironmentVariable("QueueMessageCountToRead"));

            CloudQueue queue = GetCloudQueue();

            QueueRequestOptions options = new QueueRequestOptions();

            return await queue.GetMessagesAsync(queueMessageCountToRead, new TimeSpan(0, 0, 0, 0, queueVisibilityTimeOutInMS),null,null);
        }

        /// <summary>
        /// Deletes a blob event from the storage queue
        /// </summary>
        /// <param name="message">A Queue message to be deleted</param>
        public async Task DeleteBLOBEventAsync(CloudQueueMessage message)
        {
            CloudQueue queue = GetCloudQueue();

            await queue.DeleteMessageAsync(message);

        }
    }
}
