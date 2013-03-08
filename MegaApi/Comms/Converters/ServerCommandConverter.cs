using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using MegaApi.Comms.Requests;
using Newtonsoft.Json.Linq;

namespace MegaApi.Comms.Converters
{
    public class ServerCommandConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ServerCommand);
        }

        MegaUser user;
        public ServerCommandConverter(MegaUser user)
        {
            this.user = user;
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            var str = jsonObject.ToString();
            ServerCommand cmd = null;
            switch(jsonObject["a"].ToObject<string>())
            {
                case ServerCommandType.NodeUpdation:
                    cmd = JsonConvert.DeserializeObject<NodeUpdationCommand>(str,
                        new NodeKeyConverter(user.masterKeyAlg,user));
                break;
                case ServerCommandType.NodeAddition:
                    cmd = JsonConvert.DeserializeObject<NodeAdditionCommand>(str,
                        new NodeKeyConverter(user.masterKeyAlg,user));
                break;
                case ServerCommandType.NodeDeletion:
                    cmd = JsonConvert.DeserializeObject<NodeDeletionCommand>(str);
                break;
                case ServerCommandType.ShareOperation:
                    cmd = JsonConvert.DeserializeObject<ShareOperationCommand>(str);
                break;
                case ServerCommandType.CryptoRequest:
                    cmd = JsonConvert.DeserializeObject<CryptoRequestCommand>(str);
                break;

            }
            return cmd;

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
