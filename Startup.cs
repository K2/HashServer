// Copyright(C) 2017 Shane Macaulay smacaulay@gmail.com
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using ProtoBuf;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration.Json;
using System.Threading;

namespace HashServer
{
    public class Startup
    {
        private const int _chunkSize = 4096;
        private const int _defaultNumChunks = 16;
        private static byte[] _chunk = Encoding.UTF8.GetBytes(new string('a', _chunkSize));
        static string GoldenState = "GoldState.buf";

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("Default");
            Program.log = logger;

            logger.Log<string>(LogLevel.Information, new EventId(0, "Configure"), "Booting up", null, (state, ex) => { return $"{state}"; });
            List<string> gRoots = new List<string>();
            
            var gi = new GoldImages(logger);

            // figuer out a default locatedb if one does not exist
            var locateDb = File.Exists(Program.Settings.Host.FileLocateNfo) ? Program.Settings.Host.FileLocateNfo : GoldenState;

            if (File.Exists(locateDb))
            {
                using (var SerData = File.OpenRead(locateDb))
                {
                    logger.LogInformation($"Serialed data found {locateDb} for golden image locate database, will skip filesystem scan");
                    GoldImages.DiskFiles = Serializer.Deserialize<ConcurrentDictionary<string, ConcurrentBag<Tuple<uint, uint, string>>>>(SerData);
                    GoldImages.AtLeastOneGoldImageSetIndexed = true;
                    logger.LogInformation($"{GoldImages.DiskFiles.Count} files have been located from the configured inputs, to regenerate, delete the {locateDb} and restart.");
                    if (GoldImages.DiskFiles.Count < 1024)
                        logger.LogWarning($"Only {GoldImages.DiskFiles.Count} files found, this seems low, try adding more folders to the config file. Or delete the {locateDb} file so it can be re-generated.");
                }
            } else if(Program.Settings.GoldSourceFiles != null && Program.Settings.GoldSourceFiles.Images != null && Program.Settings.GoldSourceFiles.Images.Length > 0)
            {
                foreach(var imageSet in Program.Settings.GoldSourceFiles.Images)
                {
                    logger.LogInformation($"Compiling gold locate db from {imageSet.ROOT} {imageSet.OS}, server will continue after filesystem scan.");
                    if(!Directory.Exists(imageSet.ROOT))
                    {
                        logger.LogCritical($"Unable to handle configured gold image path {imageSet.ROOT} skipping.");
                        continue;
                    }
                    gi.Init(new string[] { imageSet.ROOT });
                }
            }
            else
            // by default we'll use the local C:\ as the golden image, it's not very optimal since 
            // many files will be inaccessable due to permissions and in-use
            {
                logger.LogInformation("No specified path found to use as 'golden' images.  Using C:");
                gi.Init(new string[] { "c:\\" });
            }

            if(!GoldImages.AtLeastOneGoldImageSetIndexed)
            {
                logger.LogCritical($"Fatal state!!! No golden images were able to be loaded so this server has no work to do, exiting in 5 seconds.");
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            try
            {
                app.Run(async (context) =>
                {
                    // pull in data from POST

                    var connectionFeature = context.Connection;
                    logger.Log<string>(LogLevel.Information, new EventId(1, "IP"), $"Peer: {connectionFeature.RemoteIpAddress?.ToString()}:{connectionFeature.RemotePort}"
                        + $"{Environment.NewLine}"
                        + $"{context.Request.Path}{Environment.NewLine}", null, (state, ex) => $"{state}");

                    var request = context.Request;
                    var response = context.Response;

                    await PageHash.Run(context, "x", logger).ConfigureAwait(false);
                    return;
                });
            }
            finally
            {
                var saveFile = Program.Settings.Host.FileLocateNfo;
                if (string.IsNullOrWhiteSpace(saveFile))
                    saveFile = GoldenState;

                Program.Settings.Host.FileLocateNfo = GoldenState;

                logger.LogInformation($"Saving locate database to {saveFile}");
                using (var serOut = File.OpenWrite(saveFile))
                    Serializer.Serialize<ConcurrentDictionary<string, ConcurrentBag<Tuple<uint, uint, string>>>>(serOut, GoldImages.DiskFiles);
            }
        }
    }
}