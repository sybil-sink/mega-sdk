using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using MegaApi.Cryptography;
using System.Net;
using System.IO;
using MegaApi.Utility;
using MegaApi.DataTypes;
using MegaApi.Comms;
using System.Threading;

namespace MegaApi.Comms.Transfers
{
    class MegaDownloader : TransferController
    {


        public MegaDownloader(Transport transport, string tempFolder = null, int maxConnections = 4, int maxThreads = 4)
            : base(transport, tempFolder, maxConnections, maxThreads) { }
       
        

        protected override void TransferChunk(MegaChunk chunk, Action transferCompleteFn)
        {
            if (chunk.Handle.SkipChunks) { transferCompleteFn(); return; }
            var wc = new WebClient();
            wc.Proxy = transport.Proxy;
            chunk.Handle.OnStartedTransfer(wc);
            wc.DownloadDataCompleted += (s, e) =>
            {
                transferCompleteFn();
                chunk.Handle.OnEndedTransfer(wc);
                if (e.Cancelled) { return; }
                if (e.Error != null)
                {
                    chunk.Handle.OnTransferredBytes(0 - chunk.transferredBytes);
                    EnqueueTransfer(new List<MegaChunk> { chunk }, true);
                }
                else 
                {
                    chunk.Data = e.Result;
                    EnqueueCrypt(new List<MegaChunk> { chunk });
                }
            };
            wc.DownloadProgressChanged += (s, e) => OnDownloadedBytes(chunk, e.BytesReceived);
            var url = ((DownloadHandle)chunk.Handle).DownloadUrl;
            url += String.Format("/{0}-{1}", chunk.Offset, chunk.Offset + chunk.Size - 1);
            wc.DownloadDataAsync(new Uri(url));

        }

        protected void OnDownloadedBytes(MegaChunk chunk, long bytes)
        {
            var delta = bytes - chunk.transferredBytes;
            chunk.transferredBytes = bytes;
            chunk.Handle.OnTransferredBytes(delta);
        }

        private void FinishFile(DownloadHandle handle)
        {
            if (handle.Status == TransferHandleStatus.Cancelled) { return; }
            handle.Stream.Close();
            handle.Mac = new byte[16];
            foreach (var chunk in handle.Chunks)
            {
                handle.Mac = handle.Mac.Xor(chunk.Mac);
                handle.Mac = Crypto.Encrypt(handle.AesAlg, handle.Mac);
            }

            var check = new byte[8];
            handle.Mac.XorWith(0, handle.Mac, 4, 4);
            handle.Mac.XorWith(8, handle.Mac, 12, 4);
            Array.Copy(handle.Mac, 0, check, 0, 4);
            Array.Copy(handle.Mac, 8, check, 4, 4);

            var ok = check.SequenceEqual(handle.MacCheck);
            if (ok) 
            {
                File.Move(handle.TempFile, handle.LocalFilename);
                handle.Status = TransferHandleStatus.Success;
            }
            else { handle.Error = MegaApiError.EKEY; handle.Status = TransferHandleStatus.Error; }
        }


        public DownloadHandle DownloadFile(string filename, long filesize, string downloadUrl, MegaNode node, string tempPath = null)
        {
            var handle = new DownloadHandle(filename, filesize, tempPath);
            handle.DownloadUrl = downloadUrl;
            handle.Node = node;

            byte[] dlKey = new byte[handle.Node.NodeKey.DecryptedKey.Length];
            Array.Copy(handle.Node.NodeKey.DecryptedKey, dlKey, dlKey.Length);
            if (dlKey.Length > 16) { dlKey.XorWith(0, dlKey, 16, 16); }
            var aesKey = new byte[16];
            Array.Copy(dlKey, aesKey, 16);
            handle.AesAlg = Crypto.CreateAes(aesKey);

            handle.Nonce = new byte[8];
            Array.Copy(dlKey, 16, handle.Nonce, 0, 8);

            handle.MacCheck = new byte[8];
            Array.Copy(dlKey, 24, handle.MacCheck, 0, 8);

            EnqueueTransfer(handle.Chunks);

            return handle;
        }

        protected override void CryptChunk(MegaChunk chunk)
        {
            var data = chunk.Data;
            var hndl = chunk.Handle;
            chunk.Mac = Crypto.DecryptCtr(chunk.Handle.AesAlg, data, chunk.Handle.Nonce, chunk.Offset);
            hndl.Stream.Seek(chunk.Offset, SeekOrigin.Begin);
            hndl.Stream.Write(data, 0, chunk.Size);
            hndl.chunksProcessed++;
            chunk.ClearData();

            if (chunk.Handle.chunksProcessed == chunk.Handle.Chunks.Count)
            {
                FinishFile((DownloadHandle)chunk.Handle);
            }
        }

        
    }
}
