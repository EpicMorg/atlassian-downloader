using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader
{

    public partial class ResponseItem
    {
        public string Description { get; set; }
        public string Edition { get; set; }
        public Uri ZipUrl { get; set; }
        public object TarUrl { get; set; }
        public string Md5 { get; set; }
        public string Size { get; set; }
        public string Released { get; set; }
        public string Type { get; set; }
        public string Platform { get; set; }
        public string Version { get; set; }
        public Uri ReleaseNotes { get; set; }
        public Uri UpgradeNotes { get; set; }
    }
}