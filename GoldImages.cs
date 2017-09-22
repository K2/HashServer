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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Threading;
using System.Collections.Concurrent;
using Reloc;
using Microsoft.Extensions.Logging;
using Monitor.Core.Utilities;
using ProtoBuf;

namespace HashServer
{
    [ProtoContract(AsReferenceDefault = true, ImplicitFields = ImplicitFields.None)]
    public class GoldImages
    {
        [ProtoMember(1)]
        public static ConcurrentDictionary<string, ConcurrentBag<Tuple<uint, uint, string>>> DiskFiles;
        public static ILogger Log;

        static string CurrFolderRoot;

        public GoldImages() {
            DiskFiles = new ConcurrentDictionary<string, ConcurrentBag<Tuple<uint, uint, string>>>();
        }
        public GoldImages(ILogger log) : this()
        {
            Log = log;
            Log = log;
            Log.Log<string>(LogLevel.Information, new EventId(0, "ImageLoad"), "Initializing gold image files", null, (state, ex) => { return $"{state}"; });
        }

        public GoldImages(string[] Folders, ILogger log) :this(log)
        {
            Init(Folders);
        }

        public void Init(string[] Folders)
        {
            foreach (var folder in Folders)
            {
                CurrFolderRoot = Path.GetPathRoot(folder);
                Recursive(folder);
            }
            Log.Log<string>(LogLevel.Information, new EventId(1, "ImageLoad"), $"Done gold image files, count is {DiskFiles.Count}", null, (state, ex) => { return $"{state}"; });
        }

        static void Recursive(string folder)
        {
            Extract e = null;
            FileInfo finfo = null;

            var buff = new byte[4096];
            IEnumerable<string> files = null;

            if(Path.GetPathRoot(folder) != CurrFolderRoot) {
                Log.Log<string>(LogLevel.Warning, new EventId(102, "ImageLoad"), $"Skipping folder {folder} we moved out of the specified ROOT {CurrFolderRoot}", null, (state, x) => { return $"{state}"; });
                return;
            }

            if (DiskFiles.Count > 0 && (DiskFiles.Count % 100) == 0)
                Log.Log<string>(LogLevel.Information, new EventId(10, "ImageLoad status."), $"Gold image files, count is {DiskFiles.Count}", null, (state, ex) => { return $"{state}"; });

            try {
                files = from afile in Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                        select afile;

            } catch (Exception ex) {
                Log.Log<string>(LogLevel.Warning, new EventId(101, "ImageLoad"), $"Skipping folder {folder} {ex.Message}", ex, (state, x) => { return $"{state}"; });
            }

            if (files != null)
            {
                // get list of PE's we can hash
                foreach (var file in files)
                {
                    try
                    {
                        finfo = new FileInfo(file);
                        using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                            fs.Read(buff, 0, buff.Length);

                        e = Extract.IsBlockaPE(buff);
                        if (e == null) continue;

                    }
                    catch (Exception ex)
                    {
                        Log.Log<string>(LogLevel.Warning, new EventId(100, "ImageLoad"), $"Skipping file {finfo.FullName} {ex.Message}", ex, (state, x) => { return $"{state}"; });
                    }

                    if (e == null) continue;

                    var bag = new ConcurrentBag<Tuple<uint, uint, string>>();
                    bag.Add(Tuple.Create<uint, uint, string>(e.SizeOfImage, e.TimeStamp, file));

                    DiskFiles.AddOrUpdate(Path.GetFileName(file).ToLower(), bag, (key, oldvalue) => { oldvalue.Add(Tuple.Create<uint, uint, string>(e.SizeOfImage, e.TimeStamp, file)); return oldvalue; });
                }
            }

            // Parse subdirectories
            foreach (var subdir in Directory.EnumerateDirectories(folder, "*.*", SearchOption.TopDirectoryOnly))
            {
                try {
                    if (!JunctionPoint.Exists(subdir))
                        Recursive(subdir);

                } catch (Exception ex) {
                    Log.LogWarning(1, $"Problem with scanning folder: {subdir}", ex.Message);
                }
            }
        }
    }
}
