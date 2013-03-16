using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MegaApi.Comms.Requests;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MegaApi.Comms.Requests
{
    public class MResponseRemoveNode : MegaResponse
    {
        public bool Ok = true;
    }

    internal class MRequestRemoveNode<T> : TrackingRequest<T> where T : MResponseRemoveNode
    {
        [DataMember]
        public string a = "d";
        [DataMember]
        public string n;
        public MRequestRemoveNode(MegaUser user, string nodeId) : base(user) { n = nodeId; }
        public override void HandleSuccess(Newtonsoft.Json.Linq.JToken response)
        {
            if (response.Type == JTokenType.Integer && response.ToObject<int>() == 0)
            {
                CallSuccessHandler((T)new MResponseRemoveNode());
            }
            else
            {
                HandleError(MegaApiError.EUNEXPECTED);
            }
        }
    }
}
