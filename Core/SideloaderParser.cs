using System.Collections;
using System.Collections.Generic;
using KoikatsuCardReader.Core.Models;
using MessagePack;

namespace KoikatsuCardReader.Core
{
    /// <summary>
    /// Extracts zipmod GUIDs from Sideloader extended data (AGENTS.md 2.4).
    /// </summary>
    public static class SideloaderParser
    {
        /// <summary>Extended data ID used by Sideloader.</summary>
        public const string UniversalAutoResolverId = "com.bepis.sideloader.universalautoresolver";

        /// <summary>Character and coordinate cards: list of MessagePack ResolveInfo.</summary>
        private const string CardInfoKey = "info";

        /// <summary>Scenes: list of MessagePack ResolveInfo for scene objects.</summary>
        private const string SceneItemInfoKey = "itemInfo";

        /// <summary>Scenes: list of MessagePack StudioPatternResolveInfo for item patterns.</summary>
        private const string ScenePatternInfoKey = "patternInfo";

        /// <summary>Scenes: mod GUID of the scene filter (plain string).</summary>
        private const string SceneFilterGUIDKey = "filterInfoGUID";

        /// <summary>Scenes: mod GUID of the scene ramp / shadow type (plain string).</summary>
        private const string SceneRampGUIDKey = "rampInfoGUID";

        /// <summary>Scenes: mod GUID of the scene map (plain string).</summary>
        private const string SceneMapGUIDKey = "mapInfoGUID";

        /// <summary>Scenes: mod GUID of the scene BGM (plain string).</summary>
        private const string SceneBGMGUIDKey = "bgmInfoGUID";

        public static bool IsSideloaderKey(string key) => key == UniversalAutoResolverId;

        /// <summary>
        /// Returns the distinct zipmod GUIDs referenced by a Sideloader entry.
        /// Reads "info" (character / coordinate) and, for scenes, "itemInfo", "patternInfo",
        /// "filterInfoGUID", "rampInfoGUID", "mapInfoGUID" and "bgmInfoGUID".
        /// Keys that are absent are skipped.
        /// </summary>
        public static List<string> ExtractZipmodGUIDs(PluginData pluginData)
        {
            var guids = new List<string>();
            if (pluginData?.Data == null) return guids;

            AddResolveGUIDs(pluginData.Data, CardInfoKey, guids);
            AddResolveGUIDs(pluginData.Data, SceneItemInfoKey, guids);
            AddPatternGUIDs(pluginData.Data, ScenePatternInfoKey, guids);
            AddPlainGUID(pluginData.Data, SceneFilterGUIDKey, guids);
            AddPlainGUID(pluginData.Data, SceneRampGUIDKey, guids);
            AddPlainGUID(pluginData.Data, SceneMapGUIDKey, guids);
            AddPlainGUID(pluginData.Data, SceneBGMGUIDKey, guids);

            return guids;
        }

        private static void AddResolveGUIDs(Dictionary<string, object> data, string key, List<string> guids)
        {
            if (!data.TryGetValue(key, out var value) || value == null) return;

            foreach (var item in (IEnumerable)value)
            {
                if (!(item is byte[] bytes)) continue;

                var resolveInfo = MessagePackSerializer.Deserialize<ResolveInfo>(bytes);
                AddGUID(resolveInfo?.GUID, guids);
            }
        }

        private static void AddPatternGUIDs(Dictionary<string, object> data, string key, List<string> guids)
        {
            if (!data.TryGetValue(key, out var value) || value == null) return;

            foreach (var item in (IEnumerable)value)
            {
                if (!(item is byte[] bytes)) continue;

                var patternData = MessagePackSerializer.Deserialize<StudioPatternResolveInfo>(bytes);
                if (patternData?.ObjectPatternInfo == null) continue;

                foreach (var pattern in patternData.ObjectPatternInfo.Values)
                    AddGUID(pattern?.GUID, guids);
            }
        }

        private static void AddPlainGUID(Dictionary<string, object> data, string key, List<string> guids)
        {
            if (!data.TryGetValue(key, out var value)) return;
            AddGUID(value as string, guids);
        }

        private static void AddGUID(string guid, List<string> guids)
        {
            guid = guid?.Trim();
            if (string.IsNullOrEmpty(guid)) return;
            if (!guids.Contains(guid)) guids.Add(guid);
        }
    }
}
