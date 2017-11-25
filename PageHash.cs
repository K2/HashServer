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
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using Reloc;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Dia2Sharp;

namespace HashServer
{
    public class MemPageHash
    {
        public string HdrHash;
        public uint TimeDateStamp;
        public long AllocationBase;
        public long BaseAddress;
        public long Size;
        public uint ImageSize;
        public int Id;
        public string ProcessName;
        public string ModuleName;
        public int SharedAway;
        public int HashedBlocks;
        public HashSet<PageHashBlock> HashSet = new HashSet<PageHashBlock>();
    }
    public class PageHashBlock
    {
        public long Address;
        public string Hash;
    }
    public class PageHashBlockResult
    {
        public long Address;
        public bool HashCheckEquivalant;

        public override string ToString() => $"Address:{Address:x} - Matched:{HashCheckEquivalant}";
    }


    public static class PageHash
    {
        static SHA256 hasher = SHA256.Create();

        static byte[] nullBuff = new byte[4096];

        public static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        static void AttemptDelocate(byte[] block, Extract e, ulong Delta, long SecOffset, ulong OrigBase, DeLocate Relocs)
        {
            // were we able to get all the details we need to DeLocate (ReReLocate)?
            // This should be moved into some global cache so each Process can share the info for shared modules etc..
            if (SecOffset == 0)
            {
                Program.log.LogTrace($"Attempting Header delocation {e} Relocation Delta {Delta:x} ");
                DeLocate.DelocateHeader(block, OrigBase, (int)e.ImageBaseOffset, e.Is64);

                for (int i = e.CheckSumPos; i < e.CheckSumPos + 4; i++)
                {
                    Program.log.LogTrace($"Ignoring checksum {Convert.ToInt32(block[i]):x} ");
                    block[i] = 0;
                }

                // I hate how bound imports sit's in the text section it's not code!
                int BoundImportsOffset = e.Directories[11].Item1;
                if (BoundImportsOffset > 0 && BoundImportsOffset < 0x1000)
                {
                    bool KeepGoing = true;
                    short curr = 0, offoff = 0;
                    do
                    {
                        offoff += 4;
                        curr = BitConverter.ToInt16(block, BoundImportsOffset + offoff);
                        if (curr == 0)
                            KeepGoing = false;
                        e.BoundImprotLen += curr;
                        offoff += 4;

                    } while (KeepGoing);
                }
                if (e.BoundImprotLen > 0x400)
                    e.BoundImprotLen = 0x3FC;

                Program.log.LogTrace($"Ignoring bound imports length {e.BoundImprotLen}");

                foreach (var dirEntry in e.Directories)
                    if (dirEntry.Item1 < 0x1000 && ((dirEntry.Item1 + dirEntry.Item2) < 0x1000))
                        Buffer.BlockCopy(nullBuff, 0, block, dirEntry.Item1, dirEntry.Item2);
            }

            if (SecOffset == e.BaseOfCode && e.BoundImprotLen > 0)
                Buffer.BlockCopy(nullBuff, 0, block, 0, e.BoundImprotLen * 4);

            else if (Relocs != null && !e.Is64)
                Relocs.DeLocateBuff32(block, (uint)Delta, (uint)SecOffset, Relocs.RelocData.ProcessedArray);
            else if (Relocs != null)
                Relocs.DeLocateBuff64(block, Delta, (ulong)SecOffset, Relocs.RelocData.ProcessedArray);
        }

        /// <summary>
        /// There are 2 paths through Run()
        /// 
        /// The primary route is the PageHash JSON protocol that validates hash entries sent by the client
        /// performs relocations and soforth.
        /// 
        /// The other sequence is to return a given file that the client wants to manually inspect.
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public async static Task Run(HttpContext ctx, string name, ILogger log)
        {
            Dictionary<string, string> entries = null;
            DeLocate rere = null;
            MemPageHash mph = null;
            StringContent failure = null;
            ulong BaseAddress = 0;
            long content_Len = 0L, RVA = 0L, SecOffset = 0L;
            var block = new Byte[0x1000];
            MiniSection ms = MiniSection.Empty;
            string modName = string.Empty, targetName = string.Empty, hash = string.Empty;
            var req = ctx.Request;
            var resp = ctx.Response;

            resp.StatusCode = (int)HttpStatusCode.NoContent;

            try {
                content_Len = req.ContentLength ?? 0;
                entries = req.Query.ToDictionary(q => q.Key, q => (string)q.Value);

                // attempt to just dump back the binary we would of hashed
                if (req.Method == "GET")
                {
                    if(!entries.ContainsKey("file") || string.IsNullOrWhiteSpace(entries["file"]))
                    {
                        log.LogWarning($"Get requests require a file parameter, none was sent.");
                        resp.StatusCode = (int) HttpStatusCode.NotFound;
                        return;
                    }
                    var return_file = entries["file"].ToLower();
                    var ret_file_base = Path.GetFileName(return_file);
                    if (!GoldImages.DiskFiles.ContainsKey(ret_file_base))
                    {
                        log.LogInformation($"Unknown file requested from client [{ret_file_base}].");
                        return;
                    }

                    var file_set = GoldImages.DiskFiles[ret_file_base];
                    foreach (var x in file_set)
                    {
                        if (x.Item3.Substring(2).Equals(return_file.Substring(2), StringComparison.InvariantCultureIgnoreCase))
                        {
                            resp.StatusCode = (int)HttpStatusCode.OK;
                            targetName = x.Item3;
                            using (var fout = File.Open(targetName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                // return the file rerelocated
                                if (entries.ContainsKey("mapped"))
                                {
                                    fout.Read(block, 0, 0x1000);
                                    var e = Extract.IsBlockaPE(block);

                                    // PS sends this in base 10
                                    var parsed = ulong.TryParse(entries["mapped"], out BaseAddress);

                                    log.LogInformation($"Requested relocation address of returned image  [{BaseAddress:x}], performing relocations.");

                                    if (e.SizeOfHeaders < block.Length)
                                        Buffer.BlockCopy(nullBuff, 0, block, (int)e.SizeOfHeaders, block.Length - (int)e.SizeOfHeaders);
                                    if (e.ImageBase != BaseAddress)
                                    {
                                        var reRawData = Extract.ExtractRelocData(targetName);
                                        if (reRawData != null)
                                        {
                                            e.ReReState = rere = new DeLocate(e.ImageBase, reRawData);
                                            rere.RelocData.Processed = DeLocate.ProcessRelocs(reRawData, null);
                                            rere.RelocData.ProcessedArray = rere.RelocData.Processed.ToArray();
                                        }
                                        AttemptDelocate(block, e, 0, 0, BaseAddress, rere);
                                    }
                                    // send header out
                                    await resp.Body.WriteAsync(block, 0, 0x1000).ConfigureAwait(false);
                                    // loop over the rest of the file
                                    RVA += 0x1000;
                                    do {
                                        for (int i = 0; i < e.NumberOfSections; i++) {
                                            if (RVA >= e.Sections[i].VirtualAddress && RVA < (e.Sections[i].VirtualAddress + e.Sections[i].VirtualSize)) {
                                                ms = e.Sections[i];
                                                break;
                                            }
                                        }
                                        SecOffset = (RVA - ms.VirtualAddress);
                                        fout.Position = ms.RawFilePointer + (RVA - ms.VirtualAddress);
                                        fout.Read(block, 0, 0x1000);
                                        if (rere != null && e.ImageBase != BaseAddress)
                                            AttemptDelocate(block, e, e.ImageBase - BaseAddress, RVA, e.ImageBase, rere);

                                        await resp.Body.WriteAsync(block, 0, 0x1000).ConfigureAwait(false);
                                        RVA += 0x1000;
                                    } while (RVA < e.SizeOfImage);
                                }
                                else
                                {
                                    await fout.CopyToAsync(resp.Body).ConfigureAwait(false);
                                    return;
                                }
                            }
                        }
                    }
                    log.LogTrace("Get request serviced.");
                    return;
                }
            } catch (Exception ex) { log.LogWarning($"Handling exception {ex}"); }

            if(req.Method != "POST")
            {
                log.LogWarning($"POST or GET allowed only, client sent {req.Method}");
                return;
            }

            var rv = new HashSet<PageHashBlockResult>();
            var cv = CODEVIEW_HEADER.Init(entries);
            try {
                var buffer = new byte[content_Len];
                var bytesRead = 0;
                while (bytesRead < buffer.Length)
                {
                    var count = await req.Body.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                    bytesRead += count;
                }
                log.Log<string>(LogLevel.Trace, new EventId(2, "Read"), $"read post body {bytesRead}", null, (state, ex) => $"{state}");

                hash = Encoding.Default.GetString(buffer);
                // preconfigure return buffer so we will always return FALSE for failure
                mph = JsonConvert.DeserializeObject<MemPageHash>(hash);
                if (mph == null)
                {
                    log.LogWarning($"Client sent a serialized JSON POST content that were unable to handle Data:[{hash}], len:[{content_Len}]");
                    return;
                }

                rv.Add(new PageHashBlockResult() { Address = mph.AllocationBase, HashCheckEquivalant = false });
                foreach (var ph in mph.HashSet)
                    rv.Add(new PageHashBlockResult() { Address = ph.Address, HashCheckEquivalant = false });

                //configure failure case
                var rx = JsonConvert.SerializeObject(rv, Formatting.Indented);
                failure = new StringContent(rx, Encoding.Default, "application/json");
                // after here we can send 200 back but everything is a fail
                resp.StatusCode = (int) HttpStatusCode.OK;

                // sanitation & isolation of file name portion of input string (hopefully:)
                modName = mph.ModuleName.Split(Path.DirectorySeparatorChar).LastOrDefault();
                if (modName.Length <= 3 || modName.Contains(".."))
                {
                    await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                    log.LogWarning($"Client filename [{mph.ModuleName}] invalid or Likely dynamic code at address [{mph.BaseAddress}:x]");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(modName) &&
                    modName.Any((x) => x ==
                        Path.AltDirectorySeparatorChar ||
                        InvalidFileNameChars.Contains(x) ||
                        char.IsSurrogate(x) ||
                        char.IsSymbol(x) ||
                        char.IsControl(x) ||
                        x == Path.VolumeSeparatorChar ||
                        x == Path.PathSeparator))
                {
                    await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                    log.LogWarning($"Client filename invalid or possibly evil [{mph.ModuleName}]");
                    return;
                }

                // should be redundant given were already at the last '\\'
                modName = Path.GetFileName(modName);


            }
            catch (Exception ex)
            {
                await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                log.LogWarning($"Failure, check exception {ex}");
                return;
            }

            cv.Name = modName.ToLower();
            cv.TimeDateStamp = mph.TimeDateStamp;
            cv.VSize = mph.ImageSize;

            log.LogTrace($"Performing lookup on client requested dtails [{mph.ModuleName}] [{cv.TimeDateStamp:x}] [{cv.VSize:x}]");

            if (cv.TimeDateStamp == 0|| cv.VSize == 0)
            {
                await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                log.LogWarning($"Client timestamp is out of range [{cv.TimeDateStamp:x}]");
                return;
            }
            // if we do not have this binary locally cached
            // attempt to retrieve it
                
            var localGoldBinaryFound = GoldImages.DiskFiles.ContainsKey(cv.Name);
            // update targetname if we can find it in the local ngen cache
            if (localGoldBinaryFound)
            {
                log.LogTrace("Found candidates for client integrity check.");

                localGoldBinaryFound = false;
                var goldEntries = GoldImages.DiskFiles[cv.Name];

                foreach (var ng in goldEntries.Where((x) => x.Item3.Substring(3).Equals(mph.ModuleName.Substring(3), StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (ng.Item1 == cv.VSize && ng.Item2 == cv.TimeDateStamp)
                    {
                        targetName = ng.Item3;
                        localGoldBinaryFound = true;
                        log.LogInformation($"Matched candiate for client [{targetName}]");
                        break;
                    }
                }
                if (!localGoldBinaryFound)
                {
                    foreach (var ng in goldEntries)
                    {
                        if (ng.Item1 == cv.VSize && ng.Item2 == cv.TimeDateStamp)
                        {
                            targetName = ng.Item3;
                            localGoldBinaryFound = true;
                            log.LogInformation($"Matched candiate for client {targetName}");
                            break;
                        }
                    }
                }
            }

            if (!localGoldBinaryFound)
            {
                if (!Program.Settings.Host.ProxyToExternalgRoot)
                {
                    await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                    log.LogInformation($"Unable to locate a candiate that fits required criteria to requeted info, find original media for client requested file {modName} and store into golden image filesystem.7");
                    return;
                }
                else // proxy is enabled
                {
                    if(Program.Settings.External != null && !string.IsNullOrWhiteSpace(Program.Settings.External.gRoot))
                    {
                        log.LogInformation($"Remote service call started");
                        resp.StatusCode = (int) HttpStatusCode.OK;
                        try { 
                            await WebAPI.ProxyObject(hash, resp.Body);
                        } catch (Exception ex)  {
                            log.LogWarning($"exceptoin in calling external gRoot server.  {ex}");
                            resp.StatusCode = (int) HttpStatusCode.NotFound;
                        }
                        log.LogInformation($"External server handled request.");
                        return;
                    }
                }
            }
            // setup connection to desired originating input so we can hash it and determine if
            // the caller is investigating the same binary
            using (var fr = new FileStream(targetName, FileMode.Open, FileAccess.Read))
            {
                fr.Read(block, 0, 0x1000);
                // parse the binary
                var e = Extract.IsBlockaPE(block);
                // fatal error
                if (e == null)
                {
                    await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                    log.LogWarning($"Corrupt image or some failure, unable to determine PE information for request {targetName}");
                    return;
                }

                if (e.SizeOfHeaders < block.Length)
                    Buffer.BlockCopy(nullBuff, 0, block, (int)e.SizeOfHeaders, block.Length - (int)e.SizeOfHeaders);

                var LocalHeaderHash = Convert.ToBase64String(hasher.ComputeHash(block));

                var HdrMatch = (mph.HdrHash == LocalHeaderHash);
                bool NeededHdrDelocate = false;

                if ((long)e.ImageBase != mph.AllocationBase && !HdrMatch)
                {
                    var reRawData = Extract.ExtractRelocData(targetName);
                    if (reRawData != null)
                    {
                        e.ReReState = rere = new DeLocate(e.ImageBase, reRawData);
                        rere.RelocData.Processed = DeLocate.ProcessRelocs(reRawData, null);
                        rere.RelocData.ProcessedArray = rere.RelocData.Processed.ToArray();
                    }

                    AttemptDelocate(block, e, 0, 0, (ulong)mph.AllocationBase, rere);
                    LocalHeaderHash = Convert.ToBase64String(hasher.ComputeHash(block));
                    HdrMatch = NeededHdrDelocate = (mph.HdrHash == LocalHeaderHash);
                }

                var rvenum = rv.GetEnumerator();
                // we injected one for the header so move to it here
                rvenum.MoveNext();
                rvenum.Current.HashCheckEquivalant = HdrMatch;

                // now we have structured access to the file VA's etc..
                // go through each hash and match
                foreach (var h in mph.HashSet)
                {
                    RVA = h.Address - mph.AllocationBase;
                    if (RVA > uint.MaxValue || RVA < 0)
                    {
                        await failure.CopyToAsync(resp.Body).ConfigureAwait(false);
                        log.LogWarning($"RVA in client JSON request seems invalid, aborting. {RVA:x}");
                        return;
                    }

                    for (int i = 0; i < e.NumberOfSections; i++)
                    {
                        if (RVA >= e.Sections[i].VirtualAddress && RVA < (e.Sections[i].VirtualAddress + e.Sections[i].VirtualSize))
                        {
                            ms = e.Sections[i];
                            break;
                        }
                    }
                    // since RVA is calculated from the AllocationBase, section VA may be anywhere above it
                    // RawFilePointer to get file POS + (RVA - SectionVA) 
                    SecOffset = (RVA - ms.VirtualAddress);
                    fr.Position = ms.RawFilePointer + (RVA - ms.VirtualAddress);

                    // we should be file-aligned to the appropriate location to read a page and check the hash now
                    fr.Read(block, 0, 0x1000);

                    var localHashCheck = hasher.ComputeHash(block);
                    var check64 = Convert.ToBase64String(localHashCheck);

                    if (rvenum.MoveNext())
                        rvenum.Current.HashCheckEquivalant = (check64 == h.Hash);

                    if (!rvenum.Current.HashCheckEquivalant && rere != null && e.ImageBase != (ulong)mph.AllocationBase)
                    {
                        AttemptDelocate(block, e, e.ImageBase - (ulong)mph.AllocationBase, RVA, e.ImageBase, rere);

                        check64 = Convert.ToBase64String(hasher.ComputeHash(block));
                        rvenum.Current.HashCheckEquivalant = (check64 == h.Hash);
                    }
                }
            }
#if FALSE
            // find a better way todo this...
            if(Program.Settings.Host.ProxyFailedOrMissing)
            {
                if(rv.Any((x) => !x.HashCheckEquivalant))
                {
                    HttpResponseMessage prox_rv = null;
                    log.LogInformation($"At least one hash check failed, ProxyFailedOrMissing check to double check results");
                    try {
                        prox_rv = WebAPI.POST(hash).Result;
                    } catch (Exception ex)
                    {
                        log.LogWarning($"Error in contacting remote server. {ex}");
                    }
                    if (prox_rv == null)
                        return;

                    try
                    {
                        var prox_data = await prox_rv.Content.ReadAsByteArrayAsync();
                        var prox_r = JsonConvert.DeserializeObject<HashSet<PageHashBlockResult>>(Encoding.Default.GetString(prox_data));
                        HashSet<PageHashBlockResult> CombinedLookups = new HashSet<PageHashBlockResult>();
                        var enumer = rv.GetEnumerator();
                        enumer.MoveNext();
                        foreach (var pbr in prox_r)
                        {
                            if (enumer.Current.Address != pbr.Address)
                            {
                                log.LogInformation("Server and local results are not aligne, unable to corolate checks further");
                                break;
                            }
                            // build combined list checking if local passed, use it, if not use the server result, maybe it passed ? ;)
                            CombinedLookups.Add(enumer.Current.HashCheckEquivalant ? enumer.Current : pbr);
                            enumer.MoveNext();
                        }
                        log.LogInformation($"Using combined lookups Orig Pass count[{rv.Count((x) => x.HashCheckEquivalant)}] New pass count [{CombinedLookups.Count((x) => x.HashCheckEquivalant)}]");
                        rv = CombinedLookups;
                    } catch (Exception ex)
                    {
                        log.LogWarning($"Error in corolation processing {ex}");
                    }
                }
            }
#endif
            // since we made it this far we should re-serialize our state since we currently are holding onto
            // and object that is used for a fast path error exit (all failed)
            var r = JsonConvert.SerializeObject(rv, Formatting.Indented);
            resp.StatusCode = 200;
            var success = new StringContent(r, Encoding.Default, "application/json").CopyToAsync(resp.Body).ConfigureAwait(false);
            log.LogInformation($"Finished client service call, hash check results sent.");
        }
    }
}