using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaApi.DataTypes;
using Newtonsoft.Json;
using MegaApi.Cryptography;

namespace MegaApi.Comms.Converters
{
    /// <summary>
    /// checks if the session's last bytes are the first ones encrypted with the provided key
    /// </summary>
    public class CheckedSessionConverter : MCryptoConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(CheckedSession);
        }

        public CheckedSessionConverter(byte[] key) : base(key) { }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String) { return null; }
            var result = new CheckedSession();
            result.SessionId = reader.Value.ToString();
            var sidBytes = Transport.Decode(result.SessionId);
            var sidFirst = new byte[16];
            var sidSecond = new byte[16];
            Array.Copy(sidBytes, sidFirst, 16);
            Array.Copy(sidBytes, sidBytes.Length - 16, sidSecond, 0, 16);
            var t = Crypto.Encrypt(alg, sidFirst);
            result.IsOk = t.SequenceEqual(sidSecond);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, ((CheckedSession)value).SessionId);
        }
    }

}
