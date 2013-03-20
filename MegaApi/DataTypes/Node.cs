using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using System.Security.Cryptography;
using MegaApi.Comms;
using MegaApi.Cryptography;
using MegaApi.Utility;
using MegaApi.DataTypes;
using MegaApi.Comms.Converters;

namespace MegaApi
{
    public class MegaNodeType
    {
        public const int File = 0;
        public const int Folder = 1;
        public const int RootFolder = 2;
        public const int Inbox = 3;
        public const int Trash = 4;
        public const int Dummy = -1;
    }

    public interface IEncryptableAttributes
    {
        NodeKeys NodeKey { get; set; }
        byte[] encryptedAttributes { get; set; }
        NodeAttributes Attributes { get; set; }
    }

    public class MegaNode : IEncryptableAttributes
    {
        [JsonProperty("t")]
        public int Type { get; set; }

        [JsonProperty("h")]
        public string Id { get; set; }

        [JsonProperty("p")]
        public string ParentId { get; set; }

        [JsonProperty("a")]
        [JsonConverter(typeof(ByteConverter))]
        public byte[] encryptedAttributes { get; set; }

        [JsonProperty("k")]
        public NodeKeys NodeKey { get; set; }
        
        [JsonProperty("u")]
        public string UserId { get; set; }

        [JsonProperty("s")]
        public long? Size { get; set; }

        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty("ts")]
        public DateTime? Timestamp { get; set; }

        [JsonIgnore] // todo Name through getter/setter?
        public NodeAttributes Attributes { get; set; }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (encryptedAttributes == null || encryptedAttributes.Length < 1)
            {
                Attributes = new NodeAttributes();
                switch (Type)
                {
                    case MegaNodeType.Trash: Attributes.Name = "Trash"; break;
                    case MegaNodeType.Inbox: Attributes.Name = "Inbox"; break;
                    case MegaNodeType.RootFolder: Attributes.Name = "Root"; break;
                }
                return;
            }

            DecryptAttrs(this);
        }

        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            EncryptAttrs(this);
            ParentId = null;
        }

        public static void DecryptAttrs(IEncryptableAttributes node)
        {
            var nodeKey = node.NodeKey.DecryptedKey;
            if (nodeKey == null) { node.Attributes = new NodeAttributes(); return; }

            var key2 = new byte[16];
            
            // do we need to reduce
            var r = nodeKey.Length > key2.Length;
            for (var i = 0; i < 16; i++)
            {
                key2[i] = r ? (byte)(nodeKey[i] ^ nodeKey[i + 16]) : nodeKey[i];
            }
            var attrBytes = Crypto.DecryptCbc(key2, node.encryptedAttributes);
            var attrString = Encoding.UTF8.GetString(attrBytes)
                .Replace("\0", String.Empty)
                .Replace("MEGA", String.Empty);
            if (string.IsNullOrEmpty(attrString)) { return; }
            try { node.Attributes = JsonConvert.DeserializeObject<NodeAttributes>(attrString); }
            catch (JsonException) { node.Attributes = new NodeAttributes { Name = "error_loading_attrs" }; }
        }

        public static void EncryptAttrs(IEncryptableAttributes node)
        {
            var nodeKey = node.NodeKey.DecryptedKey;
            var attrs = JsonConvert.SerializeObject(node.Attributes);
            var attr = Encoding.UTF8.GetBytes("MEGA" + attrs);
            var needsPadding = attr.Length % 16 != 0;
            var pAttr = new byte[needsPadding ? attr.Length + 16 - (attr.Length % 16) : attr.Length];

            Array.Copy(attr, pAttr, attr.Length);

            var newKey = new byte[nodeKey.Length];
            Array.Copy(nodeKey, newKey, nodeKey.Length);

            if (newKey.Length > 16) { newKey.XorWith(0, newKey, 16, 16); }
            var aes_key = new byte[16];
            Array.Copy(newKey, aes_key, 16);

            node.encryptedAttributes = Crypto.EncryptCbc(aes_key, pAttr);
        }

        public override string ToString()
        {
            string name = string.Empty;
            return String.Format("{0}: {1}", Id, Attributes.Name);
        }

    }
}
