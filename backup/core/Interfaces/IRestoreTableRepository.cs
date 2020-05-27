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
using backup.core.Models;

using System;
using System.Threading.Tasks;

/**
 * Description:
 * This interface defines operations that can be performed on the 'Restore request' Azure Storage Table.
 * 
 * Author: GR @Microsoft
 * Dated: 05-26-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Interfaces
{
    /// <summary>
    /// Repository to manipulate restore requests/jobs stored in an Azure Storage Table.
    /// </summary>
    public interface IRestoreTableRepository
    {
        /// <summary>
        /// Inserts an async restore request/job in Az storage table
        /// </summary>
        /// <param name="restoreRequest"></param>
        /// <returns></returns>
	public Task InsertRestoreRequest(RestoreReqResponse restoreRequest);

	/// <summary>
	/// Retrieves the details of an async restore request from the storage table
	/// </summary>
	/// <param name="datestr"></param>
	/// <param name="guid"></param>
	/// <returns>RestoreReqResponse</returns>
	public RestoreReqResponse GetRestoreReqDetails(string datestr, string guid);

	/// <summary>
	/// Retrieves an async restore request for processing
	/// </summary>
	/// <returns>RestoreReqResponse</returns>
	public Task<RestoreReqResponse> GetRestoreRequest();
	
        /// <summary>
        /// Updates a restore request/job in Az storage table
        /// </summary>
        /// <param name="restoreRequest"></param>
        /// <returns></returns>
	public Task UpdateRestoreRequest(RestoreReqResponse restoreRequest);
    }
}
