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
    public abstract class TransferHandle : INotifyPropertyChanged
    {
        public Stream Stream;
        public event EventHandler StatusChanged;
        TransferHandleStatus _status;
        public TransferHandleStatus Status 
        {
            get { return _status; } 
            set
            {
                _status = value;
                NotifyPropertyChanged();
                if (StatusChanged != null) { StatusChanged(this, null); } 
            }
        }
        string tempPath;
        public string LocalFilename;
        public SymmetricAlgorithm AesAlg;
        public byte[] Mac;
        public List<MegaChunk> Chunks = new List<MegaChunk>();
        public int chunksProcessed = 0;
        public byte[] Nonce;
        public long Size;
        long processedBytes = 0;
        List<WebClient> runningConnections = new List<WebClient>();
        //Stopwatch timer = new Stopwatch();
        //long timerBytes = 0;
        //const int timerInterval = 500;

        public MegaNode Node { get; set; }
        public int? Error { get; set; }

        double _progress;
        public double Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                NotifyPropertyChanged();
            }
        }

        double? _speed;
        public double? Speed
        {
            get { return _speed; }
            set
            {
                _speed = value;
                NotifyPropertyChanged();
            }
        }
        
        public TransferHandle(string filename, long size, string tempPath)
        {
            this.tempPath = tempPath == null ? Path.GetTempPath() : tempPath;
            LocalFilename = filename;
            Status = TransferHandleStatus.Pending;
            Size = size;
            PrepareChunks(size);
        }

        public bool SkipChunks 
        { 
            get 
            {
                return Status == TransferHandleStatus.Cancelled || Status == TransferHandleStatus.Paused;
            } 
        }

        public void OnTransferredBytes(long bytes)
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
        protected void PrepareChunks(long filesize)
        {
            Size = filesize;
            var p = 0;
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
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void PauseTransfer()
        {
            Util.StartThread(() =>
            {
                Status = TransferHandleStatus.Paused;
                ResetConnections();
                Stream.Close();
            });
        }
        public virtual void ResumeTransfer()
        {

        }
        public void CancelTransfer()
        {
            Util.StartThread(() => CancelTransferInternal());
        }

        protected virtual void CancelTransferInternal()
        {
            Status = TransferHandleStatus.Cancelled;
            ResetConnections();
            ClearChunks();
            Stream.Close();
            Progress = 0;
            chunksProcessed = 0;
            processedBytes = 0;
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
        public virtual void OnStartedTransfer(WebClient wc)
        {
            //if (!timer.IsRunning) { timer.Start(); Speed = 0; }
            lock (runningConnections) { runningConnections.Add(wc); }
        }
        public void OnEndedTransfer(WebClient wc)
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
    }

    public class UploadHandle : TransferHandle
    {
        public string UploadUrl;
        public string UploadTargetNode;
        public byte[] NodeKeys;
        public byte[] UploadKey;
        public UploadHandle(string filename, string tempPath)
            : base(filename, new FileInfo(filename).Length, tempPath)
        {
            Stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public override void OnStartedTransfer(WebClient wc)
        {
            base.OnStartedTransfer(wc);
            Status = TransferHandleStatus.Uploading;
        }
    }

    public class DownloadHandle : TransferHandle
    {
        public string DownloadUrl { get; set; }
        public byte[] MacCheck;
        public string TempFile { get; private set; }
        public DownloadHandle(string filename, long filesize, string tempPath)
            : base(filename, filesize, tempPath) 
        {
            TempFile = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".mdwnl");
            Stream = File.Open(TempFile, FileMode.OpenOrCreate);
        }
        public override void OnStartedTransfer(WebClient wc)
        {
            base.OnStartedTransfer(wc);
            Status = TransferHandleStatus.Downloading;
        }

        protected override void CancelTransferInternal()
        {
            base.CancelTransferInternal();
            File.Delete(TempFile);
        }
    }
}
