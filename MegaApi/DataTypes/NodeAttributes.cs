using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MegaApi.DataTypes
{
    public class NodeAttributes
    {
        // todo more attributes
        [JsonProperty("n")]
        public string Name { get; set; }
    }
}
