namespace KoikatsuCardReader.Core
{
    /// <summary>
    /// Card marker constants and game / card type names.
    /// Coordinate cards do not encode the game, so their game is null.
    /// Scenes have no leading marker; they are identified by the trailing marker.
    /// </summary>
    public static class CardMarker
    {
        public const string Koikatsu = "Koikatsu";
        public const string Sunshine = "Koikatsu Sunshine";

        public const string Character = "Character";
        public const string Coordinate = "Coordinate";
        public const string Scene = "Scene";

        public const string CharacterMarker = "【KoiKatuChara】";
        public const string CharacterPartyMarker = "【KoiKatuCharaS】";
        public const string CharacterPartySpecialPatchMarker = "【KoiKatuCharaSP】";
        public const string CharacterSunshineMarker = "【KoiKatuCharaSun】";
        public const string CoordinateMarker = "【KoiKatuClothes】";

        /// <summary>Marker written at the end of the scene body.</summary>
        public const string SceneMarker = "【KStudio】";
    }
}
