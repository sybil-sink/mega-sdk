using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaApi.DataTypes;
using System.Security.Cryptography;
using MegaApi.Cryptography;
using Newtonsoft.Json;
using Mono.Math;

namespace MegaApi.Comms.Converters
{
    /// <summary>
    /// Convert rsa private key from (and to?) bytes in the mega's format
    /// </summary>
    class PrivateKeyConverter : MCryptoConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RSAKey);
        }

        public PrivateKeyConverter(byte[] key) : base(key) { }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String) { return null; }
            var decrypted = (MCrypto)base.ReadJson(reader, objectType, existingValue, serializer);
            var rsa_privk = new BigInteger[4];
            var bytes = decrypted.Value;
            int startindex = 0;
            for (var i = 0; i < 4; i++)
            {
                var l = ((bytes[startindex + 0] * 256 + bytes[startindex + 1] + 7) >> 3) + 2;
                rsa_privk[i] = Converter.MpiToBigInt(bytes, startindex, l);
                startindex += l;
            }

            if (bytes.Length - startindex < 16)
            {
                return new RSAKey
                {
                    P = rsa_privk[0],
                    Q = rsa_privk[1],
                    D = rsa_privk[2],
                    U = rsa_privk[3],
                    N = BigInteger.Multiply(rsa_privk[0], rsa_privk[1])
                };
            }
            else return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
            //var encrypted = Crypto.Encrypt(alg, ((MCrypto)value).Value);
            //serializer.Serialize(writer, MTransport.Encode(encrypted));
        }
    }
}
