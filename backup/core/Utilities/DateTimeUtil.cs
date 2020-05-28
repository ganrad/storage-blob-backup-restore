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

/**
 * Description:
 * This class contains misc. utility functions.
 * 
 * Author: Ganesh Radhakrishnan @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace backup.core.Utilities
{
    /// <summary>Class containing methods to manipulate Date and Times</summary>
    public static class DateTimeUtil
    {
	private static readonly DateTime Jan1St1970 = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Get extra long current timestamp</summary>
	public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }	

        /// <summary>Get formatted date time string</summary>
	public static String GetString { get { return DateTime.Now.ToString("MM-dd-yyyy@HH:mm:ss.fff"); } }	

        /// <summary>Get date time string</summary>
	public static String getTimeString(TimeSpan ts) {
	  return ( String.Format("{0:00}:{1:00}:{2:00}.{3:00}",ts.Hours,ts.Minutes,ts.Seconds,ts.Milliseconds / 10) );
	}
    }
}
