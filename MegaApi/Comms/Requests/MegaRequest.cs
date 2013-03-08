using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using MegaApi.Cryptography;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using MegaApi.Utility;
using System.Threading;

namespace MegaApi.Comms.Requests
{
    public abstract class MegaResponse : EventArgs
    {
    }
    public class MegaResponseError : EventArgs
    {
        public int Error { get; set; }
    }
    [DataContract]
    public abstract class MegaRequest
    {
        public ManualResetEvent ResetEvent { get; set; }
        public virtual bool IsTrackig { get; set; }
        public int Id { get; protected set; }
        public string Sid { get; set; }
        public string NodeSid { get; protected set; }
        public List<JsonConverter> Converters = new List<JsonConverter>();
        public int retries = 0;
        public event EventHandler<MegaResponseError> Error;

        public abstract void HandleSuccess(JToken response);

        public void HandleError(int errno)
        {
            if (Error != null) { Error(this, new MegaResponseError { Error = errno }); }
            ResetEvent.Set();
        }
    }
    public abstract class MegaRequest<T> : MegaRequest where T : MegaResponse
    {
        public event EventHandler<T> Success;

        protected void CallSuccessHandler(T args)
        {
            if (Success != null)
            {
                Success(this, args);
            }
            ResetEvent.Set();
        }
        public override void HandleSuccess(JToken response)
        {
            CallSuccessHandler(response.ToObject<T>());
        }

        public MegaRequest(MegaUser user)
        {
            ResetEvent = new ManualResetEvent(false);
            Id = new Random().Next();
            if (user != null)
            {
                Sid = user.Sid;
                NodeSid = user.NodeSid;
            }
        }
    }

    public interface ITrackingRequest
    {
        string TrackingId { get; set; }
    }

    public abstract class TrackingRequest<T> : MegaRequest<T>, ITrackingRequest where T : MegaResponse
    {
        public override bool IsTrackig { get; set; }
        [DataMember]
        [JsonProperty("i")]
        public string TrackingId { get; set; }

        public TrackingRequest(MegaUser user) : base (user)
        {
            IsTrackig = true;
            TrackingId = Util.RandomString(10);
        }
    }
    


}