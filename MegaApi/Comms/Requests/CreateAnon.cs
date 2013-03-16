using MegaApi.Comms;
using MegaApi.Cryptography;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using MegaApi;
using System;

namespace MegaApi.Comms.Requests
{
    [JsonConverter(typeof(StringConverter))]
    public class MResponseCreateAnon : MegaResponse
    {
        [JsonProperty("str")]
        public string UserId { get; set; }
    }

    internal class MRequestCreateAnon<T> : MegaRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "up";
        [DataMember]
        [JsonConverter(typeof(ByteConverter))]
        byte[] k;
        [DataMember]
        [JsonConverter(typeof(ByteConverter))]
        byte[] ts;

        public MRequestCreateAnon(MegaUser user)
            : base(user)
        {
            var ssc = Crypto.RandomKey(16);
            var aes = Crypto.CreateAes(user.PassKey);
            var e_ssc = Crypto.Encrypt(aes, ssc);
            ts = new byte[ssc.Length + e_ssc.Length];
            Array.Copy(ssc, ts, ssc.Length);
            Array.Copy(e_ssc, 0, ts, ssc.Length, e_ssc.Length);

            k = user.EncryptUserKey();
        }
    }
}