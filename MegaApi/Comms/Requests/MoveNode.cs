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
    public class MResponseMoveNode : MegaResponse
    {
        public bool Ok = true;
    }

    internal class MRequestMoveNode<T> : TrackingRequest<T> where T : MResponseMoveNode
    {
        [DataMember]
        public string a = "m";
        [DataMember]
        public string n;
        [DataMember]
        public string t;

        public MRequestMoveNode(MegaUser user, string nodeId, string targetNodeId) : base(user) { n = nodeId; t = targetNodeId; }
        public override void HandleSuccess(Newtonsoft.Json.Linq.JToken response)
        {
            if (response.Type == JTokenType.Integer && response.ToObject<int>() == 0)
            {
                CallSuccessHandler((T)new MResponseMoveNode());
            }
            else
            {
                HandleError(MegaApiError.EUNEXPECTED);
            }
        }
    }
}
