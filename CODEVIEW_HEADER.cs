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


using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using System.IO;

namespace Dia2Sharp
{
    [ProtoContract(AsReferenceDefault = true, ImplicitFields = ImplicitFields.AllPublic)]
    public class CODEVIEW_HEADER
    {
        public static CODEVIEW_HEADER Init(IEnumerable<KeyValuePair<string, string>> entries)
        {
            string GUID = string.Empty;
            string age = string.Empty;
            string PDB = string.Empty;
            string sig = string.Empty;
            string name = string.Empty;
            string vsize = string.Empty;
            string symName = string.Empty;
            string symRange = string.Empty;
            string symAddr = string.Empty;
            string typeName = string.Empty;
            string timeStamp = string.Empty;
            string baseVaddr = string.Empty;

            foreach (var entry in entries)
            {
                var key = entry.Key.ToLower();
                switch (key)
                {
                    case "age": age = entry.Value; break;
                    case "sig": sig = entry.Value; break;
                    case "pdb": PDB = entry.Value; break;
                    case "guid": GUID = entry.Value; break;
                    case "name": name = entry.Value; break;
                    case "vsize": vsize = entry.Value; break;
                    case "type": typeName = entry.Value; break;
                    case "symaddr": symAddr = entry.Value; break;
                    case "symname": symName = entry.Value; break;
                    case "baseva": baseVaddr = entry.Value; break;
                    case "symrange": symRange = entry.Value; break;
                    case "timedate": timeStamp = entry.Value; break;
                    default: break;
                }
            }
            return Init(name, PDB, symName, symAddr, typeName, baseVaddr, vsize, symRange, age, sig, timeStamp, GUID);
        }


        public static CODEVIEW_HEADER Init(
            string name = null,
            string pdb = null,
            string symname = null,
            string symaddr = null,
            string typename = null,
            string baseva = null,
            string vsize = null,
            string symrange = null,
            string age = null,
            string sig = null,
            string timestamp = null,
            string guid = null
            )
        {
            bool parsed = false;
            var cv = new CODEVIEW_HEADER();

            if (name.Contains("..")) return null;
            if (pdb.Contains("..")) return null;

            if (!string.IsNullOrWhiteSpace(pdb) && pdb.Any((x) => char.IsSurrogate(x) || char.IsSymbol(x) || char.IsControl(x) || x == Path.DirectorySeparatorChar || x == Path.AltDirectorySeparatorChar || x == Path.VolumeSeparatorChar || x == Path.PathSeparator)) return null;
            if (!string.IsNullOrWhiteSpace(name) && name.Any((x) => char.IsSurrogate(x) || char.IsSymbol(x) || char.IsControl(x) || x == Path.DirectorySeparatorChar || x == Path.AltDirectorySeparatorChar || x == Path.VolumeSeparatorChar || x == Path.PathSeparator)) return null;

            cv.Name = name;
            cv.PdbName = pdb;
            cv.Type = typename;
            cv.SymName = symname;
            cv.BaseVA = ParseUlong(baseva, ref parsed);
            cv.SymAddr = ParseUlong(symaddr, ref parsed);
            cv.VSize = ParseUint(vsize, ref parsed);
            cv.SymRange = ParseUint(symrange, ref parsed);
            cv.Age = ParseUint(age, ref parsed);
            cv.Sig = ParseUint(sig, ref parsed);
            cv.TimeDateStamp = ParseUint(timestamp, ref parsed);
            Guid.TryParse(guid, out cv.aGuid);
            return cv;
        }

        public static ulong ParseUlong(string intStr, ref bool parsed)
        {
            ulong rv = 0;
            var parse = intStr.Trim(new char[] { '\"', '\'', '?', '&', '=', '.', ',' });
            if (parse.Contains("x"))
                parse = parse.Substring(parse.IndexOf("x") + 1);

            if (!ulong.TryParse(parse, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out rv))
                if (!ulong.TryParse(parse, out rv))
                    parsed = false;
                else
                    parsed = true;
            else
                parsed = true;

            return rv;
        }

        public static uint ParseUint(string intStr, ref bool parsed)
        {
            uint rv = 0;
            var parse = intStr.Trim(new char[] { '\"', '\'', '?', '&', '=', '.', ',' });
            if (parse.Contains("x"))
                parse = parse.Substring(parse.IndexOf("x") + 1);

            if (!uint.TryParse(parse, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out rv))
                if (!uint.TryParse(parse, out rv))
                    parsed = false;
                else
                    parsed = true;
            else
                parsed = true;

            return rv;
        }

        public uint Age;
        public uint Sig;
        public uint TimeDateStamp;
        public Guid aGuid;
        public byte[] byteGuid;
        public string Name;
        public string PdbName;
        public string Type;
        public string SymName;
        public ulong SymAddr;
        public ulong BaseVA;
        public uint VSize;
        public uint SymRange;
        // This field is determined through a call to SymFindFileInPath/Ex from the above info 
        public string PDBFullPath;
    }
}