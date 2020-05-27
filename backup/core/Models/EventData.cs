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

using Microsoft.Azure.Cosmos.Table;

using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

/**
 * Description:
 * This class represents a container which is used during the storage account backup and restore process.
 *
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */

namespace backup.core.Models
{
    /// <summary>
    /// Event Data
    /// </summary>
    public class EventData: IEventData
    {
        public EventData()
        {
        }

        /// <summary>
        /// Event recieved from storage Queue
        /// </summary>
        public IBlobEvent ReceivedEventData { get; set; }

        /// <summary>
        /// Destination Blob Info. Populated only in case of CREATE
        /// </summary>
        public DestinationBlobInfo DestinationBlobInfo { get; set; }

        /// <summary>
        /// DestinationBlobInfoJSON
        /// Destination Blob Info. Populated only in case of CREATE
        /// </summary>
        public string DestinationBlobInfoJSON { get; set; }

        /// <summary>
        /// ReceivedEventDataJSON
        /// Destination Blob Info. Populated for CREATE and DELETE events
        /// </summary>
        public string ReceivedEventDataJSON { get; set; }

        /// <summary>
        /// Event Data Constructor
        /// </summary>
        /// <param name="eventData"></param>
        public EventData(string eventData)
        {
            string eventId;

            DateTime eventDateTime;

            ReceivedEventData = IBlobEvent.ParseBlobEvent(eventData, out eventId, out eventDateTime);

            if (ReceivedEventData != null)
            {
                ReceivedEventDataJSON = JsonConvert.SerializeObject(ReceivedEventData);

                // string partitionKey = string.Empty;

                // string rowKey = string.Empty;

                EventDateDetails dateDetails = new EventDateDetails(eventDateTime);

                base.PartitionKey = $"{dateDetails.year}_{dateDetails.WeekNumber}";

                base.RowKey = $"{dateDetails.formattedDate}_{eventId.Replace("-", "")}";

            }
        }
    }
}
