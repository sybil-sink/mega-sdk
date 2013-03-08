using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using Mono.Math;
using System.Threading;
using MegaApi.Comms;
using MegaApi.Comms.Requests;
using MegaApi.DataTypes;
using MegaApi.Comms.Transfers;

namespace MegaApi
{
    /// <summary>
    /// The main class for api methods
    /// </summary>
    public class Mega
    {
        

        MegaUser _user;
        public MegaUser User
        {
            get { return _user; }
            private set 
            {
                uploader.User = transport.Auth = _user = value; 
            }
        }

        MegaDownloader downloader;
        MegaUploader uploader;
        Transport transport;

        public event EventHandler<ServerRequestArgs> ServerRequest;
        private Mega(Transport t)
        {
            transport = t;
            transport.Proxy = WebRequest.GetSystemWebProxy();
            transport.ServerRequest += (s, e) => { if (ServerRequest != null) { ServerRequest(s, e); } };
            downloader = new MegaDownloader(transport);
            uploader = new MegaUploader(transport);
        }

        

        public static Mega Init(MegaUser user, Action<Mega> OnSuccess, Action<int> OnError)
        {
            var trpt = new Transport();
            var mega = new Mega(trpt);

            if (user == null || (user.Email == null && user.Id == null))
            {
                user = new MegaUser();
                var req = new MRequestCreateAnon<MResponseCreateAnon>(user);
                req.Success += (s, args) => user.Id = args.UserId;
                req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
                trpt.EnqueueRequest(req);
                req.ResetEvent.WaitOne();
            }

            mega.User = user;

            var sidRequest = new MRequestGetSid<MResponseGetSid>(mega.User);
            sidRequest.Success += (s, args) =>
            {
                mega.User.Sid = args.SessionId;
                mega.User.SetMasterKey(args.MasterKey.Value);

                var getUserRequest = new MRequestGetUser<MResponseGetUser>(mega.User);
                getUserRequest.Success += (s2, args2) =>
                {
                    mega.User.Status = args2.UserStatus;
                    mega.User.Email = args2.Email;
                    mega.User.Id = args2.UserId;
                    OnSuccess(mega);
                };
                trpt.EnqueueRequest(getUserRequest);
            };

            sidRequest.Error += (s, a) => 
            {
                if (OnError != null)
                {
                    OnError(a.Error);
                }
            };
            trpt.EnqueueRequest(sidRequest);

            return mega;
        }



        public void Register(string email, string password, 
            Action OnSuccess, Action<int> OnError)
        {
            throw new NotImplementedException();
        }

        public MegaRequest GetNodes(Action<List<MegaNode>> OnSuccess, Action<int> OnError)
        {
            var filesRequest = new MRequestGetFiles<MResponseGetFiles>(User);
            filesRequest.Success += (s, a) => 
            {
                transport.StartPoll(filesRequest, a.SCid);
                if (OnSuccess != null) { OnSuccess(a.Nodes); } 
            };
            filesRequest.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(filesRequest);
            return filesRequest;
        }

        public void UploadFile(string targetNode, string filename, Action<UploadHandle> OnHandleReady, Action<int> OnError)
        {
            var fs = new FileInfo(filename).Length;
            var req = new MRequestGetUploadUrl<MResponseGetUploadUrl>(User, fs);
            req.Success += (s, a) =>
            {
                var handle = uploader.UploadFile(filename, targetNode, a.Url);
                OnHandleReady(handle);
            };
            req.Error += (s, e) => 
            {
                if (OnError != null) { OnError(e.Error); } 
            };
            transport.EnqueueRequest(req);
        }

        public void DownloadFile(MegaNode node, string filename, Action<DownloadHandle> OnHandleReady, Action<int> OnError)
        {
            var req = new MRequestGetDownloadUrl<MResponseGetDownloadUrl>(User, node.Id);
            req.Success += (s, a) =>
            {
                var handle = downloader.DownloadFile(filename, a.FileSize, a.Url, node);
                OnHandleReady(handle);
            };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
        }
        public MegaNode CreateFolder(string targetNode, string folderName)
        {
            MegaNode folder = null;
            var mre = new ManualResetEvent(false);
            var r = CreateFolder(targetNode, folderName, (n) => folder = n, (i) => {});
            r.ResetEvent.WaitOne();
            return folder;
        }

        public MegaRequest CreateFolder(string targetNode, string folderName, Action<MegaNode> OnSuccess, Action<int> OnError)
        {
            var req = new MRequestCreateFolder<MResponseCreateFolder>(User, folderName, targetNode);
            req.Success += (s, e) => { OnSuccess(e.Created.First()); };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }

        public MegaRequest RemoveNode(MegaNode targetNode, Action OnSuccess, Action<int> OnError)
        {
            return RemoveNode(targetNode.Id, OnSuccess, OnError);
        }

        public MegaRequest RemoveNode(string targetNodeId, Action OnSuccess, Action<int> OnError)
        {
            var req = new MRequestRemoveNode<MResponseRemoveNode>(User, targetNodeId);
            req.Success += (s, e) => { OnSuccess(); };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }

        public MegaRequest MoveNode(string nodeId, string targetNodeId, Action OnSuccess, Action<int> OnError)
        {
            var req = new MRequestMoveNode<MResponseMoveNode>(User, nodeId, targetNodeId);
            req.Success += (s, e) => { OnSuccess(); };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }

        public MegaRequest UpdateNodeAttr(MegaNode node, Action OnSuccess, Action<int> OnError)
        {
            var req = new MRequestUpdateAttributes<MResponseUpdateAttributes>(User, node);
            req.Success += (s, e) => { OnSuccess(); };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }
        
        public static MegaUser LoadAccount(string filePath)
        {
            MegaUser u = null;
            try
            {
                var encrypted = File.ReadAllBytes(filePath);

                var decrypted = ProtectedData.Unprotect(encrypted, keyStoreSeed,
                    DataProtectionScope.CurrentUser);

                // registered user
                if (decrypted[0] == 1)
                {
                    u = new MegaUser(
                        email: decrypted.Skip(24 + 1).Take(decrypted.Length - 24 + 1).ToArray(),
                        userHash: decrypted.Skip(1).Take(8).ToArray(),
                        passKey: decrypted.Skip(8 + 1).Take(16).ToArray()
                        );
                }
                else // anon saved user
                {
                    u = new MegaUser(
                        userId: decrypted.Skip(16 + 1).Take(8).ToArray(),
                        passKey: decrypted.Skip(1).Take(16).ToArray());
                }
            }
            catch
            {
                return null;
            }
            return u;
        }
        public void SaveAccount(string filePath)
        {
            byte[] decrypted;
            if (!string.IsNullOrEmpty(User.Email))
            {
                var emailBytes = Encoding.UTF8.GetBytes(User.Email);
                var hashBytes = User.GetHash();
                decrypted = new byte[emailBytes.Length + hashBytes.Length + User.PassKey.Length + 1];
                decrypted[0] = 1;
                Array.Copy(hashBytes, 0, decrypted, 1, hashBytes.Length);
                Array.Copy(User.PassKey, 0, decrypted, hashBytes.Length + 1, User.PassKey.Length);
                Array.Copy(emailBytes, 0, decrypted, User.PassKey.Length + hashBytes.Length + 1, emailBytes.Length);
            }
            else
            {
                var idBytes = User.GetIdBytes();
                decrypted = new byte[idBytes.Length + User.PassKey.Length + 1];
                decrypted[0] = 0;
                Array.Copy(User.PassKey, 0, decrypted, 1, User.PassKey.Length);
                Array.Copy(idBytes, 0, decrypted, User.PassKey.Length + 1, idBytes.Length);
            }
            var encrypted = ProtectedData.Protect(decrypted, keyStoreSeed,
                    DataProtectionScope.CurrentUser);

            File.WriteAllBytes(filePath, encrypted);
        }

        private static readonly byte[] keyStoreSeed = new byte[]{
            0xFD, 0xDF, 0xF7, 0xA2, 0x06, 0x14, 0x47, 0x20, 
            0xAE, 0x9E, 0x9A, 0x49, 0x6B, 0x8E, 0x0A, 0x13
        };
    }
}
