using System.Collections.Generic;

namespace KoikatsuCardReader.Core
{
    public class CardInfo
    {
        public string FileName { get; set; }

        public string FilePath { get; set; }

        /// <summary>Koikatsu / Koikatsu Sunshine. Null when the marker does not encode the game.</summary>
        public string Game { get; set; }

        /// <summary>Character / Coordinate / Scene. Null for unsupported files.</summary>
        public string Type { get; set; }

        /// <summary>Character names and sex joined into one string. Null for other card types.</summary>
        public string CharacterInfo { get; set; }

        /// <summary>Coordinate cards only. Null for other types.</summary>
        public string CoordinateName { get; set; }

        /// <summary>Extended data keys (plugin GUIDs), sorted.</summary>
        public List<string> Plugins { get; set; } = new List<string>();

        /// <summary>Zipmod GUIDs referenced through Sideloader, sorted and distinct.</summary>
        public List<string> Zipmods { get; set; } = new List<string>();

        /// <summary>Null when parsing succeeded.</summary>
        public string Error { get; set; }
    }
}
