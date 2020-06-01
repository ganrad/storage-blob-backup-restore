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
using backup.core.Constants;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Queue;

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

/**
 * Description:
 * This class represents a task that copies storage blobs from the backup SA to the source/restore SA
 * SA ~ Azure Storage Account
 * 
 * Author: Ganesh Radhakrishnan @Microsoft
 * Dated: 05-06-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Implementations
{
    /// <summary>
    /// Restore Backup Worker
    /// </summary>
    public class RestoreBackupWorker : IRestoreBackup
    {
        private readonly ILogger<RestoreBackupWorker> _logger;

        private readonly ILogTableRepository _logRepository;

        private readonly IBlobRepository _blobRepository;

	private readonly IRestoreTableRepository _restoreTblRepository;

	private int _updateFrequencyCount;

        /// <summary>
        /// Storage back up
        /// </summary>
        /// <param name="logger"></param>
        public RestoreBackupWorker(
            ILogger<RestoreBackupWorker> logger, 
            ILogTableRepository logRepository,
            IBlobRepository blobRepository,
	    IRestoreTableRepository restoreTblRepository)
        {
            _logger = logger;

            _logRepository = logRepository;

            _blobRepository = blobRepository;

            _restoreTblRepository = restoreTblRepository;

	    _updateFrequencyCount =
	       string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UpdateFrequencyCount")) ? 
	       Constants.Constants.RESTORE_UPDATE_FREQ_COUNT :
	       int.Parse(Environment.GetEnvironmentVariable("UpdateFrequencyCount"));
        }

        /// <summary>
        /// Run method
        /// 1: Reads messages from the table storage in ascending order.
        /// 2: Executes the delete or create operation on the destination blob.
        /// </summary>
        /// <returns></returns>
        public async Task Run(RestoreReqResponse reqResponse) 
        {
            _logger.LogDebug("Begin: RestoreBackupWorker.Run");

            //Since there can be many records around 84K for a day, let's read the records day by day and perform the restore operation

            List<Tuple<int,int, DateTime>> dates = GetDatesForDateRange(reqResponse.StDate, reqResponse.EnDate);

            _logger.LogInformation($"Number of dates determined.---{dates.Count()}");

            int totalSuccessCount = 0;

            int totalFailureCount = 0;

            foreach (Tuple<int,int, DateTime> dateData in dates)
            {
                _logger.LogInformation($"Starting restore for Year {dateData.Item1} Week {dateData.Item2} and Date {dateData.Item3.ToString("MM/dd/yyyy")}");

                var blobEvents = await _logRepository.GetBLOBEvents(dateData.Item1, dateData.Item2, dateData.Item3, dateData.Item3.AddDays(1));

                _logger.LogInformation($"Found {blobEvents.Count} for {dateData.Item1} and Date {dateData.Item3.ToString("MM/dd/yyyy")}");

                if (blobEvents != null && blobEvents.Count > 0)
                {
                    foreach(EventData eventData in blobEvents)
                    {
                        try
                        {
                            if (eventData.ReceivedEventData is BlobEvent<CreatedEventData>)
                            {
                                BlobEvent<CreatedEventData> createdBlob = (BlobEvent<CreatedEventData>)eventData.ReceivedEventData;

                                if (eventData.DestinationBlobInfo != null)
                                {
				    if ( (! String.IsNullOrEmpty(reqResponse.ContainerName)) && (! String.Equals(eventData.DestinationBlobInfo.OrgContainerName, reqResponse.ContainerName)) )
				       continue;

				    if ( (reqResponse.BlobNames != null) && (! reqResponse.BlobNames.Contains(eventData.DestinationBlobInfo.OrgBlobName)) )
				       continue;
				    
                                    _logger.LogInformation($"Going to perform copy as it is a created event {createdBlob.data.url}");
                                    await _blobRepository.CopyBlobFromBackupToRestore(eventData.DestinationBlobInfo);
                                }
                                else
                                {
                                    _logger.LogInformation($"Copy of the blob will be ignored as at the time of backup the blob was not present at source. One of the cause can be , a delete has been performed already on this blob. {createdBlob.data.url}");
				    continue;
                                }
                            }
                            else if (eventData.ReceivedEventData is BlobEvent<DeletedEventData>)
                            {
                                BlobEvent<DeletedEventData> deletedBlob = (BlobEvent<DeletedEventData>)eventData.ReceivedEventData;

				if ( reqResponse.SkipDeletes.ToUpper(new CultureInfo("en-US",false)).Equals(Constants.Constants.RESTORE_SKIP_DELETES_YES) )
				   continue;

				if ( (! String.IsNullOrEmpty(reqResponse.ContainerName)) && (! deletedBlob.data.url.Contains(reqResponse.ContainerName)) )
				   continue;

				if ( (reqResponse.BlobNames != null) && (! reqResponse.BlobNames.Exists(x => deletedBlob.data.url.Contains(x)) ) )
				   continue;
				
                                _logger.LogInformation($"Going to perform delete as it is a deleted event {deletedBlob.data.url}");
                                await _blobRepository.DeleteBlobFromRestore(eventData.ReceivedEventData);
                            }
                            else
                            {
                                _logger.LogInformation($"Currently only Created and Deleted events are supported. Event Data: {eventData.ReceivedEventDataJSON}");
				continue;
                            }

                            totalSuccessCount++;
                        }
                        catch(Exception ex)
                        {
                            totalFailureCount++;
                            _logger.LogError($"Exception while restoring event {eventData.ReceivedEventDataJSON}. Exception {ex.ToString()}");
                        }
		       if ( reqResponse.ReqType.Equals(Constants.Constants.RESTORE_REQUEST_TYPE_ASYNC) && ( totalSuccessCount % _updateFrequencyCount == 0 ) )
		       {
	    	          reqResponse.TotalSuccessCount = totalSuccessCount;
	    	          reqResponse.TotalFailureCount = totalFailureCount;
		          await _restoreTblRepository.UpdateRestoreRequest(reqResponse);
		       };
                    };
		    // Update the processed record count for async restore request
                }
            }; // End of outer For loop

            _logger.LogInformation($"Restore Success records count: {totalSuccessCount}. Restore Failure records count: {totalFailureCount}.");
	    reqResponse.TotalSuccessCount = totalSuccessCount;
	    reqResponse.TotalFailureCount = totalFailureCount;

            _logger.LogDebug("End: RestoreBackupWorker.Run");
        }


        /// <summary>
        /// Returns all the dates between date range and with the corresponding week number.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="EndDate"></param>
        /// <returns></returns>
        public List<Tuple<int,int,DateTime>> GetDatesForDateRange(DateTime startDate, DateTime EndDate)
        {
            List<Tuple<int,int, DateTime>> dates = new List<Tuple<int,int, DateTime>>();

            Tuple<int,int, DateTime> dateData;

            EventDateDetails dateDetails;

            while (startDate <= EndDate)
            {
                dateDetails = new EventDateDetails(startDate);

                dateData = new Tuple<int,int, DateTime>(dateDetails.year, dateDetails.WeekNumber, startDate);

                dates.Add(dateData);

                startDate = startDate.AddDays(1);
            }

            return dates;
        }
    }
}
