using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MegaApi.Comms.Converters;

namespace MegaApi.Comms.Requests
{

    public class MResponseGetFiles : MegaResponse
    {
        [JsonProperty("f")]
        public List<MegaNode> Nodes { get; set; }

        [JsonProperty("u")]
        public List<MResponseGetUser> User { get; set; }

        [JsonProperty("sn")]
        public string SCid { get; set; }

    //    "ok": [{ <- ?
    //    "h": "7ZUQnBTQ",
    //    "ha": "pRunygYRhFRhjjzcI41fHQ",
    //    "key": "S-o8euIDmVZIm76ho0VrqQ"
    //}],
    //"s": [{ <- shares
    //    "h": "7ZUQnBTQ",
    //    "u": "D4TkyU-YG6w",
    //    "r": 2,
    //    "ts": 1360502711

    }
    internal class MRequestGetFiles<T> : MegaRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "f";
        [DataMember]
        public int c = 1;

        MegaUser user;
        public MRequestGetFiles(MegaUser user) : base(user) { this.user = user; }

        public override void HandleSuccess(JToken response)
        {
            var conv = new NodeKeyConverter(user.masterKeyAlg, user);
            CallSuccessHandler(JsonConvert.DeserializeObject<T>(response.ToString(), conv));
        }
    }

}
