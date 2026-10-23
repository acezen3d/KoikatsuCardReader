# References

External projects consulted while implementing the parsers. They are used to verify the on-disk formats only; no code from them is included in this repository.

## 1. KKManager

https://github.com/IllusionMods/KKManager

Primary reference for character cards, coordinate cards, extended data and Sideloader.

| File | Used for |
|---|---|
| `src/KKManager.Core/Data/Cards/CardLoader.cs` | Marker dispatch. Scenes (`【KStudio】`) are a TODO there. |
| `src/KKManager.Core/Data/Cards/KK/KoiCard.cs` | Character card read order. |
| `src/KKManager.Core/Data/Cards/KK/KoiCoordCard.cs` | Coordinate read order: marker, version, name, outfit bytes, `KKEx`. |
| `src/KKManager.Core/Data/Cards/KKS/KoiSunCard.cs`, `KKS/ChaFileParameter.cs` | Sunshine differences. |
| `src/KKManager.Core/Data/Cards/Utility.cs` | `IEND` search. |
| `src/KKManager.Core/Data/ExtData/Deserializers/SideloaderExtDataDeserializer.cs` | `ResolveInfo.ModID`. |

## 2. KoikatuCharaLoader

https://github.com/great-majority/KoikatuCharaLoader

Python library. Complete reader/writer for character cards, coordinate cards and scenes, including a field-by-field description of the scene layout. Check its LICENSE before reuse.

| File | Used for |
|---|---|
| `kkloader/KoikatuSceneData.py` | Scene read order: version string, object dictionary, map and lighting settings, background, frame, then `tail` (`【KStudio】`, 1-byte length prefix), then mod data (`KKEx`, `int32` version, `int32` length, MessagePack). |
| `kkloader/KoikatuCharaData.py` | `CoordinateEntry` header and payload (product number, header, version, coordinate name, payload length, clothes / accessory / makeup). The `KKEx` class shows how nested MessagePack fields are handled. |
| `kkloader/funcs.py` | `read_lstinfo_blocks`, `msg_unpack` and other low-level helpers. |

Note: `CoordinateEntry.save` does not write the trailing `KKEx` block, so it cannot be used to verify coordinate extended data. This project follows KKManager for that and confirms it against sample files.

## 3. koikatu.js

https://github.com/great-majority/koikatu.js

TypeScript implementation by the same author as KoikatuCharaLoader. Used to cross-check the scene read order.

| File | Used for |
|---|---|
| `src/scene/kk.ts` | `parseKkScene`, scene read order and version branches (`0.0.2`, `0.0.4`, `0.0.5`, `1.1.0.0`, `1.1.2.0`). |
| `src/parse/scanPng.ts` | PNG chunk walk to locate the end of `IEND`. Same goal as `PngUtil`. |

## 4. Checked but not used

| Project | Finding |
|---|---|
| https://github.com/im-mi/KoiCatalog | Card manager. No usable scene or coordinate parsing found. |
| https://github.com/FairBear/KK_Wardrobe | Plugin for runtime outfit changes. Does not deal with file formats. |
| https://github.com/dumbiart666-png/KK-Studio-Mods | CharaStudio plugins. No file format parsing found. |

## 5. Scene Sideloader keys

None of the references above document the scene Sideloader keys. They were determined from the sample scenes:

- `itemInfo`: list of MessagePack `ResolveInfo`, `ModID`.
- `patternInfo`: list of MessagePack `StudioPatternResolveInfo`, `ObjectPatternInfo[*].GUID`.
- `filterInfoGUID`, `rampInfoGUID`, `mapInfoGUID`, `bgmInfoGUID`: plain GUID strings.

## 6. Other links
- https://github.com/IllusionMods/BepisPlugins/tree/master/src/Core_Sideloader_Studio/UniversalAutoResolver
- https://github.com/great-majority/koikatu.js
- https://pypi.org/project/kkloader/
- https://github.com/FairBear/KK_Wardrobe
- https://github.com/im-mi/KoiCatalog
- https://github.com/great-majority/KoikatuCharaLoader
- https://github.com/DaniJani2025/koikatsu-card-sorter
