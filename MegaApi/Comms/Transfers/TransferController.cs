using MegaApi.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace MegaApi.Comms.Transfers
{
    internal abstract class TransferController
    {
        protected Transport transport;
        int maxConnections;
        int maxThreads;
        volatile int connectionsCount = 0;
        volatile int threadsCount = 0;
        string tempPath;

        List<MegaChunk> transferQueue = new List<MegaChunk>();
        List<MegaChunk> cryptQueue = new List<MegaChunk>();

        public TransferController(Transport transport, string tempFolder = null, int maxConnections = 4, int maxThreads = 4)
        {
            tempPath = tempFolder == null ? Path.GetTempPath() : tempFolder;
            this.transport = transport;
            this.maxConnections = maxConnections;
            this.maxThreads = maxThreads;
        }
        public abstract void StartTransfer(TransferHandle handle);

        protected void EnqueueCrypt(List<MegaChunk> chunks)
        {
            lock (cryptQueue)
            {
                cryptQueue.AddRange(chunks);
            }
            CryptNext();
        }
        private void CryptNext()
        {
            List<MegaChunk> shedule;
            lock (cryptQueue)
            {
                if (threadsCount >= maxThreads) { return; }
                var count = Math.Min(maxThreads - threadsCount, cryptQueue.Count);
                shedule = new List<MegaChunk>(cryptQueue.Take(count).ToList());
                cryptQueue.RemoveRange(0, count);
            }

            shedule.ForEach((chunk) =>
            {
                ThreadPool.QueueUserWorkItem((a) =>
                {
                    threadsCount++;
                    CryptChunk(chunk);
                    threadsCount--;
                    CryptNext();
                });
            });
        }
        protected abstract void CryptChunk(MegaChunk chunk);
        protected void EnqueueTransfer(List<MegaChunk> chunks, bool firstPriority = false)
        {
            lock (transferQueue)
            {
                if (!firstPriority) { transferQueue.AddRange(chunks); }
                else { transferQueue.InsertRange(0, chunks); }
            }
            TransferNext();
        }
        private void TransferNext()
        {
            List<MegaChunk> shedule;
            lock (transferQueue)
            {
                if (connectionsCount >= maxConnections) { return; }
                var count = Math.Min(maxConnections - connectionsCount, transferQueue.Count);
                shedule = new List<MegaChunk>(transferQueue.Take(count).ToList());
                transferQueue.RemoveRange(0, count);
            }
            
            shedule.ForEach((chunk) =>
            {
                ThreadPool.QueueUserWorkItem((a) =>
                {
                    connectionsCount++;
                    TransferChunk(chunk, () =>
                        {
                            connectionsCount--;
                            TransferNext();
                        });
                });
            });
                
            
        }
        protected abstract void TransferChunk(MegaChunk megaChunk, Action transferCompleteFn);
        protected void OnUploadedBytes(MegaChunk chunk, long bytes)
        {
            var delta = bytes - chunk.transferredBytes;
            chunk.transferredBytes = bytes;
            chunk.Handle.BytesTransferred(delta);
        }
    }
}
