using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using MegaApi.Comms.Requests;
using System.Net;
using System.Timers;
using Newtonsoft.Json;
using MegaApi.Comms.Converters;
using System.Threading;

namespace MegaApi.Comms
{
    public class ServerRequestArgs : EventArgs
    {
        public List<ServerCommand> commands { get; set; }
    }

    internal class PollingTransport
    {
        static string ServerClientUrl = "https://g.api.mega.co.nz/sc";
        public event EventHandler<ServerRequestArgs> ServerCommand;
        WebClient ScWc = new WebClient();
        MegaUser user;
        public List<string> tracking;
        public PollingTransport(MegaUser user) { this.user = user; }
        public IWebProxy Proxy { get; set; }

        public void Cancel()
        {
            ScWc.CancelAsync();
        }
        public void StartPoll(MegaRequest cause, string handle)
        {
            ScWc = new WebClient();
            ScWc.DownloadStringCompleted += (s, e) =>
            {
                if (e.Cancelled) { return; }
                if (e.Error != null) { StartPoll(cause, handle); return; }
                try
                {
                    var response = String.Format("{{root:{0}}}", e.Result);
                    var r = JObject.Parse(response)["root"];
                    if (r["w"] != null)
                    {
                        StartWait(cause, handle, r["w"].ToString());
                        return;
                    }
                    else if (r["a"] != null)
                    {
                        if (ServerCommand != null)
                        {
                            Utility.Util.StartThread(() =>
                            {
                                var str = r["a"].ToString();
                                var cmds = JsonConvert.DeserializeObject<List<ServerCommand>>(str, new ServerCommandConverter(user));

                                foreach (var cmd in cmds)
                                {
                                    if (cmd.CommandId != null)
                                    {
                                        var track = tracking.Where(t => t == cmd.CommandId).FirstOrDefault();
                                        if (track != null)
                                        {
                                            tracking.Remove(track);
                                            cmd.IsMine = true;
                                        }
                                    }
                                }

                                ServerCommand(this, new ServerRequestArgs
                                {
                                    commands = cmds
                                });
                            }, "server_request_handling");
                        }
                        StartPoll(cause, r["sn"].ToString());
                    }
                }
                catch { StartPoll(cause, handle); }

            };


            try { ScWc.DownloadStringAsync(BuildScUri(cause, handle)); }
            catch (System.Net.WebException)
            {
                StartPoll(cause, handle);
            }

        }
        private Uri BuildScUri(MegaRequest req, string handle)
        {
            var url = ServerClientUrl;
            url += "?sn=" + handle;
            url += req.Sid != null ? "&sid=" + req.Sid : "";
            return new Uri(url);
        }

        int waitRetries = 0;

        private void StartWait(MegaRequest cause, string handle, string waitUrl)
        {
            if (waitRetries > 1)
            {
                Thread.Sleep((int)(Math.Pow(2, waitRetries) * 100));
                // 0,400,800,1600,3200ms etc
            }
            ScWc = new WebClient();
            ScWc.DownloadStringCompleted += (s, e) =>
            {
                if (e.Cancelled) { return; }
                // todo kinds of errors
                if (e.Error != null)
                {
                    waitRetries++;
                    StartWait(cause, handle, waitUrl); return;
                }
                waitRetries = 0;
                StartPoll(cause, handle);
            };
            try
            {
                ScWc.DownloadStringAsync(new Uri(waitUrl));

            }
            catch (WebException)
            {
                waitRetries++;
                StartWait(cause, handle, waitUrl);
            }

        }


    }
}
