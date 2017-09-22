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
            logger.Log<string>(LogLevel.Debug, new EventId(0, "Configure"), "Booting up", null, (state, ex) => { return $"{state}"; });
            List<string> gRoots = new List<string>();
            
            var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var config = configBuilder.Build();
            config.GetSection("GoldRoots");

            foreach (var root in config.GetChildren())
                gRoots.Add(root.Value);

            var gi = new GoldImages(logger);

            if(File.Exists(GoldenState))
                using (var SerData = File.OpenRead(GoldenState))
                    GoldImages.DiskFiles = Serializer.Deserialize<ConcurrentDictionary<string, ConcurrentBag<Tuple<uint, uint, string>>>>(SerData);
            else 
                 gi.Init(new string[] { "T:\\" });


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
                using (var serOut = File.OpenWrite(GoldenState))
                    Serializer.Serialize<ConcurrentDictionary<string, ConcurrentBag<Tuple<uint, uint, string>>>>(serOut, GoldImages.DiskFiles);
            }
        }
    }
}