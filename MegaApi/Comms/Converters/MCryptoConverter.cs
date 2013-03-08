using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using MegaApi.Cryptography;
using MegaApi.DataTypes;

namespace MegaApi.Comms.Converters
{
    /// <summary>
    /// Convert values using Crypto.Decrypt method and the provided algorithm
    /// </summary>
    public class MCryptoConverter : JsonConverter
    {
        public SymmetricAlgorithm alg;
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MCrypto);
        }


        public MCryptoConverter(byte[] key) : this(Crypto.CreateAes(key))
        {
        }
        public MCryptoConverter(SymmetricAlgorithm alg)
        {
            this.alg = alg;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var encrypted = Transport.Decode(reader.Value.ToString());
            return new MCrypto { Value = Crypto.Decrypt(alg, encrypted) };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var encrypted = Crypto.Encrypt(alg, ((MCrypto)value).Value);
            serializer.Serialize(writer, Transport.Encode(encrypted));
        }
    }
}
