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
        public gFiles GoldSourceFiles { get; set; }
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

        // This setting will enable proxying to the external groot.
        // You will be able to maintain just a selection of unique binaries not nessissiarally the whole system.
        public bool ProxyToExternalgRoot { get; set; }
        public ushort ThreadCount { get; set; }
        public long MaxConcurrentConnections { get; set; }

        public string LogLevel { get; set; }

        public string CertificateFile { get; set; }
        public string CertificatePassword { get; set; }

        public string FileLocateNfo { get; set; }
    }
}
