using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Math;
using MegaApi.Utility;

namespace MegaApi
{
    /// <summary>
    /// mega's js byte handling
    /// </summary>
    public static class Converter
    {
        //public static int[] encrypt_key(int[] key, int[] data)
        //{
        //    var alg = MegaCrypto.CreateAes(intsToBytes(key));
        //    var dataBuf = intsToBytes(data);
        //    return bytesToInts(MegaCrypto.Encrypt(alg, dataBuf));
        //}

        //public static int[] decrypt_key(int[] key, int[] data)
        //{
        //    var dataBuf = intsToBytes(data);
        //    var alg = MegaCrypto.CreateAes(intsToBytes(key));
        //    return bytesToInts(MegaCrypto.Decrypt(alg, dataBuf));
        //}

        //public static string encode(int[] data)
        //{
        //    return MegaTransport.Encode(intsToBytes(data));
        //}
        //public static int[] decode(string data)
        //{
        //    return bytesToInts(Convert.FromBase64String(data));
        //}

        // to big endian ints used in mega
        public static int[] BytesToInts(byte[] bytes)
        {
            var result = new int[(int)Math.Ceiling(bytes.Length / 4.0f)];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            for (var i = 0; i < result.Length; i++)
                result[i] = (int)Util.SwapEndianness((uint)result[i]);
            return result;
        }

        public static byte[] IntsToBytes(int[] ints)
        {
            var newInts = new int[ints.Length];
            Array.Copy(ints, newInts, ints.Length);
            for (var i = 0; i < newInts.Length; i++)
                newInts[i] = (int)Util.SwapEndianness((uint)newInts[i]);
            var result = new byte[newInts.Length * 4];
            Buffer.BlockCopy(newInts, 0, result, 0, result.Length);
            return result;
        }
 
        //mpi: 2 octets with length in bits + octets in big endian order (rsa.js)
        // for debugging
        public static BigInteger MpiToBigInt(byte[] mpi)
        {
            var bytesCount = mpi.Length - 2;
            var shift = 0;
            //if ((mpi[2] & 0x80) > 0)
            //{
            //    bytesCount++;
            //    shift++;
            //}
            var init = new byte[bytesCount];
            Array.Copy(mpi, 2, init, shift, mpi.Length - 2);
            //init = init.Reverse().ToArray();
            return new BigInteger(init);
        }

        
        public static BigInteger MpiToBigInt(byte[] src, int offset, int count)
        {
            var bytesCount = count - 2;
            var shift = 0;
            //if ((src[offset+2] & 0x80) > 0)
            //{
            //    // make the number positive
            //    bytesCount++;
            //    shift++;
            //}

            var init = new byte[bytesCount];
            Array.Copy(src, offset + 2, init, shift, count - 2);
            //init = init.Reverse().ToArray();
            return new BigInteger(init);
        }

        public static byte[] BigIntToMpi(BigInteger bint)
        {
            var ibytes = bint.GetBytes();//.Reverse().ToArray();
            var shift = 0;// ibytes[0] == 0 ? 1 : 0;
            var length = ibytes.Length - shift;
            var bytes = new byte[length + 2];
            ushort ulength = Convert.ToUInt16(length * 8);
            bytes[0] = (byte)(ulength >> 8);
            bytes[1] = (byte)(ulength & 0xff);
            Array.Copy(ibytes, shift, bytes, 2, length);
            return bytes;
        }

    }
}
