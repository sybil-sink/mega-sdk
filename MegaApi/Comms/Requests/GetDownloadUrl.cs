using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaApi.Comms.Requests;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MegaApi.Comms.Requests
{
    public class MResponseGetDownloadUrl : MegaResponse
    {
        [JsonProperty("g")]
        public string Url { get; set; }
        [JsonProperty("s")]
        public long FileSize { get; set; }
        [JsonProperty("at")]
        public string Attributes { get; set; }
    }

    internal class MRequestGetDownloadUrl<T> : MegaRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "g";
        [DataMember]
        public bool ssl = false;
        [DataMember]
        public int g = 1; // ?
        [DataMember]
        public string n;
        // p - ?

        public MRequestGetDownloadUrl(MegaUser user, string nodeId) : base(user) { n = nodeId; }

    }
}
