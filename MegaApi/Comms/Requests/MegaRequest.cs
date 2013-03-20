using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using MegaApi.Cryptography;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading;
using MegaApi.Utility;

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
    internal abstract class MegaRequest : IMegaRequest
    {
        public ManualResetEvent ResetEvent { get; set; }
        public virtual bool IsTracking { get; set; }
        public int Id { get; protected set; }
        public string Sid { get; set; }
        public string NodeSid { get; protected set; }
        public List<JsonConverter> Converters = new List<JsonConverter>();
        public int retries = 0;
        public event EventHandler<MegaResponseError> Error;

        public abstract void HandleSuccess(JToken response);

        public void HandleError(int errno)
        {
            if (Error != null)
            {
                Util.StartThread(() => 
                {
                    Error(this, new MegaResponseError { Error = errno });
                    ResetEvent.Set();
                }, "request_error_handler");
            }
            else
            {
                ResetEvent.Set();
            }
        }
    }
    internal abstract class MegaRequest<T> : MegaRequest where T : MegaResponse
    {
        public event EventHandler<T> Success;

        protected void CallSuccessHandler(T args)
        {
            if (Success != null)
            {
                Util.StartThread(() => 
                { 
                    Success(this, args);
                    ResetEvent.Set();
                }, "request_success_handler");
            }
            else
            {
                ResetEvent.Set();
            }
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

    internal abstract class TrackingRequest<T> : MegaRequest<T>, ITrackingRequest where T : MegaResponse
    {
        public override bool IsTracking { get; set; }
        [DataMember]
        [JsonProperty("i")]
        public string TrackingId { get; set; }

        public TrackingRequest(MegaUser user) : base (user)
        {
            IsTracking = true;
            TrackingId = Util.RandomString(10);
        }
    }
    
    /// <summary>
    /// Exposing from api functions to the end users 
    /// </summary>
    public interface IMegaRequest
    {
        ManualResetEvent ResetEvent { get; }
    }

    /// <summary>
    /// The dummy request for returning in the case of error for sync-way compatibility
    /// </summary>
    internal class EmptyRequest : IMegaRequest
    {
        public ManualResetEvent ResetEvent { get; private set; }
        public EmptyRequest() { ResetEvent = new ManualResetEvent(true); }
    }


}