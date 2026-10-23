using System.Collections.Generic;
using MessagePack;

namespace KoikatsuCardReader.Core.Models
{
    /// <summary>
    /// Block table stored after the card header. Mirrors KKManager's BlockHeader (keyAsPropertyName).
    /// </summary>
    [MessagePackObject]
    public class BlockHeader
    {
        [Key("lstInfo")]
        public List<BlockInfo> LstInfo { get; set; } = new List<BlockInfo>();

        public BlockInfo SearchInfo(string name) => LstInfo.Find(x => x.Name == name);

        [MessagePackObject]
        public class BlockInfo
        {
            [Key("name")]
            public string Name { get; set; }

            [Key("version")]
            public string Version { get; set; }

            [Key("pos")]
            public long Pos { get; set; }

            [Key("size")]
            public long Size { get; set; }
        }
    }
}
