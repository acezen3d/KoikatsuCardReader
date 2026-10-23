# KoikatsuCardReader - Project Guide (AGENTS.md)

Last updated: 2026-10-24 21:47:32

This document describes the project as currently implemented. Read it before making changes.

---

## 1. Project overview

A C# Windows Forms application (.NET Framework 4.8) that recursively scans a folder for Koikatsu PNG files and reports the plugin and zipmod dependencies of every card, coordinate and scene.

Two JSON-related features exist:

- A details tree in the UI listing plugins and zipmods of the selected file, or of the whole scan.
- Export of that list to a JSON file.

The tool is strictly read-only with respect to game data. Cards are opened with `FileShare.Read` and never modified, moved or deleted.
The only files the program writes are the user-requested JSON export and the `ZipmodMap_*.csv` cache next to the executable.

---

## 2. Type model

Each parsed file gets a `Game` and a `Type`. Unsupported PNG files are dropped from the result list and only counted.

### 2.1 Types (`CardInfo.Type`)

| Value | Meaning |
|---|---|
| `Character` | Character card: KK, Party or Sunshine |
| `Coordinate` | Outfit / coordinate card |
| `Scene` | CharaStudio scene |
| `null` | Unsupported; dropped from results |

### 2.2 Games (`CardInfo.Game`)

| Value | Marker |
|---|---|
| `Koikatsu` | `【KoiKatuChara】`, `【KoiKatuCharaS】`, `【KoiKatuCharaSP】` |
| `Koikatsu Sunshine` | `【KoiKatuCharaSun】` |
| `null` | Coordinate and Scene, because their format does not encode the game |

Koikatsu Party is reported as `Koikatsu`; the marker is not preserved as a separate game value (see section 14).

### 2.3 Markers and length prefixes

Markers are stored as length-prefixed UTF-8 strings. The prefix byte is the byte count, not the character count.

| Marker | Prefix | Total bytes |
|---|---|---|
| `【KoiKatuChara】` | 0x12 | 18 |
| `【KoiKatuCharaS】` | 0x13 | 19 |
| `【KoiKatuCharaSP】` | 0x14 | 20 |
| `【KoiKatuCharaSun】` | 0x15 | 21 |
| `【KoiKatuClothes】` | 0x14 | 20 |
| `【KStudio】` | 0x0D | 13 |

---

## 3. File formats

All three types are PNG images with game data appended after the PNG `IEND` chunk.
`IEND` chunk type and CRC: `49 45 4E 44 AE 42 60 82`. The payload begins immediately after those 8 bytes.

Offsets below were measured on real files in `Examples/`, not inferred.

### 3.1 Character cards

Example: `KoikatsuSun_F_20250901005002404_Az Chika.png`, file length 344688, PNG end 143390.

```
PNG data ... IEND
int32   loadProductNo        = 100 (not used)
str     marker               e.g. 【KoiKatuCharaSun】
str     loadVersion          e.g. "0.0.0" (not used)
int32   faceLength           length of the face thumbnail
bytes   faceThumbnail        skipped
int32   blockHeaderLength
bytes   BlockHeader          MessagePack
int64   placeholder          0
-------- data base (base = position right after the int64) --------
block N is at [base + pos .. base + pos + size]
```

`BlockHeader` is a MessagePack map with one key `lstInfo`, a list of entries:

```
{ name: string, version: string, pos: long, size: long }
```

Block names in practice: `Custom`, `Coordinate`, `Parameter`, `Status`, `About` (Sunshine only), `KKEx`.
`lstInfo` order is not the physical file order; always seek with `pos`.

Measured table:

```
KKEx        ver=3      pos=51348  size=5506    [339182..344688]
Custom      ver=0.0.0  pos=0      size=3850    [287834..291684]
Coordinate  ver=0.0.0  pos=3850   size=45887   [291684..337571]
Parameter   ver=0.0.6  pos=49737  size=640     [337571..338211]
Status      ver=0.0.0  pos=50377  size=838     [338211..339049]
About       ver=0.0.0  pos=51215  size=133     [339049..339182]
```

`Parameter` is MessagePack keyed by field name. Only these are read:

| Key | Type | Used for |
|---|---|---|
| `sex` | byte | `0` = `[M]`, anything else = `[F]` |
| `lastname` | string | name |
| `firstname` | string | name |
| `nickname` | string | appended as `(nickname)` |

`CharacterInfo` is built as:

```
[M] or [F]
+ " " + "Last First"   (empty parts dropped)
+ " (Nick)"            (only when nickname is not empty)
```

Examples: `[F] Az Chika`, `[F] Haruno Chika (Chika)`.

Personality is not parsed (the `personality` key is ignored).

### 3.2 Coordinate cards

Example: `KKCoordeF_20230826035019783.png`, file length 3059072, PNG end 142653.

```
PNG data ... IEND
int32   loadProductNo    = 100
str     marker           【KoiKatuClothes】
str     version          e.g. "0.0.0" (not used)
str     coordinateName   may be an empty string
int32   payloadLength    = 11764
bytes   payload          [142694..154458]
          int32 + MessagePack   clothes
          int32 + MessagePack   accessory
          bool                  enableMakeup
          int32 + MessagePack   makeup
str     "KKEx"
int32   kkExVersion      e.g. 3
int32   kkExLength       = 2904601
bytes   MessagePack map<string, PluginData>   [154471..3059072]
```

No `BlockHeader`. The `KKEx` block follows the outfit payload and normally runs to the end of the file.

### 3.3 Scene cards

KKManager does not parse scenes (`【KStudio】` is a TODO in its `CardLoader.cs`). The layout was verified against `Examples/2026_0802_0358_48_486.png` (file length 7564548, PNG end 132972) and cross-checked with the two projects in section 13.

```
PNG data ... IEND
str     sceneVersion     "1.1.2.1"    no productNo, no card marker
int32   objectCount
object[objectCount]
          int32 key
          int32 type     0=Character 1=Item 2=Light 3=Folder
                         4=Route 5=Camera 7=Text
          object payload (version-dependent)
int32   map
ChangeAmount             9 floats: pos, rot, scale
...     lights, 10 cameras, BGM, ambient sound, background, frame ...
str     "【KStudio】"    at 137723
str     "KKEx"
int32   kkExVersion     = 3
int32   kkExLength      = 7426798
bytes   MessagePack map<string, PluginData>   [137750..7564548]
```

Scene files can be very large (148 MB in the samples). The program never loads the whole file.

Detection and parsing:

1. Read `int32` product number and the leading marker. If the marker is unknown, fall through to scene detection.
2. Search the file from the PNG end for the length-prefixed `【KStudio】` pattern (14 bytes). A hit means `Type = Scene`, `Game = null`.
3. Locate the trailing extended block by searching for the length-prefixed `"KKEx"` pattern, then read it like a coordinate block.
4. Scene object data is not parsed, except for the optional embedded-character scan described in 4.3.

---

## 4. Extended data: plugins and zipmods

Plugin and zipmod information is read for all three card types.

### 4.1 PluginData

Extended data is a MessagePack `Dictionary<string, PluginData>`.

- The **key** is the extended data id, usually a plugin GUID (`com.bepis.bepinex.sideloader`), sometimes a legacy id (`EC.Core.Sideloader.UniversalAutoResolver`).
- Every key in the dictionary becomes an entry in `Plugins`.
- `PluginData` only declares key `1` (`Dictionary<string, object> data`). Version (key `0`) is not read.
- Binary values are never exported; only keys are collected.
- `CardParser.ReadExtendedPayload` reads the map with the hand-written `MessagePackUtil` reader, skips non-Sideloader values, and decodes the Sideloader value with `MessagePackSerializer`.

### 4.2 Sideloader zipmod keys

The Sideloader entry id is `com.bepis.sideloader.universalautoresolver`. The legacy `EC.Core.Sideloader.UniversalAutoResolver` id is not handled currently.

| Key | Value | GUID source |
|---|---|---|
| `info` | list of MessagePack `ResolveInfo` | each item's `ModID` |
| `itemInfo` | list of MessagePack `ResolveInfo` | each item's `ModID` |
| `patternInfo` | list of MessagePack `StudioPatternResolveInfo` | each `ObjectPatternInfo` value's `GUID` |
| `filterInfoGUID` | string | the value itself |
| `rampInfoGUID` | string | the value itself |
| `mapInfoGUID` | string | the value itself |
| `bgmInfoGUID` | string | the value itself |

`info` is used by character and coordinate cards. Scenes use `itemInfo`, `patternInfo` and the four plain GUID keys.

The result is de-duplicated and sorted with ordinal comparison. `ResolveInfo` declares only `ModID`; `Slot`, `LocalSlot`, `Property`, `CategoryNo` are skipped.

### 4.3 Embedded characters in scenes

`Parse characters in scenes` (checkbox, off by default) makes the parser walk the scene body looking for the MessagePack-encoded Sideloader id.

For each occurrence it locates the enclosing map header (1, 3 or 5 bytes before the key), decodes the whole dictionary, merges every plugin key into `Plugins`, and merges the Sideloader zipmod GUIDs into `Zipmods`. If the dictionary cannot be decoded, it falls back to decoding the single Sideloader value.

This is a heuristic scan, not a scene object parse. It exists because scenes embed character cards wholesale.

---

## 5. Zipmod name map

`Core/ZipmodMap.cs` resolves zipmod GUIDs to file names.

- `Build(folder)` recursively enumerates `*.zipmod`, opens each archive, and reads `manifest.xml` (`/manifest/guid`, `/manifest/version`).
- Duplicate GUIDs keep the highest manifest version; when neither file has a version, the last scanned file wins. `FileCount` records how many files share the GUID.
- The map is saved next to the executable as `ZipmodMap_yyyy-MM-dd HH-mm-ss.csv`, UTF-8 without BOM, no header. Format: `guid,file name,file count`. File names may contain commas, so the count is parsed from the last field.
- At startup the newest `ZipmodMap_*.csv` is loaded automatically.
- The UI shows `Zipmod map: not built` or `Zipmod map: N entries`, plus an `Open` button that launches the CSV.
- Display format when a map exists: `guid -> filename`; when missing: `guid -> (Missing)`.

The map is optional. Without it, every zipmod is displayed as `guid -> (Missing)` and the map is never queried for correctness.

---

## 6. Functional requirements

### 6.1 Implemented

| ID | Requirement | Notes |
|---|---|---|
| F-01 | Choose scan folder | Modern COM folder picker, path also editable |
| F-02 | Recursive scan | `*.png`, `Include subfolders` checked by default |
| F-03 | Detect file type | Character / Coordinate / Scene; anything else is dropped |
| F-04 | Read basic info | Sex marker, full name, nickname from `Parameter` |
| F-05 | Read plugins | Extended data keys |
| F-06 | Read zipmods | Sideloader keys per 4.2, all three types |
| F-07 | Result grid | File name, path, name, game, type, plugin count, zipmod count, status |
| F-08 | Details view | Tree view of plugins and zipmods, selected file or all results |
| F-09 | JSON export | Plugins and zipmods of the selected file or of the whole scan |
| F-10 | Fault tolerance | A failing file records an error; the scan continues |
| F-11 | Progress and cancel | Progress bar, cancel discards everything |
| F-12 | Zipmod name map | Build from a zipmod folder, cached as CSV, auto-loaded |

### 6.2 Out of scope

- Modifying, moving, deleting or installing anything.
- Parsing plugin DLLs (no Mono.Cecil).
- Scene object data (characters, items, lights) beyond the optional embedded-character scan.
- Cataloguing installed plugins or zipmods beyond the name map.
- Thumbnail previews.
- Game data other than the four `Parameter` fields.

---

## 7. UI

Layout is in `MainForm.Designer.cs`; behavior is in `MainForm.cs`. All UI text is English.

```
+--------------------------------------------------------------------------+
| [Path.................................] [Browse...] [x Include subfolders] |
| [x Parse characters in scenes]                                            |
| [Scan] [Cancel] [Export JSON]   Zipmod map: N entries  [Build zipmod map] [Open] |
| [progress bar]  status text                                               |
+--------------------------------------------------------------------------+
| File name | Path | Name | Game | Type | Plugins | Zipmods | Status         |
+--------------------------------------------------------------------------+
| (o) Selected card   ( ) All results                                       |
| TreeView: Plugins (n) / Zipmods (n)                                       |
+--------------------------------------------------------------------------+
```

Behavior:

- `Browse...` opens `Util/FolderPickerDialog.Show(...)`, a COM wrapper around `IFileOpenDialog` with `FOS_PICKFOLDERS` (the modern Explorer-style dialog).
- Cancelling the folder picker clears the path text box and also clears any previous scan results, resetting the status to `Ready`.
- While scanning, scan-related controls are disabled, including `Build zipmod map`; `Cancel` is enabled.
- The grid shows only recognized files. Unsupported PNGs are counted in the status line only.
- Row colors by type: Character light green, Coordinate light yellow, Scene light blue. Rows with an error use dark red text.
- Selecting a row shows its plugins and zipmods in the details tree; `All results` shows the union.
- Missing values are shown as `(Empty)` in the grid (empty game or empty name).
- Cancel discards all results and disables export. `Cancel` is disabled while building the zipmod map, because that operation is not cancellable.

---

## 8. Output

### 8.1 Grid and details view

Per card: `FileName`, `FilePath`, `Name` (`CharacterInfo` or `CoordinateName`), `Game`, `Type`, `Plugins` count, `Zipmods` count, `Status` (`OK` / `Error`).

Details tree, per selection:

```
Plugins (n)
  <guid>
Zipmods (n)
  <guid> -> <zipmod file name>   or   <guid> -> (Missing)
```

### 8.2 JSON export

Export does **not** dump the whole `CardScanResult`. It writes a two-list object for the selected card or for the whole scan.

```json
{
  "plugins": [
    "com.bepis.bepinex.sideloader",
    "com.deathweasel.bepinex.materialeditor"
  ],
  "zipmods": [
    "ryo2003.stiletto -> [Acezen] Az.Chara.Clothing v1.0.0.zipmod",
    "com.example.missing -> (Missing)"
  ]
}
```

- camelCase, indented, UTF-8 without BOM.
- `plugins`: distinct GUIDs, ordinal sort.
- `zipmods`: distinct GUIDs, ordinal sort, then formatted with the name map. When no map is loaded, every entry ends with `-> (Missing)`.
- Default file name: `All results.json` for the whole scan, `<card file name>.json` for a single card.
- Nothing is exported when there are no plugins and no zipmods.

### 8.3 `CardInfo` shape

```csharp
public class CardInfo
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string Game { get; set; }            // Koikatsu / Koikatsu Sunshine / null
    public string Type { get; set; }            // Character / Coordinate / Scene / null
    public string CharacterInfo { get; set; }   // e.g. "[F] Az Chika"
    public string CoordinateName { get; set; }  // Coordinate only
    public List<string> Plugins { get; set; }
    public List<string> Zipmods { get; set; }
    public string Error { get; set; }           // null when parsing succeeded
}
```

---

## 9. Error handling

| Situation | Behavior |
|---|---|
| Not a PNG | `error = "Not a PNG file"`, file dropped |
| No `IEND` | `error = "PNG end marker not found"`, file dropped |
| Plain PNG ending at `IEND` | no type, no error, dropped |
| Unknown marker and no `【KStudio】` | no type, no error, dropped |
| MessagePack failure while parsing a recognized type | error recorded, file kept with its `Type` |
| File locked (`ERROR_SHARING_VIOLATION` / `ERROR_LOCK_VIOLATION`) | `error = "File is locked by another process"` |
| Access denied | `error = "Access denied"` |
| Folder does not exist | message box, scan does not start |
| No extended data | normal result, empty `Plugins` and `Zipmods` |
| Cancelled | scanner returns `null`, UI clears everything |
| Unreadable subfolder | skipped, counted in `SkippedDirectories`, shown in the status line |
| Zipmod map build failure | status line reports it, message box with the exception message |

Any other exception is caught per file and stored as `ExceptionType: message`.

---

## 10. Implementation

### 10.1 Target and dependencies

- C#, Windows Forms, `net48`, C# latest, Visual Studio 2022.
- `MessagePack` 2.5.307. Do not downgrade: 2.5.187 has published advisories (NU1902/NU1903).
- `Newtonsoft.Json` 13.0.3.
- Framework references: `System.Configuration`, `System.IO.Compression`, `System.IO.Compression.FileSystem`.
- No Mono.Cecil and no third-party UI library.

### 10.2 Code structure

```
KoikatsuCardReader.csproj
App.config                    MaxDegreeOfParallelism setting, accessibility switches
Program.cs                    entry point
MainForm.cs                   behavior
MainForm.Designer.cs          layout (designer generated)
MainForm.resx
Core/
  CardParser.cs               per-file parse, read-only, never throws
  CardScanner.cs              enumeration, Parallel.ForEach, cancel, progress, CardScanResult
  CardInfo.cs                 result model
  CardMarker.cs               marker constants and Type/Game names
  SideloaderParser.cs         zipmod GUID extraction
  ZipmodMap.cs                GUID to zipmod file name map plus CSV cache
  JsonExporter.cs             camelCase serializer, UTF-8 without BOM
  Models/
    BlockHeader.cs            lstInfo / name / version / pos / size
    ChaFileParameter.cs       sex, lastname, firstname, nickname
    PluginData.cs             key 1 data
    SideloaderModels.cs       ResolveInfo (ModID), StudioPatternResolveInfo
Util/
  PngUtil.cs                  PNG check, IEND search, streaming KMP sequence search
  MessagePackUtil.cs          hand-written streaming MessagePack reader and skip helpers
  FolderPickerDialog.cs       IFileOpenDialog COM folder picker
```

### 10.3 Parse flow

1. Open with `FileStream` and `FileShare.Read`, `FileOptions.SequentialScan`.
2. Verify the PNG signature, then find `IEND`.
3. If the file ends exactly at `IEND`, it is not a card.
4. Read `int32` product number and the leading marker; map it to `Game` and `Type`.
5. Skip the load version string when a type was recognized.
6. If no type was recognized, search for the length-prefixed `【KStudio】` marker; a hit means `Scene`.
7. Dispatch:
   - Character: skip face thumbnail, read `BlockHeader`, read `Parameter` into `CharacterInfo`, read the `KKEx` block.
   - Coordinate: read `coordinateName`, skip the outfit payload, read the trailing `KKEx` block.
   - Scene: search for the `KKEx` block from the PNG end and read it; optionally scan embedded characters.
8. Merge plugin keys and zipmod GUIDs, de-duplicate and sort.
9. Files with no `Type` are omitted from `CardScanResult.Cards`.

All length-prefixed strings are 7-bit UTF-8 with a 64 KB cap. Empty strings are allowed for coordinate names.

### 10.4 Concurrency and cancellation

- Scanning runs on a worker thread via `Task.Run`; the UI thread is never blocked.
- `Parallel.ForEach` degree of parallelism comes from `App.config` `MaxDegreeOfParallelism`, default 2. Large scene files make higher values counterproductive.
- Progress is reported through `IProgress<(int Done, int Total)>`, throttled to about 200 updates.
- Cancellation does not throw. Worker threads check the token, call `loopState.Break()`, and `Scan` returns `null`; the UI reacts by clearing results. This avoids `Parallel.ForEach` surfacing `OperationCanceledException` through the task boundary, where the debugger would break first.
- Zipmod map building runs on a worker thread and is not cancellable.

---

## 11. Build and repository

```
dotnet build KoikatsuCardReader.csproj -c Debug
```

Output: `bin\Debug\net48\KoikatsuCardReader.exe`. Or open the project in Visual Studio 2022.

The executable must be closed before rebuilding; otherwise MSBuild fails to overwrite it.
For a compile-only check while the app is running:

```
dotnet build KoikatsuCardReader.csproj -c Debug -p:OutputPath=<temp>\bin\ -p:IntermediateOutputPath=<temp>\obj\
```

Conventions:

- All text files use CRLF.
- Abbreviations that stand for separate words are fully uppercase (`GUID`, `HTTP`); lowercase inside camelCase (`guid`, `http`). Abbreviations embedded in a word use Pascal case (`App`).
- No comments unless they explain non-obvious binary format details.
- `.gitignore` excludes `bin/`, `obj/`, `.vs/`, `*.csproj.user`, NuGet intermediates, `Examples/` and `*.zipmod`.
- `Examples/` and `Mods/` are local test data supplied by the user and must never be committed. `Examples/` contains a 148 MB scene.
- Only commit when explicitly asked.

---

## 12. Verified samples

`Examples/` currently holds 46 PNG files: 20 coordinate cards, 3 scenes, 12 Sunshine character cards, several plain/unsupported PNGs, and additional KK/coordinate files added later. `Mods/` holds the zipmods used to build the name map.

Observed behavior:

- `KK BR-Chan.png` contains `【KoiKatuCharaSun】`, so it is Sunshine. Trust the marker, not the file name.
- Coordinate names parse correctly, including the empty name case.
- Scene files up to 148 MB parse quickly because only the tail is read.
- Scene zipmods, for example: `Az.Filter.AzLUT`, `Az.Studio.Dome`, `Az.Studio.ReflectionProbe`, `HarvexARC.StudioEffects`, `uppervolta.MoreShadowTypes`.

---

## 13. References

Full list in `REFERENCES.md`. Key sources:

- KKManager, https://github.com/IllusionMods/KKManager - character card, coordinate card, extended data, Sideloader. Scenes are a TODO there.
- KoikatuCharaLoader, https://github.com/great-majority/KoikatuCharaLoader - `kkloader/KoikatuSceneData.py` for the scene layout, `kkloader/KoikatuCharaData.py` for the coordinate payload.
- koikatu.js, https://github.com/great-majority/koikatu.js - `src/scene/kk.ts` and `src/parse/scanPng.ts` for cross-checking.

---

## 14. Open items and known deviations

1. **Party is not distinguished.** `【KoiKatuCharaS】` and `【KoiKatuCharaSP】` map to `Game = Koikatsu`. Reporting `Koikatsu Party` separately would require a new game value in `CardMarker` and the UI.
2. **Personality, sex text and per-plugin version are not parsed.** `CardInfo` uses `[M]`/`[F]` inside `CharacterInfo` and plain GUID lists, so there is no `Sex`, `Personality` or `PluginInfo.Version` field.
3. **JSON export shape changed.** It exports only `{ plugins, zipmods }`, not the full scan result, and zipmod entries may include the resolved file name.
4. **Legacy Sideloader id is not handled.** `EC.Core.Sideloader.UniversalAutoResolver` is not in `IsSideloaderKey`; EffectCreators-era cards would lose their zipmod list.
5. **Scene `KKEx` lookup is a plain pattern search.** It finds the first length-prefixed `"KKEx"` after the PNG end. A coincidental match inside the scene body would misparse.
6. **Real Party cards are still missing** from `Examples/`, so that path is untested with real data.
7. **Scene embedded-character parsing** is heuristic and off by default.
8. **`ZipmodMap` is only written next to the executable.** Old CSVs are never cleaned up.
