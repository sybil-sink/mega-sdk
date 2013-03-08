using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MegaApi.Comms
{
    /// <summary>
    /// Used to convert the string token itself to an object witn the attribute marked [JsonProperty("str")]
    /// 
    /// Example: 
    /// [JsonConverter(typeof(StringConverter))]
    /// public class MResponseCreateAnon : MResponse
    /// {
    ///     [JsonProperty("str")]
    ///     public string UserId { get; set; }
    /// }
    /// </summary>
    class StringConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.String) { return null; }
            var jObject = JObject.Parse(String.Format("{{\"str\":\"{0}\"}}", reader.Value));
            var target = Activator.CreateInstance(objectType);
            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            
        }
    }
}
