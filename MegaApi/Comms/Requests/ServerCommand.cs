using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using MegaApi.Comms.Converters;
using MegaApi.DataTypes;
using System.Runtime.Serialization;

namespace MegaApi.Comms.Requests
{
    public static class ServerCommandType
    {
        public const string NodeUpdation = "u";
        public const string NodeAddition = "t";
        public const string NodeDeletion = "d";
        public const string ShareOperation = "s";
        public const string CryptoRequest = "k";
    }

    public class ServerCommand
    {
        [JsonProperty("a")]
        public string Command { get; set; }
        [JsonProperty("i")]
        public string CommandId { get; set; }
        [JsonIgnore]
        public bool IsMine { get; set; }
    }
    

    public class NodeListHelper
    {
        [JsonProperty("f")]
        public List<MegaNode> Nodes { get; set; }
    }
    public class NodeAdditionCommand : ServerCommand
    {
        [JsonProperty("t")]
        public NodeListHelper helper { get; set; }

        [JsonIgnore]
        public List<MegaNode> Nodes
        {
            get
            {
                return helper.Nodes;
            }
        }
    }

    public class NodeDeletionCommand : ServerCommand
    {
        [JsonProperty("n")]
        public string NodeId { get; set; }
    }

    public class NodeUpdationCommand : ServerCommand, IEncryptableAttributes
    {

        [JsonProperty("n")]
        public string NodeId { get; set; }
        [JsonProperty("u")]
        public string UserId { get; set; }
        [JsonProperty("at")]
        [JsonConverter(typeof(ByteConverter))]
        public byte[] encryptedAttributes { get; set; }
        [JsonIgnore]
        public NodeAttributes Attributes { get; set; }
        [JsonProperty("k")]
        public NodeKeys NodeKey { get; set; }
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty("ts")]
        public DateTime Timestamp { get; set; }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            MegaNode.DecryptAttrs(this);
        }
    }

    public class ShareOperationCommand : ServerCommand
    {
          //"a": "s",
          //"n": "WdVCnQjQ",
          //"o": "PbbMhTHDiYw",
          //"ok": "k1KdFWKRDqMq5u8WEM7oVg",
          //"ha": "kUteBC3xF8ZW_kx5bs-mOA",
          //"u": "D4TkyU-YG6w",
          //"key": "CACeq3Nu_RAZO9WUj97Hf53ErKpdFBw98aWFo_xNlguOyEMpmro0I-Z6N_qUP6nTrPgmhmdzeJGyQanV6OzLdZHtT3auCQ02C6V0BwD6rR9oV59MFqhHL4v_K1GtLtWxojgSckEgMV9MuGS8wwWwmex-QrQcdEfz9h8iU7K-xGUoASeCxw5h-NSScEnGQEW19HFxhqbjO_eVzbo1769HOQvXajNfc8AhD3XXBT_KzcrFE6jllQSGe5tLqBq2wNPuwM1xXamoyjXZ54GYQJBCh42YFweDcqiopgPU0cUEhTYoCHzrlQaT5n8tXmDvI2S7EbMLj2EokO77tDx2F7pEPqBv",
          //"r": 0,
          //"ts": 1360615411,
          //"i": "y2qZWBFEHJ"

        //or
  //      "a": "s",
  //"n": "oElXWJwA",
  //"key": "wOl_MefgpnVjEWoQz50mYQ"
    }

    public class CryptoRequestCommand : ServerCommand
    {
        //?
    }


}
