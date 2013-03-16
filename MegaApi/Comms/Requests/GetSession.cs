using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Mono.Math;
using MegaApi.Comms.Converters;
using Newtonsoft.Json.Linq;
using MegaApi.DataTypes;
using MegaApi.Cryptography;

namespace MegaApi.Comms.Requests
{
    
    public class MResponseGetSid : MegaResponse
    {
        public string SessionId { get; set; }

        [DataMember]
        [JsonProperty("k")]
        public MCrypto MasterKey { get; set; }
        [DataMember]
        public CheckedSession tsid { get; set; }
        [DataMember]
        [JsonConverter(typeof(BigIntegerConverter))]
        public BigInteger csid { get; set; }

        [DataMember]
        public string privk { get; set; }

        public RSAKey PrivateKey { get; set; }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (tsid!=null && tsid.IsOk) { SessionId = tsid.SessionId; }
            else
            {
                // decrypt session
                if (MasterKey == null) { return; }
                var str = String.Format(@"{{""val"":""{0}""}}", privk);
                var helper = JsonConvert.DeserializeObject<PrivateKeyHelper>(str, new PrivateKeyConverter(MasterKey.Value));
                PrivateKey = helper.val;
                if (PrivateKey == null) { return; }
                
                var sid = csid.ModPow(PrivateKey.D, PrivateKey.N);
                // val, exp, mod
                //var sid = BigInteger.ModPow(csid, PrivateKey.D, PrivateKey.N);

                var reversedBytes = sid.GetBytes();
                var shift = reversedBytes[reversedBytes.Length - 1] == 0 ? 1 : 0;
                SessionId = Transport.Encode(reversedBytes
                             //                       .Reverse()
                               //                     .Skip(shift)
                                                    .Take(43)
                                                    .ToArray());
                
            }
        }

    }
    internal class MRequestGetSid<T> : MegaRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "us";
        [DataMember]
        string user;
        [DataMember]
        [JsonConverter(typeof(ByteConverter))]
        byte[] uh;

        byte[] passKey;

        public MRequestGetSid(MegaUser user) : base(user)
        {
            Sid = null;
            this.user = user.Id;
            passKey = user.PassKey;
            if (user.Email != null)
            {
                this.user = user.Email.ToLower();
                uh = user.GetHash();
            }
        }

        public override void HandleSuccess(JToken response)
        {
            var resp = JsonConvert.DeserializeObject<T>(response.ToString(), 
                new MCryptoConverter(passKey), 
                new CheckedSessionConverter(passKey));
            CallSuccessHandler(resp);
        }
    }
    public class PrivateKeyHelper
    {
        public RSAKey val { get; set; }
    }
}
