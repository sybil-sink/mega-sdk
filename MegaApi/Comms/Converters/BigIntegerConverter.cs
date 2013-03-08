using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Mono.Math;

namespace MegaApi.Comms.Converters
{
    /// <summary>
    /// Used to convert base64 encoded values encrypted as MPI into BigInteger and vice versa
    /// </summary>
    class BigIntegerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BigInteger);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return new BigInteger((int)reader.Value);
            }
            var bytes = Transport.Decode(reader.Value.ToString());
            return Converter.MpiToBigInt(bytes);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bytes = Converter.BigIntToMpi((BigInteger)value);
            serializer.Serialize(writer, Transport.Encode(bytes));
        }
    }
}
