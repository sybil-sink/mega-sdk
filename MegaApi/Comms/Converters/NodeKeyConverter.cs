using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MegaApi.DataTypes;
using MegaApi.Cryptography;
using System.Security.Cryptography;

namespace MegaApi.Comms.Converters
{
    class NodeKeyConverter : MCryptoConverter
    {
        string userId;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(NodeKeys);
        }

        public NodeKeyConverter(SymmetricAlgorithm alg, MegaUser user) : base(alg)
        {
            userId = user.Id;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String) { return null; }
            var k = reader.Value.ToString();
            return ParseString(k);
        }

        NodeKeys ParseString(string keydef)
        {
            var keys = new NodeKeys();
            keys.Keys = new Dictionary<string, byte[]>();
            if (string.IsNullOrEmpty(keydef)) { return keys; }

            var pairs = keydef.Split(new char[] { '/' });
            foreach (var pair in pairs)
            {
                var p = pair.Split(new char[] { ':' });
                keys.Keys.Add(p[0], Transport.Decode(p[1]));
            }
            if (keys.Keys.ContainsKey(userId))
            {
                keys.EncryptedKey = keys.Keys[userId];
                keys.DecryptedKey = Crypto.Decrypt(alg, keys.Keys[userId]);
            }
            return keys;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, Transport.Encode(((NodeKeys)value).EncryptedKey));
        }
    }
}
