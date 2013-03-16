using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using MegaApi.Comms.Converters;

namespace MegaApi.Comms.Requests
{
    public class MResponseGetUser : MegaResponse
    {
        [JsonProperty("u")]
        public string UserId { get; set; }

        [JsonProperty("email")]
        public string Email 
        {
            get { return _mail; }
            set { _mail = value; }
        }

        // do not remove! used in the getFiles request
        [JsonProperty("m")]
        public string _mail { get; set; }

        public string privk { get; set; }
        public int c { get; set; }

        //[JsonProperty("ts")]
        //[JsonConverter(typeof(UnixDateTimeConverter))]
        //public DateTime Timestamp { get; set; }

        [JsonIgnore]
        public int UserStatus;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (Email == null) { UserStatus = MegaUserStatus.Anonymous; }
            else if (privk == null) { UserStatus = MegaUserStatus.EmailConfirmed; }
            else { UserStatus = MegaUserStatus.Complete; }
            if (c > 0) { UserStatus = c; }
        }
    }
    internal class MRequestGetUser<T> : MegaRequest<T> where T : MegaResponse
    {
        [DataMember]
        public string a = "ug";

        public MRequestGetUser(MegaUser user) : base(user) { }
    }

    // works only when just registered anon user, mega identicaly
    // last 16 bytes should be the first 16 encoded by the passkey
    // var ts = GetStr(obj["ts"]);
    //var sidBytes = MegaTransport.Decode(ts);
    //var firstBytes = new byte[16];
    //var lastBytes = new byte[16];
    //Array.Copy(sidBytes, firstBytes, 16);
    //Array.Copy(sidBytes, 16, lastBytes, 0, 16);
    //var aes = MegaCrypto.CreateAes(usr.PassKey);
    //var checkBytes = MegaCrypto.Encrypt(aes,firstBytes);
    //args.SessionClear = lastBytes.SequenceEqual(checkBytes);
    //Debug.WriteLine(String.Format("ts is {0}clear", args.SessionClear ? "" : "not "));
}
