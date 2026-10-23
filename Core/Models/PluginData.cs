using System.Collections.Generic;
using MessagePack;

namespace KoikatsuCardReader.Core.Models
{
    /// <summary>
    /// One entry of the extended save dictionary (KKManager PluginData).
    /// </summary>
    [MessagePackObject]
    public class PluginData
    {
        [Key(1)]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
