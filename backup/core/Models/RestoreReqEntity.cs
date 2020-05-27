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

using Microsoft.Azure.Cosmos.Table;

using System;
using System.Text;

using Newtonsoft.Json;

/**
 * Description:
 * This class represents a Table Entity type for a 'Restore Request'. Captures the details of an async
 * 'Restore' request.
 *
 * Author: GR @Microsoft
 * Dated: 05-25-2020
 *
 * NOTES: Capture updates to the code below.
 */

namespace backup.core.Models
{
    /// <summary>
    /// Event Data
    /// </summary>
    public class RestoreReqEntity : IRestoreReqEntity
    {
        public RestoreReqEntity()
        {
        }

	/// <summary>
	/// Status of the restore request
	/// </summary>
	public string CurrentStatus { get; set; }

        /// <summary>
        /// Restore async request
        /// </summary>
        public RestoreReqResponse RestoreReqRespData { get; set; }

        /// <summary>
        /// Restore async request JSON
        /// </summary>
        public string RestoreReqRespDataJSON { get; set; }

        /// <summary>
        /// Restore request constructor
        /// </summary>
        /// <param name="restoreRequest"></param>
        public RestoreReqEntity(RestoreReqResponse restoreRequest)
        {
           EventDateDetails dateDetails = new EventDateDetails(DateTime.Now);

           base.PartitionKey = $"{dateDetails.year}_{dateDetails.WeekNumber}";
           base.RowKey = Guid.NewGuid().ToString();

	   restoreRequest.StatusLocationUri += $"{base.PartitionKey}/{base.RowKey}";

	   CurrentStatus = restoreRequest.Status;
           RestoreReqRespDataJSON = JsonConvert.SerializeObject(restoreRequest);
        }
    }
}
