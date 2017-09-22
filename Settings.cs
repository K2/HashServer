using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HashServer
{
    public class AppOptions
    {
        public Profile Host { get; set; }
        public gRoots External { get; set; }
        public gRoots Internal { get; set; }
        public gRoots InternalSSL { get; set; }
        public gFiles Profile { get; set; }
    }

    public class gRoots
    {
        public string gRoot { get; set; }
    }

    public class Images
    {
        public string OS { get; set; }
        public string ROOT { get; set; }
    }

    public class gFiles
    {
        public Images[] Images { get; set; }
    }

    public class Profile
    {
        public string Machine { get; set; }
        public ushort BasePort { get; set; }
    }
}
