using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessSharePoint.Entities
{
    public class Downlaod
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string odataContext { get; set; }

        public List<Value> value { get; set; }

    }

    public class Value
    {
        [JsonProperty(PropertyName = "@microsoft.graph.downloadUrl")]
        public string microsoftgraphdownloadUrl { get; set; }
        public DateTime createdDateTime { get; set; }
        public string eTag { get; set; }
        public string id { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string name { get; set; }
        public string webUrl { get; set; }
        public string cTag { get; set; }
        public int size { get; set; }

        public FileSystemInfo fileSystemInfo { get; set; }
        public Shared shared { get; set; }
    }
}
