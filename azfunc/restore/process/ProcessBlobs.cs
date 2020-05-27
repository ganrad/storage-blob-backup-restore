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

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

/**
 * Description:
 * This Azure Function Wrapper restores Azure Storage Block blobs from backup storage account into destination storage
 * account.
 *
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */

namespace azfunc.restore.process
{
    /// <summary>
    /// This class restores blobs from the backup SA to the source/original SA.
    /// </summary>
    class ProcessBlobs
    {
        private readonly IRestoreBackup _restoreBackup;
	private readonly IRestoreTableRepository _restoreTable;

	public ProcessBlobs(IRestoreBackup restoreBackup, IRestoreTableRepository restoreTable)
	{
	   _restoreBackup = restoreBackup;
	   _restoreTable = restoreTable;
	}

	/// <summary>
	/// This function exposes an HTTP end-point
	/// </summary>
	[FunctionName("PerformRestore")]
        public async Task<ActionResult<RestoreReqResponse>> Run(
	   [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "restore/blobs")]
	    HttpRequest req, Microsoft.Extensions.Logging.ILogger log)
        {
	   log.LogInformation($"PerformRestore: Invoked at: {DateTime.Now}");
	   Stopwatch stopWatch = new Stopwatch();
	   stopWatch.Start();

	   RestoreReqResponse reqRespData = await ValidateInput(req);

	   if ( string.IsNullOrEmpty(reqRespData.ExceptionMessage) )
	   {
	      try
	      {
	         // First check if async restore is requested
	         if ( reqRespData.ReqType.Equals(Constants.RESTORE_REQUEST_TYPE_ASYNC) )
	         {
	            reqRespData.Status = Constants.RESTORE_STATUS_ACCEPTED;
		    reqRespData.StatusLocationUri = $"{req.Scheme}://{req.Host}{req.PathBase}/api/restore/";

		    await _restoreTable.InsertRestoreRequest(reqRespData);
	         }
	         else
	         {
                    log.LogInformation($"PerformRestore: Start date : {reqRespData.StDate.ToString("MM/dd/yyyy")}, End date {reqRespData.EnDate.ToString("MM/dd/yyyy")}. Proceeding with restore process ...");

	            if ( ! String.IsNullOrEmpty(reqRespData.ContainerName) )
                       log.LogInformation($"PerformRestore: Container Name : {reqRespData.ContainerName}");

                    await _restoreBackup.Run(reqRespData);
	         };
	         log.LogInformation($"PerformRestore: Completed execution at: {DateTime.Now}");
              }
              catch(Exception ex)
              {
                 log.LogError($"PerformRestore: Exception occurred. Exception: {@ex.ToString()}");
	         reqRespData.ExceptionMessage = $"Encountered exception : {@ex.ToString()}";
	         reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;
              }
	   };

	   stopWatch.Stop();
	   reqRespData.ExecutionTime = getTimeString(stopWatch.Elapsed);

	   return reqRespData;
        }

	private string getTimeString(TimeSpan ts) {
	  return ( String.Format("{0:00}:{1:00}:{2:00}.{3:00}",ts.Hours, ts.Minutes, ts.Seconds,
	    ts.Milliseconds / 10) );
	}

	private async Task<RestoreReqResponse> ValidateInput(HttpRequest req)
	{
	   string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
	   RestoreReqResponse reqRespData = JsonConvert.DeserializeObject<RestoreReqResponse>(requestBody);

           if ( String.IsNullOrEmpty(reqRespData.StartDate) || String.IsNullOrEmpty(reqRespData.EndDate) ) {
	      reqRespData.ExceptionMessage = "Start and End dates are incorrect and/or missing!";
	      reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;

	      return reqRespData;
	   };

           DateTime startDate = DateTime.MinValue;
           DateTime endDate = DateTime.MinValue;

           bool startDateParsed = false;
           bool endDateParsed = false;

           startDateParsed = DateTime.TryParse(reqRespData.StartDate, out startDate);
           endDateParsed = DateTime.TryParse(reqRespData.EndDate, out endDate);

           if (!startDateParsed || !endDateParsed) {
	      reqRespData.ExceptionMessage = $"Unable to parse start and end dates. Provide dates in mm/dd/yyyy format. Start date value {reqRespData.StartDate} End date value {reqRespData.EndDate}. ";
	      reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;

	      return reqRespData;
	   };


           if (startDate > endDate) {
              reqRespData.ExceptionMessage = "Start date cannot be greater than End date.";
	      reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;

	      return reqRespData;
	   };

	   reqRespData.StDate = startDate;
	   reqRespData.EnDate = endDate;

	   if ( ! String.IsNullOrEmpty(reqRespData.BlobName) ) {
	      if ( String.IsNullOrEmpty(reqRespData.ContainerName) ) {
	         reqRespData.ExceptionMessage = $"To restore File : {reqRespData.BlobName}, Container name is required!";
	         reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;

		 return reqRespData;
	      };
	   };

	   if ( ! String.IsNullOrEmpty(reqRespData.ReqType) ) {
	      if ( !reqRespData.ReqType.Equals(Constants.RESTORE_REQUEST_TYPE_SYNC) && 
		   !reqRespData.ReqType.Equals(Constants.RESTORE_REQUEST_TYPE_ASYNC) )
	      {
	         reqRespData.ExceptionMessage = 
	   	   $"Request Type '{reqRespData.ReqType}' is invalid.  Value should be either 'Sync' or 'Async'!";
	         reqRespData.Status = Constants.RESTORE_STATUS_EXCEPTION;
	      };
	   };

	   return reqRespData;
	}
    }
}
