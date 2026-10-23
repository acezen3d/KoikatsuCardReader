# KoikatsuCardReader

Version 1.0.0 | License: GPL-3.0 | https://github.com/acezen3d/KoikatsuCardReader

Read-only tool that scans a folder of Koikatsu cards and lists the plugins and zipmods each card, coordinate and scene depends on.

Cards are opened with `FileShare.Read`. The only files written are the JSON export you request and the `ZipmodMap_*.csv` cache next to the executable.

## Supported files

| Marker | `Type` | `Game` |
|---|---|---|
| `【KoiKatuChara】` | `Character` | `Koikatsu` |
| `【KoiKatuCharaS】`, `【KoiKatuCharaSP】` | `Character` | `Koikatsu` |
| `【KoiKatuCharaSun】` | `Character` | `Koikatsu Sunshine` |
| `【KoiKatuClothes】` | `Coordinate` | *(null)* |
| `【KStudio】` | `Scene` | *(null)* |
| anything else | dropped | |

Scene cards and coordinate cards do not encode the game, so `Game` is `null` for them. Koikatsu Party is currently reported as `Koikatsu`.

## Requirements

Windows and .NET Framework 4.8.

## Usage

1. Choose the folder to scan, or type a path. `Include subfolders` is on by default.
2. Optionally enable `Parse characters in scenes` (heuristic, slower).
3. Click `Scan`. `Cancel` discards all results.
4. Select a row to inspect its plugins and zipmods, or switch to `All results`.
5. Click `Export JSON` to save the list.

### Zipmod map

Zipmods are identified by GUID, so without a map they show as `guid -> (Missing)`.

Click `Build zipmod map`, select the folder containing your `.zipmod` files, and the tool reads `manifest.xml` from each archive. The result is cached as `ZipmodMap_yyyy-MM-dd HH-mm-ss.csv` next to the executable and the newest one loads at startup. `Open` launches it. Duplicate GUIDs keep the highest manifest version.

## Output

Details tree:

```
Plugins (n)
  <guid>
Zipmods (n)
  <guid> -> <zipmod file name>     (or "<guid> -> (Missing)")
```

JSON export, camelCase, indented, UTF-8 without BOM:

```json
{
  "plugins": [
    "<guid>"
  ],
  "zipmods": [
    "<guid> -> <zipmod file name>",
    "<guid> -> (Missing)"
  ]
}
```

Default name: `All results.json` for the whole scan, `<card file name>.json` for a single card. Nothing is exported when both lists are empty.

## How it works

Cards are PNG images with data appended after `IEND` (`49 45 4E 44 AE 42 60 82`).

- **Character cards**: product number, marker, version, face thumbnail, MessagePack `BlockHeader` (`lstInfo` with `pos`/`size`), `int64` placeholder, then the blocks. Basic info comes from `Parameter`, plugins and zipmods from `KKEx`.
- **Coordinate cards**: marker, version, outfit name, length-prefixed outfit payload, trailing `KKEx`.
- **Scene cards**: version, objects, map and lighting settings, `【KStudio】`, trailing `KKEx`. Only the tail is read, so a 148 MB scene parses quickly.

Zipmod GUIDs come from the Sideloader entry `com.bepis.sideloader.universalautoresolver`: `info` for character and coordinate cards; `itemInfo`, `patternInfo`, `filterInfoGUID`, `rampInfoGUID`, `mapInfoGUID` and `bgmInfoGUID` for scenes. Every other extended data key becomes a plugin GUID.

## Limitations

- Plugins are listed by GUID only. Unlike zipmods, there is no plugin map that resolves a GUID to the DLL file name.

## License

GPL-3.0. See [LICENSE](LICENSE).
