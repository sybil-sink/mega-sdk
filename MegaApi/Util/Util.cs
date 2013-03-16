using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MegaApi.Utility
{
    public static class Util
    {
        public static uint SwapEndianness(uint x)
        {
            return ((x & 0x000000ff) << 24) +
                   ((x & 0x0000ff00) << 8) +
                   ((x & 0x00ff0000) >> 8) +
                   ((x & 0xff000000) >> 24);
        }

        public static string RandomString(int size, int moreRandom = 0)
        {
            string strPwdchar = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string strPwd = "";
            var len = strPwdchar.Length;
            Random rnd = new Random((int)DateTime.Now.Ticks + moreRandom);
            for (int i = 0; i <= size; i++)
            {
                int iRandom = rnd.Next(0, len - 1);
                strPwd += strPwdchar.Substring(iRandom, 1);
            }
            return strPwd;
        }

        public static void Fill<T>(this T[] arr, T value, int start, int length)
        {
            for (int i = start; i < length; i++)
            {
                arr[i] = value;
            }
        }

        public static byte[] And(this byte[] arg1, byte[] arg2)
        {
            var result = new byte[arg1.Length];
            for (int i = 0; i < arg1.Length; i++)
            {
                result[i] = (byte)(arg1[i] & arg2[i]);
            }
            return result;
        }

        public static byte[] Xor(this byte[] arg1, byte[] arg2)
        {
            var result = new byte[arg1.Length];
            for (var i = 0; i < arg1.Length; i++)
            {
                result[i] = (byte)(arg1[i] ^ arg2[i]);
            }
            return result;
        }

        // get the fuckin' rid of this!!!111eleveneleven
        public static byte[] XorWith(this byte[] modifiable, int modifiableIndex, byte[] modifyer, int modifyerIndex, int count)
        {
            for (var i = 0; i < count; i++)
            {
                modifiable[modifiableIndex + i] ^= modifyer[modifyerIndex + i];
            }
            return modifiable;
        }

        public static Thread StartThread(Action action, string name)
        {
            var t = new Thread(new ThreadStart(action));
            t.Name = name;
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            return t;
        }
    }
}
