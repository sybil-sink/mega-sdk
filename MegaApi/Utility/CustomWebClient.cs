using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MegaApi.Utility
{
    public class CustomWC : WebClient
    {
        public CustomWC(bool keepalive = true, int timeout = -1)
        {
            this.keepalive = keepalive;
            this.timeout = timeout;
        }
        protected override WebRequest GetWebRequest(Uri address)
        {
            var br = base.GetWebRequest(address);
            ((HttpWebRequest)br).KeepAlive = keepalive;
            if (timeout > 0)
            {
                br.Timeout = timeout;
            }
            return br;
        }

        public bool keepalive { get; set; }

        public int timeout { get; set; }
    }
}
