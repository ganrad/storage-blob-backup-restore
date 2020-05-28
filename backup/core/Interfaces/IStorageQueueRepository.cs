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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using backup.core.Models;

using Microsoft.Azure.Storage.Queue;

/**
 * Description:
 * This interface defines operations that can be performed on messages within an Azure Storage Queue.
 * 
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Interfaces
{
    public interface IStorageQueueRepository
    {
        /// <summary>
        /// Returns the BLOB Events
        /// </summary>
        /// <returns>A enumerable list of queue messages</returns>
        Task<IEnumerable<CloudQueueMessage>> GetBLOBEvents();

        /// <summary>
        /// Deletes the BLOB Event from the underlying storage queue
        /// </summary>
	/// <param name="message">A queue message to be deleted</param>
        Task DeleteBLOBEventAsync(CloudQueueMessage message);
    }
}
