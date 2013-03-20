using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using MegaApi.DataTypes;
using System.ComponentModel;
using System.Net;
using System.Diagnostics;
using MegaApi.Utility;
using System.Threading;

namespace MegaApi
{
    public enum TransferHandleStatus
    {
        Pending,
        Uploading,
        Downloading,
        Success,
        Error,
        Cancelled,
        Paused
    }
    public class TransferEndedArgs : EventArgs
    {
        public int? Error { get; set; }
    }

    public abstract class TransferHandle : INotifyPropertyChanged
    {
        public event EventHandler<TransferEndedArgs> TransferEnded;
        public event PropertyChangedEventHandler PropertyChanged;

        public MegaNode Node { get; internal set; }
        public int? Error { get; private set; }
        public TransferHandleStatus Status 
        {
            get { return _status; } 
            protected set
            {
                _status = value;
                if (value == TransferHandleStatus.Success || value == TransferHandleStatus.Error)
                {
                    Progress = 100;
                }
                OnPropertyChanged("Status");
            }
        }
        public double Progress
        {
            get { return _progress; }
            private set
            {
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }
        public double? Speed
        {
            get { return _speed; }
            private set
            {
                _speed = value;
                OnPropertyChanged("Speed");
            }
        }

        internal bool SkipChunks
        {
            get
            {
                return Status == TransferHandleStatus.Cancelled || Status == TransferHandleStatus.Paused;
            }
        }
        internal string Name;
        internal Stream Stream;
        internal SymmetricAlgorithm AesAlg;
        internal byte[] Mac;
        internal List<MegaChunk> Chunks = new List<MegaChunk>();
        internal volatile int chunksProcessed = 0;
        internal byte[] Nonce;
        internal long Size;
        
        TransferHandleStatus _status;
        string tempPath;
        long processedBytes = 0;
        List<WebClient> runningConnections = new List<WebClient>();
        double _progress;
        double? _speed;
        
        // filename is without the directory path
        internal TransferHandle(string filename, long size, string tempPath)
        {
            this.tempPath = tempPath == null ? Path.GetTempPath() : tempPath;
            Name = filename;
            Status = TransferHandleStatus.Pending;
            Size = size;
            PrepareChunks(size);
        }

        public virtual void PauseTransfer()
        {
            //Util.StartThread(() =>
            //{
            Status = TransferHandleStatus.Paused;
            ResetConnections();
            Stream.Close();
            // });
        }
        public virtual void ResumeTransfer()
        {

        }
        public void CancelTransfer()
        {
            Util.StartThread(() => CancelTransferInternal(null), "mega_api_transfer_cancel");
        }
        
        internal void EndTransfer(int? error)
        {
            Progress = 100;
            if (error == null)
            {
                Status = TransferHandleStatus.Success;
            }
            else
            {
                Status = TransferHandleStatus.Error;
                Error = error;
            }
            OnTransferEnded();
        }
        internal void CancelTransfer(int? error)
        {
            CancelTransferInternal(error);
        }
        internal virtual void ChunkTransferStarted(WebClient wc)
        {
            //if (!timer.IsRunning) { timer.Start(); Speed = 0; }
            lock (runningConnections) { runningConnections.Add(wc); }
        }
        internal void ChunkTransferEnded(WebClient wc)
        {
            lock (runningConnections) { runningConnections.Remove(wc); }
            //if (runningConnections.Count < 1) 
            //{ 
            //    timer.Reset(); 
            //    timerBytes = 0;
            //    Progress = 100;
            //    Speed = null;
            //}
        }
        internal void BytesTransferred(long bytes)
        {
            processedBytes += bytes;
            if (processedBytes < 0) { processedBytes = 0; }
            //timerBytes += bytes;
            Progress = 100 * (double)processedBytes / Size;
            //// mb per seconds
            //if (timer.ElapsedMilliseconds > timerInterval) 
            //{
            //    Speed = timerBytes / 1048.576 / timer.ElapsedMilliseconds;
            //    timer.Restart();
            //    timerBytes = 0;
            //}


        }

        protected virtual void CancelTransferInternal(int? error)
        {
            Error = error;
            Status = TransferHandleStatus.Cancelled;
            ResetConnections();
            ClearChunks();
            Stream.Close();
            Progress = 0;
            chunksProcessed = 0;
            processedBytes = 0;
        }
        private void PrepareChunks(long filesize)
        {
            Size = filesize;
            long p = 0;
            long pp = 0;
            for (var i = 1; i <= 8 && p < Size - i * 131072; i++)
            {
                var s = i * 131072;
                Chunks.Add(new MegaChunk(tempPath)
                {
                    Handle = this,
                    Size = s,
                    Offset = p
                });
                pp = p;
                p += s;
            }

            while (p < Size)
            {
                Chunks.Add(new MegaChunk(tempPath)
                {
                    Handle = this,
                    Size = 1048576,
                    Offset = p
                });
                pp = p;
                p += 1048576;
            }

            if (Size - pp > 0)
            {
                Chunks.Last().Size = (int)(Size - pp);
            }

        }
        private void ResetConnections()
        {
            lock (runningConnections) { runningConnections.ForEach((c) => c.CancelAsync()); }
        }
        private void ClearChunks()
        {
            lock (Chunks)
            {
                Chunks.ForEach((c) => c.ClearData());
            }
        }
        private void OnPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                ThreadPool.QueueUserWorkItem((e) =>PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }
        private void OnTransferEnded()
        {
            if (TransferEnded != null)
            {
                // already in the end of transfer_finish_file thread
                TransferEnded(this, new TransferEndedArgs());
            }
        }
    }

    public class UploadHandle : TransferHandle
    {
        internal string UploadUrl;
        internal string UploadTargetNode;
        internal byte[] NodeKeys;
        internal byte[] UploadKey;
        internal UploadHandle(Stream stream, string name, long fileSize, string tempPath)
            : base(name, fileSize, tempPath)
        {
            Stream = stream;
        }
        internal override void ChunkTransferStarted(WebClient wc)
        {
            base.ChunkTransferStarted(wc);
            Status = TransferHandleStatus.Uploading;
        }
    }

    public class DownloadHandle : TransferHandle
    {
        internal string DownloadUrl { get; set; }
        internal string TargetPath { get; set; }
        internal byte[] MacCheck;
        internal string TempFile { get; private set; }
        internal DownloadHandle(string filename, long filesize, string tempPath)
            : base(Path.GetFileName(filename), filesize, tempPath) 
        {
            TargetPath = filename;
            TempFile = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".mdwnl");
            Stream = File.Open(TempFile, FileMode.OpenOrCreate);
        }
        internal DownloadHandle(Stream targetStream, long filesize, string tempPath)
            : base(null, filesize, tempPath)
        {
            Stream = targetStream;
        }
        internal override void ChunkTransferStarted(WebClient wc)
        {
            base.ChunkTransferStarted(wc);
            Status = TransferHandleStatus.Downloading;
        }
        protected override void CancelTransferInternal(int? error)
        {
            base.CancelTransferInternal(error);
            File.Delete(TempFile);
        }
    }
}
