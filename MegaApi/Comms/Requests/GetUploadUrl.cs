using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaApi.Comms.Requests;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MegaApi.Comms.Requests
{
    public class MResponseGetUploadUrl : MegaResponse
    {
        [JsonProperty("p")]
        public string Url { get; set; }
    }

    internal class MRequestGetUploadUrl<T> : MegaRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "u";
        [DataMember]
        public bool ssl = false;
        [DataMember]
        public long s;

        //r : ul_queue[i].retries
        //e : ul_lastreason
        //ms : ul_maxSpeed

        public MRequestGetUploadUrl(MegaUser user, long filesize) : base(user) { s = filesize; }

    }
}
