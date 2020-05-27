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
using backup.core.Constants;
using backup.core.Implementations;
using backup.core.Interfaces;
using backup.core.Models;
using backup.core.Utilities;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

/**
 * Description:
 * This Function runs the restore process (asynchronously) and is triggered by a Timer event.  Every time this Function
 * is triggered, mesages are read from an Azure Storage Table. The message contains the details for restoring blobs 
 * from a backup SA/Container to a target SA/Container over a given time period. The Function restores blobs based on 
 * the details found in the message.
 * 
 * Author: GR @Microsoft
 * Dated: 05-27-2020
 *
 * NOTES: Capture updates to the code below.
 */

namespace azfunc.restore.process
{
    /// <summary>
    /// This class restore blobs from backup SA to an target/destination SA.
    /// </summary>
    class ProcessBlobsAsync
    {
	private readonly IRestoreBackup _restoreBackup;
	private readonly IRestoreTableRepository _restoreTable;
        
	public ProcessBlobsAsync(IRestoreBackup restoreBackup, IRestoreTableRepository restoreTable)
	{
	    _restoreBackup = restoreBackup;
	    _restoreTable = restoreTable;
	}

	/// <summary>
	/// This function is invoked by a Timer Trigger
	/// </summary>
	[FunctionName("PerformRestoreAsync")]
	public async Task Run(
	  [TimerTrigger("*/10 * * * * *")] TimerInfo callTimer,  // Fire every 10 seconds ...
	   Microsoft.Extensions.Logging.ILogger log)
	{
	    if ( callTimer.IsPastDue )
	       log.LogInformation("PerformBackup: Timer is running late!");

	    log.LogInformation($"PerformRestoreAsync: Invoked at: {DateTime.Now}");
	    Stopwatch stopWatch = new Stopwatch();
	    RestoreReqResponse reqRespData = null;
            try
            {
	      // Fetch an asynchronous restore request
	      reqRespData = await _restoreTable.GetRestoreRequest();
	      if ( reqRespData != null ) 
	      {
	         stopWatch.Start();

		 // Update the status of the restore request to in-progress
		 reqRespData.Status = Constants.RESTORE_STATUS_INPROCESS;
		 reqRespData.StartTime = DateTime.Now.ToString();
	  	 await _restoreTable.UpdateRestoreRequest(reqRespData);
		 
		 // Execute the restore process
		 DateTime startDate = DateTime.MinValue;
		 DateTime endDate = DateTime.MinValue;
		 DateTime.TryParse(reqRespData.StartDate, out startDate);
		 DateTime.TryParse(reqRespData.EndDate, out endDate);
		 reqRespData.StDate = startDate;
		 reqRespData.EnDate = endDate;
		 log.LogInformation($"PerformRestore: Start date : {reqRespData.StDate.ToString("MM/dd/yyyy")}, End date {reqRespData.EnDate.ToString("MM/dd/yyyy")}. Proceeding with restore process ...");
		 if ( ! String.IsNullOrEmpty(reqRespData.ContainerName) )
		    log.LogInformation($"PerformRestore: Container Name : {reqRespData.ContainerName}");
                 await _restoreBackup.Run(reqRespData);
		 
		 // Update the status of the restore request to completed
		 reqRespData.Status = Constants.RESTORE_STATUS_COMPLETED;
		 reqRespData.EndTime = DateTime.Now.ToString();
		 stopWatch.Stop();
		 reqRespData.ExecutionTime = DateTimeUtil.getTimeString(stopWatch.Elapsed);
	  	 await _restoreTable.UpdateRestoreRequest(reqRespData);
	       }
            }
            catch(Exception ex)
            {
                log.LogError($"PerformRestoreAsync: Exception occurred while processing message. Exception: {@ex.ToString()}");
		// Update the status of the restore request to exception
		if ( reqRespData != null )
		{
		   reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;
		   reqRespData.ExceptionMessage = $"PerformRestoreAsync: Encountered Exception: {@ex.ToString()}";
		   reqRespData.EndTime = DateTime.Now.ToString();
		   stopWatch.Stop();
		   reqRespData.ExecutionTime = DateTimeUtil.getTimeString(stopWatch.Elapsed);

	  	   await _restoreTable.UpdateRestoreRequest(reqRespData);
		};
            }
	    stopWatch = null;
	    log.LogInformation($"PerformRestoreAsync: Completed execution at: {DateTime.Now}");
	}
    }
}
