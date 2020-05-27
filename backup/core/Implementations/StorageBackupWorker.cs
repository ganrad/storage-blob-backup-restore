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
using Microsoft.Azure.Storage.Queue;

using System;
using System.Threading.Tasks;
using System.Linq;

using Newtonsoft.Json;

/**
 * Description:
 * This class represents a task which copies storage blobs from the source SA to the backup SA
 * SA ~ Azure Storage Account.
 * 
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Implementations
{
    /// <summary>
    /// Storage Backup
    /// </summary>
    public class StorageBackupWorker : IStorageBackup
    {
        private readonly ILogger<StorageBackupWorker> _logger;

        private readonly IStorageQueueRepository _queueRepository;

        private readonly ILogTableRepository _logRepository;

        private readonly IBlobRepository _blobRepository;

        /// <summary>
        /// Storage back up
        /// </summary>
        /// <param name="logger"></param>
        public StorageBackupWorker(
            ILogger<StorageBackupWorker> logger, 
            IStorageQueueRepository queueRepository, 
            ILogTableRepository logRepository,
            IBlobRepository blobRepository)
        {
            _logger = logger;

            _queueRepository = queueRepository;

            _logRepository = logRepository;

            _blobRepository = blobRepository;
        }

        /// <summary>
        /// Run method
        /// 1: Reads the messgaes from the queue.
        /// 2: Stores the messages to the table storage.
        /// 3: Deletes the messages from the queue
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            _logger.LogDebug("Begin: StorageBackup.Run");

            var blobEvents = await _queueRepository.GetBLOBEvents();

            _logger.LogInformation($"Number of messages found in queue.---{blobEvents.Count()}");

            EventData eventData = null;

            string eventString = string.Empty;

            foreach (CloudQueueMessage blobEvent in blobEvents)
            {
                try
                {
                    eventString = blobEvent.AsString;

                    eventData = new EventData(eventString);

                    if (eventData.ReceivedEventData != null)
                    {
                        //In case file has been added, copy the file from source storage to destination storage
                        if (eventData.ReceivedEventData is BlobEvent<CreatedEventData>)
                        {
                            _logger.LogDebug($"Going to write to blob---{@eventString}");

                            DestinationBlobInfo destinationBlobInfo = await _blobRepository.CopyBlobFromSourceToBackup(eventData.ReceivedEventData);

                            eventData.DestinationBlobInfo = destinationBlobInfo;
                            if (eventData.DestinationBlobInfo == null)
                                _logger.LogDebug($"DestinationBlobInfo is null. File not copied---{@eventString}");
			    else
                                eventData.DestinationBlobInfoJSON  = JsonConvert.SerializeObject(destinationBlobInfo);
                        }
                        else
                            _logger.LogDebug($"Skipping copying blob as it is not blob created event.---{@eventString}");
                        _logger.LogDebug($"Going to insert to storage---{@eventString}");

                        await _logRepository.InsertBLOBEvent(eventData);

                        _logger.LogDebug($"Going to delete message from queue---{@eventString}");

                        //delete the message from queue only after insert in replay log table succeeds
                        await _queueRepository.DeleteBLOBEventAsync(blobEvent);
                        
                    }
                    else
                    {
                        _logger.LogDebug($"EventData.RecievedEventData is null. Currently the utility understands Created and Deleted Events only.---{@eventString}");
                    }
                }catch (Exception ex)
                {
                    _logger.LogError($"Error while inserting to storage repository for this event. Event should come back to queue. Exception: {@ex.ToString()} | Event Data: {@eventString}");
                }
            }

            _logger.LogDebug("End: StorageBackup.Run");
        }
    }
}
