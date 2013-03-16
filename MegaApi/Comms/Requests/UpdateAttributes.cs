using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MegaApi.Comms.Requests
{
    public class MResponseUpdateAttributes : MegaResponse
    {
        public bool Ok = true;
    }

    internal class MRequestUpdateAttributes<T> : TrackingRequest<T> where T : MResponseUpdateAttributes
    {
        [DataMember]
        public string a = "a";
        [DataMember]
        public string n;
        [DataMember]
        [JsonProperty("attr")]
        [JsonConverter(typeof(ByteConverter))]
        public byte[] encryptedAttributes { get; set; }
        [DataMember]
        [JsonConverter(typeof(ByteConverter))]
        public byte[] key;

        public MRequestUpdateAttributes(MegaUser user, MegaNode node) : base(user)
        {
            MegaNode.EncryptAttrs(node);
            encryptedAttributes = node.encryptedAttributes;
            n = node.Id;
            key = node.NodeKey.EncryptedKey;
        }

        public override void HandleSuccess(Newtonsoft.Json.Linq.JToken response)
        {
            if (response.Type == JTokenType.Integer && response.ToObject<int>() == 0)
            {
                CallSuccessHandler((T)new MResponseUpdateAttributes());
            }
            else
            {
                HandleError(MegaApiError.EUNEXPECTED);
            }
        }
    }
}
