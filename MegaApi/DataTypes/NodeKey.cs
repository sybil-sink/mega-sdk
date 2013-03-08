using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaApi.Cryptography;

namespace MegaApi.DataTypes
{
    public class NodeKeys
    {
        public Dictionary<string, byte[]> Keys { get; set; }
        public byte[] DecryptedKey { get; set; }
        public byte[] EncryptedKey { get; set; }

        public NodeKeys(){ }
        public NodeKeys(byte[] key, MegaUser user)
        {
            DecryptedKey = key;
            EncryptedKey = Crypto.Encrypt(user.masterKeyAlg, key); 
            Keys = new Dictionary<string,byte[]> { {user.Id, EncryptedKey} };
        }
    }
}
