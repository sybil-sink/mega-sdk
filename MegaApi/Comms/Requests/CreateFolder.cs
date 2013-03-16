using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using MegaApi.DataTypes;
using MegaApi.Cryptography;
using Newtonsoft.Json.Linq;
using MegaApi.Comms.Converters;
using Newtonsoft.Json;

namespace MegaApi.Comms.Requests
{
    public class MResponseCreateFolder : MegaResponse
    {
        [JsonProperty("f")]
        public List<MegaNode> Created { get; set; }
    }
    internal class MRequestCreateFolder<T> : TrackingRequest<T> where T : MResponseCreateFolder
    {
        [DataMember]
        public string a = "p";

        [DataMember]
        public string t;

        [DataMember]
        public List<MegaNode> n = new List<MegaNode>();

        MegaUser user;
        public MRequestCreateFolder(MegaUser user, string folderName, string parentNode) : base(user)
        {
            this.user = user;
            t = parentNode;
            var folder = new MegaNode
            {
                Id = "xxxxxxxx",
                Type = MegaNodeType.Folder,
                Attributes = new NodeAttributes { Name = folderName },
                NodeKey = new NodeKeys(Crypto.RandomKey(16), user)
            };
            n.Add(folder);

            Converters.Add(new NodeKeyConverter(user.masterKeyAlg, user));
        }

        public override void HandleSuccess(JToken response)
        {
            var conv = new NodeKeyConverter(user.masterKeyAlg, user);
            CallSuccessHandler(JsonConvert.DeserializeObject<T>(response.ToString(), conv));
        }
    }
}
