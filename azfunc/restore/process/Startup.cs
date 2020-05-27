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
using backup.core.Implementations;

using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.IO;

/**
 * Description:
 * This class configures and loads the Function's service dependencies into the IoC container.
 * 
 * Author: GR @Microsoft
 * Dated: 05-23-2020
 *
 * NOTES: Capture updates to the code below.
 */

[assembly: FunctionsStartup(typeof(azfunc.restore.process.Startup))]

namespace azfunc.restore.process
{
    public class Startup : FunctionsStartup
    {
       public override void Configure(IFunctionsHostBuilder builder)
       {
          // Add services
          builder.Services.AddTransient<IRestoreTableRepository, RestoreTableRepository>();
          builder.Services.AddTransient<ILogTableRepository, LogTableRepository>();
          builder.Services.AddTransient<IBlobRepository, BlobRepository>();
          builder.Services.AddTransient<IRestoreBackup, RestoreBackupWorker>();
       }
    }
}
