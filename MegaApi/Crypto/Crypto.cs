using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Mono.Math;
using System.IO;
using MegaApi.Utility;

namespace MegaApi.Cryptography
{
    class Crypto
    {
        public static byte[] RandomKey(int length)
        {
            var alg = new RijndaelManaged();
            alg.KeySize = length * 8;
            alg.GenerateKey();
            return alg.Key;
        }
        public static SymmetricAlgorithm CreateAes()
        {
            var alg = new RijndaelManaged();
            alg.GenerateKey();
            alg.BlockSize = 128;
            alg.KeySize = 128;
            alg.Mode = CipherMode.ECB;
            alg.Padding = PaddingMode.None;
            return alg;
        }
        public static SymmetricAlgorithm CreateAes(byte[] key)
        {
            var alg = CreateAes();
            alg.Key = key;
            return alg;
        }

        public static byte[] EncryptCbc(byte[] key, byte[] data)
        {

            var result = new byte[data.Length];
            var c = new AesCryptoServiceProvider();
            c.Key = key;
            c.IV = new byte[16];
            c.Mode = CipherMode.CBC;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.BlockSize = 128;
                aesAlg.KeySize = 128;
                aesAlg.Key = key;
                aesAlg.IV = new byte[16];
                aesAlg.Padding = PaddingMode.None;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream(result))
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }
                }

            }
            return result;

        }
        public static byte[] DecryptCbc(byte[] key, byte[] data)
        {
            var result = new byte[data.Length];
            var c = new AesCryptoServiceProvider();
            c.Key = key;
            c.IV = new byte[16];
            c.Mode = CipherMode.CBC;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.BlockSize = 128;
                aesAlg.KeySize = 128;
                aesAlg.Key = key;
                aesAlg.IV = new byte[16];
                aesAlg.Padding = PaddingMode.None;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(data))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        csDecrypt.Read(result, 0, result.Length);
                    }
                }
            }
            return result;
        }

        public static byte[] Decrypt(SymmetricAlgorithm alg, byte[] data, ICryptoTransform decryptor = null)
        {
            var dec = decryptor == null ? alg.CreateDecryptor() : decryptor;
            var decrypted = new byte[data.Length];
            dec.TransformBlock(data, 0, data.Length, decrypted, 0);
            return decrypted;
        }

        public static byte[] Encrypt(SymmetricAlgorithm alg, byte[] data, ICryptoTransform encryptor = null)
        {
            var enc = encryptor == null ? alg.CreateEncryptor() : encryptor;
            var encrypted = new byte[data.Length];
            enc.TransformBlock(data, 0, data.Length, encrypted, 0);
            return encrypted;
        }

        static byte[] Stringhash(string s, SymmetricAlgorithm aes)
        {
            var hash = new byte[16];
            var sBytes = ConvertString(s);

            for (var i = 0; i < sBytes.Length; i++) { hash[i & 15] ^= sBytes[i]; }
            for (var i = 0; i < 16384; i++) { hash = Encrypt(aes, hash); }
            var result = new byte[8];
            Array.Copy(hash, 0, result, 0, 4);
            Array.Copy(hash, 8, result, 4, 4);
            return result;
        }
        public static byte[] Hash(string value, byte[] key = null)
        {
            if (key == null) { return PrepareKey(ConvertString(value)); }
            return Stringhash(value, CreateAes(key));
        }

        static byte[] ConvertString(string input)
        {
            var size = (input.Length + 3) >> 2;
            var intRes = new int[size];
            var chars = input.ToCharArray();
            for (var i = 0; i < input.Length; i++)
            {
                intRes[i >> 2] |= ((int)chars[i] << (24 - (i & 3) * 8));
            }
            return Converter.IntsToBytes(intRes);
        }

        static byte[] PrepareKey(byte[] key)
        {
            if (key.Length % 4 != 0) { throw new Exception("Invalid key passed to PrepareKey"); }
            var pkey = Converter.IntsToBytes(new int[] { unchecked((int)0x93C467E3), 0x7DB0C7A4, unchecked((int)0xD1BE3F81), 0x0152CB56 });
            var tempKey = new byte[16];
            for (var r = 0; r < 65536; r++)
            {
                for (var i = 0; i < key.Length; i += 16)
                {
                    Array.Copy(key, i, tempKey, 0,
                        (key.Length - i > 16 ? 16 : key.Length - i));

                    var aes = CreateAes(tempKey);
                    pkey = Encrypt(aes, pkey);
                }

            }
            return pkey;
        }

        public static byte[] EncryptCtr(SymmetricAlgorithm aes, byte[] data, byte[] nonce, long offset)
        {
            var encryptor = aes.CreateEncryptor();
            // to test with 50 gb
            // todo change to uint64?
            var counter = new byte[16];
            Array.Copy(nonce, counter, 8);
            var n1 = offset / 0x1000000000;
            var n1b = BitConverter.GetBytes(n1).Reverse().ToArray();
            var n2 = offset / 0x10;
            var n2b = BitConverter.GetBytes(n2).Reverse().ToArray();
            Array.Copy(n1b, 4, counter, 8, 4);
            Array.Copy(n2b, 4, counter, 12, 4);

            var mac = new byte[16];
            var enc = new byte[16];
            Array.Copy(counter, mac, 8);
            Array.Copy(counter, 0, mac, 8, 8);

            var len = data.Length - 15;
            var i = 0;
            for (; i < len; i += 16)
            {
                mac.XorWith(0, data, i, 16);
                mac = Crypto.Encrypt(aes, mac, encryptor);
                enc = Crypto.Encrypt(aes, counter, encryptor);
                data.XorWith(i, enc, 0, 16);
                Byte16Inc(counter);
            }

            if (i < data.Length)
            {
                var v = new byte[16];
                Array.Copy(data, i, v, 0, data.Length - i);
                mac = mac.Xor(v);
                mac = Crypto.Encrypt(aes, mac, encryptor);
                enc = Crypto.Encrypt(aes, counter, encryptor);
                v = v.Xor(enc);
                Array.Copy(v, 0, data, i, data.Length - i);
            }

            return mac;
        }

        public static byte[] DecryptCtr(SymmetricAlgorithm aes, byte[] data, byte[] nonce, long offset)
        {
            var encryptor = aes.CreateEncryptor();
            var counter = new byte[16];
            Array.Copy(nonce, counter, 8);
            var n1 = offset / 0x1000000000;
            var n1b = BitConverter.GetBytes(n1).Reverse().ToArray();
            var n2 = offset / 0x10;
            var n2b = BitConverter.GetBytes(n2).Reverse().ToArray();
            Array.Copy(n1b, 4, counter, 8, 4);
            Array.Copy(n2b, 4, counter, 12, 4);

            var mac = new byte[16];
            var enc = new byte[16];
            Array.Copy(counter, mac, 8);
            Array.Copy(counter, 0, mac, 8, 8);

            var len = data.Length - 15;
            var i = 0;
            for (; i < len; i += 16)
            {
                enc = Crypto.Encrypt(aes, counter, encryptor);
                data.XorWith(i, enc, 0, 16);
                mac.XorWith(0, data, i, 16);
                mac = Crypto.Encrypt(aes, mac, encryptor);

                Byte16Inc(counter);
            }

            if (i < data.Length)
            {
                var v = new byte[16];
                Array.Copy(data, i, v, 0, data.Length - i);

                enc = Crypto.Encrypt(aes, counter, encryptor);
                v = v.Xor(enc);

                var j = data.Length & 15;
                var m = new byte[16];

                m.Fill((byte)0xff, 0, j);
                m.Fill((byte)0, j, 16 - j);

                v = v.And(m);
                mac = mac.Xor(v);
                mac = Crypto.Encrypt(aes, mac, encryptor);
                Array.Copy(v, 0, data, i, data.Length - i);
            }

            return mac;
        }

        public static void Byte16Inc(byte[] src)
        {
            src[15]++;
            if (src[15] == 0)
            {
                src[14]++;
                if (src[14] == 0)
                {
                    src[13]++;
                    if (src[13] == 0)
                    {
                        src[12]++;
                        if (src[12] == 0)
                        {
                            src[11]++;
                            if (src[11] == 0)
                            {
                                src[10]++;
                                if (src[10] == 0)
                                {
                                    src[9]++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
