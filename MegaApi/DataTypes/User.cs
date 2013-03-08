using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using MegaApi.Cryptography;
using MegaApi.Comms;

namespace MegaApi
{
    public class MegaUserStatus
    {
        // status: 0 - anonymous, 1 - email set, 2 - confirmed, but no RSA, 3 - complete
        public const int Anonymous = 0;
        public const int ConfirmationPending = 1;
        public const int EmailConfirmed = 2;
        public const int Complete = 3;
    }
    public class MegaUser
    {
        // used once to decrypt the masterkey
        public byte[] PassKey { get; set; }
        public SymmetricAlgorithm masterKeyAlg;
        public string Sid { get; set; }
        public string NodeSid { get; set; }
        // rsa
        public byte[] PrivateKey { get; set; }
        public byte[] PublicKey { get; set; }

        public int Status { get; set; }
        public string Email { get; set; }
        public string Id { get; set; }

        byte[] UserHash;
        // saved credentials
        public MegaUser(byte[] email, byte[] userHash, byte[] passKey)
        {
            Email = Encoding.UTF8.GetString(email).Replace("\0","");
            PassKey = passKey;
            UserHash = userHash;
        }

        // login
        public MegaUser(string email, string password)
        {
            Email = email;
            PassKey = Crypto.Hash(password);
        }
        // saved anon user with generated pw
        public MegaUser(byte[] userId, byte[] passKey)
        {
            Id = Transport.Encode(userId);
            PassKey = passKey;
        }
        // totally new user, generate keys
        public MegaUser()
        {
            var temp = Crypto.CreateAes();
            temp.GenerateKey();
            SetMasterKey(temp.Key);
            temp.GenerateKey();
            PassKey = temp.Key;
        }

        public byte[] GetIdBytes()
        {
            return Transport.Decode(Id);
        }
        public byte[] EncryptUserKey()
        {
            var aes = Crypto.CreateAes(PassKey);
            return Crypto.Encrypt(aes, masterKeyAlg.Key);
        }
        public void SetMasterKey(byte[] key)
        {
            masterKeyAlg = Crypto.CreateAes(key);
        }
        public byte[] GetHash()
        {
            if (UserHash!=null) { return UserHash; }
            if (Email == null) { return null; }
            UserHash = Crypto.Hash(Email, PassKey);
            return UserHash;
        }
    }
}
