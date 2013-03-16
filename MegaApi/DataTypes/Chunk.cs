using MegaApi.Comms.Transfers;
using System;
using System.Diagnostics;
using System.IO;
namespace MegaApi.DataTypes
{
    internal class MegaChunk
    {
        public TransferHandle Handle;
        public byte[] Mac;
        public byte[] Data
        {
            get
            {
                return File.ReadAllBytes(filename);
            }
            set
            {
                File.WriteAllBytes(filename, value);
            }
        }
        public long Offset;
        public int Size;
        public long transferredBytes;
        string filename;

        public MegaChunk(string tempPath)
        {
            filename = Path.Combine(tempPath, Guid.NewGuid().ToString("N").Substring(3,7).ToLower());
        }
        public void ClearData()
        {
            if (File.Exists(filename)) { File.Delete(filename); }
        }
    }
}
