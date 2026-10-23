using System.Collections.Generic;
using MessagePack;

namespace KoikatsuCardReader.Core.Models
{
    /// <summary>
    /// Sideloader ResolveInfo / StudioResolveInfo. Only the mod GUID is read, other keys are skipped.
    /// </summary>
    [MessagePackObject]
    public class ResolveInfo
    {
        /// <summary>GUID of the zipmod as defined in manifest.xml.</summary>
        [Key("ModID")]
        public string GUID { get; set; }
    }

    /// <summary>
    /// Sideloader StudioPatternResolveInfo. Only the mod GUIDs are read.
    /// </summary>
    [MessagePackObject]
    public class StudioPatternResolveInfo
    {
        [Key("ObjectPatternInfo")]
        public Dictionary<int, PatternInfo> ObjectPatternInfo { get; set; }

        [MessagePackObject]
        public class PatternInfo
        {
            /// <summary>GUID of the zipmod as defined in manifest.xml.</summary>
            [Key("GUID")]
            public string GUID { get; set; }
        }
    }
}
