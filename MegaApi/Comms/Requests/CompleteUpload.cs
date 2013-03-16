using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MegaApi.Comms.Converters;

namespace MegaApi.Comms.Requests
{
    public class MResponseCompleteUpload : MegaResponse
    {
        [JsonProperty("f")]
        public List<MegaNode> NewNode { get; set; }
    }

    internal class MRequestCompleteUpload<T> : TrackingRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "p";
        [DataMember]
        public string t;
        [DataMember]
        public List<MegaNode> n;

        MegaUser user;
        public MRequestCompleteUpload(MegaUser user, MegaNode node) : base(user)
        {
            t = node.ParentId;
            n = new List<MegaNode> { node };
            this.user = user;

            Converters.Add(new NodeKeyConverter(user.masterKeyAlg, user));
        }

        public override void HandleSuccess(JToken response)
        {
            var conv = new NodeKeyConverter(user.masterKeyAlg, user);
            CallSuccessHandler(JsonConvert.DeserializeObject<T>(response.ToString(), conv));
        }
    }
}
