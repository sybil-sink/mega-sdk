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
using MegaApi.Utility;

namespace MegaApi
{
    /// <summary>
    /// The main class for api methods
    /// </summary>
    public class Mega
    {
        public event EventHandler<ServerRequestArgs> ServerRequest;
        /// <summary>
        /// credentials
        /// </summary>
        public MegaUser User
        {
            get { return _user; }
            private set
            {
                uploader.User = transport.Auth = _user = value;
            }
        }
        MegaUser _user;

        MegaDownloader downloader;
        MegaUploader uploader;
        Transport transport;

        #region static initialization
        public static void Init(MegaUser user, Action<Mega> OnSuccess, Action<int> OnError)
        {
            if (OnSuccess == null || OnError == null)
            {
                throw new ArgumentException("Login handlers can not be empty");
            }
            Util.StartThread(() => ThreadInit(user, OnSuccess, OnError), "mega_api_login");
        }
        public Mega InitSync(MegaUser user)
        {
            Mega api = null;
            int? error = null;
            ThreadInit(user, (m) => api = m, (i) => error = i).ResetEvent.WaitOne();
            if (api == null || error != null)
            {
                throw new MegaApiException((int)error, "Login error");
            }
            return api;
        }

        private static IMegaRequest ThreadInit(MegaUser user, Action<Mega> OnSuccess, Action<int> OnError)
        {
            var initTransport = new Transport();
            var mega = new Mega(initTransport);

            // mandatory anonymous registration
            if (user == null || (user.Email == null && user.Id == null))
            {
                user = new MegaUser();
                var req = new MRequestCreateAnon<MResponseCreateAnon>(user);
                int? error = null;
                req.Success += (s, args) => user.Id = args.UserId;
                req.Error += (s, e) => error = e.Error;
                initTransport.EnqueueRequest(req);
                req.ResetEvent.WaitOne();
                if (error != null)
                {
                    OnError((int)error);
                    // just to have the ResetEvent for async compatibility
                    return new EmptyRequest();
                }
            }

            // set the credentials
            mega.User = user;

            // the login itself
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
                getUserRequest.Error += (s2, e) =>
                {
                    OnError(e.Error);
                    sidRequest.ResetEvent.Set();
                };
                initTransport.EnqueueRequest(getUserRequest);
                getUserRequest.ResetEvent.WaitOne();
            };
            sidRequest.Error += (s, a) => OnError(a.Error);

            initTransport.EnqueueRequest(sidRequest);
            return sidRequest;
        }
        #endregion
        private Mega(Transport t)
        {
            transport = t;
            transport.Proxy = WebRequest.GetSystemWebProxy();
            transport.ServerRequest += (s, e) => { if (ServerRequest != null) { ServerRequest(s, e); } };
            downloader = new MegaDownloader(transport);
            uploader = new MegaUploader(transport);
        }
        public void Register(string email, string password, 
            Action OnSuccess, Action<int> OnError)
        {
            throw new NotImplementedException();
        }
        public IMegaRequest GetNodes(Action<List<MegaNode>> OnSuccess, Action<int> OnError)
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
        public List<MegaNode> GetNodesSync()
        {
            List<MegaNode> result = null;
            int? error = null;
            GetNodes((l) => result = l, (e) => error = e).ResetEvent.WaitOne();
            if (result == null || error != null)
            {
                throw new MegaApiException((int)error, "Could not get the list of nodes");
            }
            return result;
        }
        public IMegaRequest UploadFile(string targetNodeId, string filename, Action<UploadHandle> OnHandleReady, Action<int> OnError)
        {
            Stream stream = null;
            long fs = 0;
            try
            {
                stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs = new FileInfo(filename).Length;
            }
            catch
            {
                if (OnError != null) { OnError(MegaApiError.ESYSTEM); }
                return new EmptyRequest();
            }
            return UploadStream(targetNodeId, Path.GetFileName(filename), fs, stream, OnHandleReady, OnError);
        }
        public IMegaRequest UploadStream(string targetNodeId, string name, long fileSize, Stream inputStream, Action<UploadHandle> OnHandleReady, Action<int> OnError)
        {
            if (fileSize == 0 || string.IsNullOrEmpty(targetNodeId) || inputStream == null || !inputStream.CanSeek)
            {
                if (OnError != null) { OnError(MegaApiError.EWRONG); }
                return new EmptyRequest();
            }

            var req = new MRequestGetUploadUrl<MResponseGetUploadUrl>(User, fileSize);
            UploadHandle handle = null;
            req.Success += (s, a) =>
            {
                //Console.WriteLine("got url");
                handle = MegaUploader.GetHandle(inputStream, name, fileSize, targetNodeId, a.Url);
                if (OnHandleReady != null) { Util.StartThread(()=> OnHandleReady(handle), "transfer_handle_ready_handler"); }
                uploader.StartTransfer(handle);
            };
            req.Error += (s, e) =>
            {
                if (OnError != null) { OnError(e.Error); }
            };
            transport.EnqueueRequest(req);
            return req;
        }
        public MegaNode UploadFileSync(string targetNodeId, string filename)
        {
            Stream stream = null;
            long fs = 0;
            try
            {
                stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs = new FileInfo(filename).Length;
            }
            catch(Exception e)
            {
                throw new MegaApiException(MegaApiError.ESYSTEM, "Could not upload the file", e);
            }
            return UploadStreamSync(targetNodeId, filename, stream, fs);
        }
        public MegaNode UploadStreamSync(string targetNodeId, string name, Stream inputStream, long size)
        {
            int? error = null;
            TransferHandle handle = null;
            var waitFile = new ManualResetEvent(false);
            UploadStream(targetNodeId, name,  size, inputStream,
                (h) => 
                {
                    handle = h;
                    h.TransferEnded += (s, e) => waitFile.Set();
                },
                (e) => error = e)
                .ResetEvent.WaitOne();
            waitFile.WaitOne();
            if (handle == null || error != null || handle.Node == null || handle.Error != null)
            {
                throw new MegaApiException(handle.Error == null ? (int)error : (int)handle.Error, "Could not upload the file");
            }
            return handle.Node;
        }
        public IMegaRequest DownloadFile(MegaNode node, string filename, Action<DownloadHandle> OnHandleReady, Action<int> OnError)
        {
            var req = new MRequestGetDownloadUrl<MResponseGetDownloadUrl>(User, node.Id);
            DownloadHandle handle = null;
            req.Success += (s, a) =>
            {
                handle = MegaDownloader.GetHandle(filename, a.FileSize, a.Url, node);
                if (OnHandleReady != null) { Util.StartThread(() => OnHandleReady(handle), "transfer_handle_ready_handler"); }
                downloader.StartTransfer(handle);
            };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }
        public void DownloadFileSync(MegaNode node, string filename)
        {
            int? error = null;
            TransferHandle handle = null;
            var waitFile = new ManualResetEvent(false);
            DownloadFile(node, filename,
                (h) =>
                {
                    handle = h;
                    h.TransferEnded += (s, e) => waitFile.Set();
                },
                (e) => error = e)
                .ResetEvent.WaitOne();
            waitFile.WaitOne();
            if (handle == null || error != null || handle.Node == null || handle.Error != null)
            {
                throw new MegaApiException(handle.Error == null ? (int)error : (int)handle.Error, "Could not upload the file");
            }
        }
        public IMegaRequest CreateFolder(string targetNodeId, string folderName, Action<MegaNode> OnSuccess, Action<int> OnError)
        {
            if (string.IsNullOrEmpty(targetNodeId) || string.IsNullOrEmpty(folderName))
            {
                if (OnError != null) { OnError(MegaApiError.EWRONG); }
                return new EmptyRequest();
            }
            var req = new MRequestCreateFolder<MResponseCreateFolder>(User, folderName, targetNodeId);
            req.Success += (s, e) => { if (OnSuccess != null) { OnSuccess(e.Created.First());} };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }

        void CreateFolders(
            MegaNode targetNode, 
            List<MegaNode> existingNodes, 
            string[] folders,
            Action<MegaNode> OnSuccess, 
            Action<int> OnError)
        {
            if(folders.Length == 0)
            {
                OnSuccess(targetNode);
                return;
            }
            MegaNode existing;
            lock (existingNodes)
            {
                existing = existingNodes.Where(n => n.ParentId == targetNode.Id && n.Attributes.Name == folders[0]).FirstOrDefault();
            }
            if (existing != null)
            {
                CreateFolders(
                        existing,
                        existingNodes,
                        folders.Skip(1).ToArray(),
                        OnSuccess,
                        OnError);
            }
            else
            {
                CreateFolder(targetNode.Id, folders[0],
                    (n) => 
                        {
                            lock (existingNodes) { existingNodes.Add(n); }
                            CreateFolders(
                            n,
                            existingNodes,
                            folders.Skip(1).ToArray(),
                            OnSuccess,
                            OnError);
                        },
                    (i) => OnError(i));
            }
        }
        public IMegaRequest CreateFolder(MegaNode targetNode, List<MegaNode> existingNodes, string folderPath, char separator, Action<MegaNode> OnSuccess, Action<int> OnError)
        {
            var result = new EmptyRequest();
            if (string.IsNullOrEmpty(folderPath))
            {
                OnSuccess(targetNode);
                return result;
            }
            result.ResetEvent.Reset();
            var folders = folderPath.Split(new char[]{separator}, StringSplitOptions.RemoveEmptyEntries);
            CreateFolders(
                targetNode, 
                existingNodes,
                folders, 
                (node)=>{OnSuccess(node); result.ResetEvent.Set();},
                (i) => { OnError(i); result.ResetEvent.Set(); });
            return result;
        }
        public MegaNode CreateFolderSync(MegaNode target, List<MegaNode> nodes, string folder, char separator)
        {
            MegaNode result = null;
            int? errno = null;
            CreateFolder(target, nodes, folder, separator, (n) => result = n, (e) => errno = e).ResetEvent.WaitOne();
            if (result == null || errno != null)
            {
                throw new MegaApiException((int)errno, String.Format("Could not create the folder {0}",folder));
            }
            return result;
        }

        public MegaNode CreateFolderSync(string targetNode, string folderName)
        {
            MegaNode folder = null;
            int? error = null;
            CreateFolder(targetNode, folderName, (n) => folder = n, (e) => error=e).ResetEvent.WaitOne();
            if (folder == null || error != null)
            {
                throw new MegaApiException((int)error, "Could not create the folder");
            }
            return folder;
        }
        public IMegaRequest RemoveNode(string targetNodeId, Action OnSuccess, Action<int> OnError)
        {
            if (string.IsNullOrEmpty(targetNodeId))
            {
                if (OnError != null) { OnError(MegaApiError.EWRONG); }
                return new EmptyRequest();
            }
            var req = new MRequestRemoveNode<MResponseRemoveNode>(User, targetNodeId);
            req.Success += (s, e) => { if (OnSuccess != null) { OnSuccess(); } };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }
        public void RemoveNodeSync(string targetNodeId)
        {
            int? error = null;
            RemoveNode(targetNodeId, null, (e) => error = e).ResetEvent.WaitOne();
            if (error != null)
            {
                throw new MegaApiException((int)error, "Could not remove the node");
            }
        }
        public IMegaRequest MoveNode(string nodeId, string targetNodeId, Action OnSuccess, Action<int> OnError)
        {
            if (string.IsNullOrEmpty(targetNodeId) || string.IsNullOrEmpty(nodeId))
            {
                if (OnError != null) { OnError(MegaApiError.EWRONG); }
                return new EmptyRequest();
            }
            var req = new MRequestMoveNode<MResponseMoveNode>(User, nodeId, targetNodeId);
            req.Success += (s, e) => { if (OnSuccess != null) { OnSuccess(); } };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }
        public void MoveNodeSync(string nodeId, string targetNodeId)
        {
            int? error = null;
            MoveNode(nodeId, targetNodeId, null, (e) => error = e).ResetEvent.WaitOne();
            if (error != null)
            {
                throw new MegaApiException((int)error, "Could not move the node");
            }
        }
        public IMegaRequest UpdateNodeAttr(MegaNode node, Action OnSuccess, Action<int> OnError)
        {
            if (node==null || node.Attributes == null)
            {
                if (OnError != null) { OnError(MegaApiError.EWRONG); }
                return new EmptyRequest();
            }
            var req = new MRequestUpdateAttributes<MResponseUpdateAttributes>(User, node);
            req.Success += (s, e) => { if (OnSuccess != null) { OnSuccess(); } };
            req.Error += (s, e) => { if (OnError != null) { OnError(e.Error); } };
            transport.EnqueueRequest(req);
            return req;
        }
        public void UpdateNodeAttrSync(MegaNode node)
        {
            int? error = null;
            UpdateNodeAttr(node, null, (e) => error = e).ResetEvent.WaitOne();
            if (error != null)
            {
                throw new MegaApiException((int)error, "Could not update the node");
            }
        }
        #region storing the user credentials
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

        #endregion

        
    }
}
