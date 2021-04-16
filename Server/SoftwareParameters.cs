using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using True_Mining_Desktop.Core;

namespace True_Mining_Desktop.Server
{
    public class Pool
    {
        public string mineableCoin { get; set; }
        public string pool { get; set; } = "nanopool";
        public string host1 { get; set; }
        public string host2 { get; set; }
        public string stratumPort { get; set; }
        public string stratumPortSSl { get; set; }
        public string wallet_TM { get; set; }
        public string email { get; set; }
        public string password { get; set; } = "x";
    }

    public class TrueMiningFiles
    {
        public string changelogLink;
        public List<FileDetails> files;
    }

    public class FileDetails
    {
        public string dlLink;
        public string path;
        public string fileName;
        public string sha256;
    }

    public class ThirdPartyBinaries
    {
        public List<FileDetails> files;
    }

    public class Kind
    {
        public List<Pool> Pools;
        public int DynamicFee = 2;
        public TrueMiningFiles TrueMiningFiles;
        public ThirdPartyBinaries ThirdPartyBinaries;
    }

    public class SoftwareParameters
    {
        public static Kind ServerConfig;

        private static DateTime lastUpdated = DateTime.Now.AddHours(-1).AddMinutes(-1);

        public static void Update(Uri uri)
        {
            while (!Tools.IsConnected()) { Thread.Sleep(3000); }

            if (lastUpdated.AddHours(1).Ticks < DateTime.Now.Ticks)
            {
                bool trying = true;

                while (trying)
                {
                    lastUpdated = DateTime.Now;
                    try
                    {
                        SoftwareParameters.ServerConfig = JsonConvert.DeserializeObject<Kind>(new WebClient().DownloadString(uri)); //update parameters
                        trying = false;
                    }
                    catch { }
                }
            }
        }
    }
}