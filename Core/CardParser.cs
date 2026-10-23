using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KoikatsuCardReader.Core.Models;
using KoikatsuCardReader.Util;
using MessagePack;

namespace KoikatsuCardReader.Core
{
    /// <summary>
    /// Parses one Koikatsu PNG file (read-only).
    /// Character and coordinate cards follow KKManager's CardLoader paradigm: PNG end, product
    /// number + marker, dispatch to a card-specific reader, then ExtendedSave data. Scenes have no
    /// leading card marker; they are detected by the trailing 【KStudio】 marker (see kkloader).
    /// Plugin and zipmod information is read for all three types.
    /// Never throws: failures are recorded in CardInfo.Error.
    /// </summary>
    public static class CardParser
    {
        private const string ExtendedBlockName = "KKEx";
        private const int MaxStringLength = 64 * 1024;

        /// <summary>Length-prefixed 【KStudio】 marker written at the end of the scene body.</summary>
        private static readonly byte[] SceneMarkerPattern = BuildLengthPrefixedIdPattern(CardMarker.SceneMarker);

        /// <summary>Length-prefixed "KKEx" id that starts an ExtendedSave block.</summary>
        private static readonly byte[] ExtendedBlockIdPattern = BuildLengthPrefixedIdPattern(ExtendedBlockName);

        /// <summary>MessagePack-encoded Sideloader extended data ids, used to find data embedded in scenes.</summary>
        private static readonly byte[] SideloaderKeyPattern = MessagePackUtil.BuildStringPattern(SideloaderParser.UniversalAutoResolverId);

        public static CardInfo Parse(string filePath, bool parseEmbeddedCharacter = false)
        {
            var info = new CardInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
            };

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    // Inlined ParseStream: reject non-PNG files before looking for the payload.
                    if (!PngUtil.IsPng(stream))
                    {
                        info.Error = "Not a PNG file";
                    }
                    else
                    {
                        var pngEnd = PngUtil.SearchForPngEnd(stream);
                        if (pngEnd < 0)
                        {
                            info.Error = "PNG end marker not found";
                        }
                        // A plain PNG ends exactly at IEND. Not a card, not an error.
                        else if (pngEnd < stream.Length)
                        {
                            // Inlined TryReadFormatInfo: read loadProductNo and the leading marker.
                            string game = null;
                            string type = null;

                            stream.Position = pngEnd;

                            // Inlined TryReadMarker: malformed prefixes fall back to scene detection.
                            string marker = null;
                            try
                            {
                                reader.ReadInt32(); // loadProductNo: not used.
                                marker = ReadString(reader);
                            }
                            catch (Exception ex) when (ex is EndOfStreamException || ex is InvalidDataException)
                            {
                            }

                            if (marker != null)
                            {
                                switch (marker)
                                {
                                    case CardMarker.CharacterMarker:
                                    case CardMarker.CharacterPartyMarker:
                                    case CardMarker.CharacterPartySpecialPatchMarker:
                                        game = CardMarker.Koikatsu;
                                        type = CardMarker.Character;
                                        break;
                                    case CardMarker.CharacterSunshineMarker:
                                        game = CardMarker.Sunshine;
                                        type = CardMarker.Character;
                                        break;
                                    case CardMarker.CoordinateMarker:
                                        type = CardMarker.Coordinate;
                                        break;
                                }

                                if (type != null)
                                {
                                    // Skip the load version; the payload starts after it.
                                    ReadString(reader);
                                }
                            }

                            // Scenes have no leading marker; identify them by the trailing 【KStudio】 marker.
                            if (type == null)
                            {
                                stream.Position = pngEnd;
                                if (PngUtil.SearchForSequence(stream, SceneMarkerPattern) >= 0)
                                    type = CardMarker.Scene;
                            }

                            // Unsupported files have no type and no error.
                            if (type != null)
                            {
                                info.Game = game;
                                info.Type = type;

                                ExtendedData extended = null;
                                switch (type)
                                {
                                    case CardMarker.Character:
                                        extended = ParseCharacter(stream, reader, info);
                                        break;
                                    case CardMarker.Coordinate:
                                        extended = ParseCoordinate(stream, reader, info);
                                        break;
                                    case CardMarker.Scene:
                                        extended = ParseScene(stream, reader, pngEnd, parseEmbeddedCharacter);
                                        break;
                                }

                                info.Plugins = SortedDistinct(extended?.PluginGUIDs);
                                info.Zipmods = SortedDistinct(extended?.ZipmodGUIDs);
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                SetError(info, "Access denied");
            }
            // Inlined IsLockedFile: ERROR_SHARING_VIOLATION (0x20) and ERROR_LOCK_VIOLATION (0x21).
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x20 || (ex.HResult & 0xFFFF) == 0x21)
            {
                SetError(info, "File is locked by another process");
            }
            catch (Exception ex)
            {
                SetError(info, $"{ex.GetType().Name}: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// Character cards: face thumbnail, block index, Parameter block and KKEx block.
        /// Follows KKManager KoiCard / KoiSunCard.ParseKoiChara.
        /// </summary>
        private static ExtendedData ParseCharacter(FileStream stream, BinaryReader reader, CardInfo info)
        {
            // Face thumbnail PNG: skip.
            var faceLength = reader.ReadInt32();
            if (faceLength > 0)
                stream.Seek(faceLength, SeekOrigin.Current);

            var headerLength = reader.ReadInt32();
            if (headerLength <= 0)
                throw new InvalidDataException("Invalid block header length");

            var blockHeader = MessagePackSerializer.Deserialize<BlockHeader>(reader.ReadBytes(headerLength));

            // Placeholder int64, ignored.
            reader.ReadInt64();
            var basePosition = stream.Position;

            var parameterBlock = blockHeader.SearchInfo(ChaFileParameter.BlockName);
            if (parameterBlock != null)
            {
                // Inlined ReadBlock: validate the declared size, seek to the block, and read it exactly.
                if (parameterBlock.Size < 0 || parameterBlock.Size > int.MaxValue)
                    throw new InvalidDataException($"Invalid size for block {parameterBlock.Name}");

                stream.Position = basePosition + parameterBlock.Pos;
                var parameterBytes = reader.ReadBytes((int)parameterBlock.Size);
                if (parameterBytes.Length != parameterBlock.Size)
                    throw new EndOfStreamException($"Block {parameterBlock.Name} is truncated");

                var parameter = MessagePackSerializer.Deserialize<ChaFileParameter>(parameterBytes);

                // Inlined BuildCharacterInfo: format [M]/[F], full name, and optional nickname.
                var sex = parameter.Sex == 0 ? "[M]" : "[F]";
                var name = string.Join(" ", new[] { parameter.LastName, parameter.FirstName }
                    .Select(value => value?.Trim())
                    .Where(value => !string.IsNullOrEmpty(value)));

                var characterInfo = string.IsNullOrEmpty(name) ? sex : $"{sex} {name}";

                var nickname = parameter.NickName?.Trim();
                if (!string.IsNullOrEmpty(nickname))
                    characterInfo += $" ({nickname})";

                info.CharacterInfo = characterInfo;
            }

            var extendedBlock = blockHeader.SearchInfo(ExtendedBlockName);
            ExtendedData extended = null;
            if (extendedBlock != null)
            {
                if (extendedBlock.Size < 0 || extendedBlock.Size > int.MaxValue)
                    throw new InvalidDataException($"Invalid size for block {extendedBlock.Name}");

                stream.Position = basePosition + extendedBlock.Pos;
                extended = ReadExtendedPayload(reader, (int)extendedBlock.Size);
            }

            return extended;
        }

        /// <summary>
        /// Coordinate cards: coordinate name, optional clothes payload, then the KKEx block.
        /// Follows KKManager KoiCoordCard.ParseKoiClothes.
        /// </summary>
        private static ExtendedData ParseCoordinate(FileStream stream, BinaryReader reader, CardInfo info)
        {
            info.CoordinateName = ReadString(reader);

            // Clothes / accessories data: skipped.
            var clothesLength = reader.ReadInt32();
            if (clothesLength < 0) throw new InvalidDataException("Invalid clothes data length");
            stream.Seek(clothesLength, SeekOrigin.Current);

            return ReadExtendedBlock(reader);
        }

        /// <summary>
        /// Scenes: the trailing ExtendedSave block is located by its "KKEx" id.
        /// </summary>
        private static ExtendedData ParseScene(FileStream stream, BinaryReader reader, long pngEnd, bool parseEmbeddedCharacter)
        {
            stream.Position = pngEnd;
            var blockPosition = PngUtil.SearchForSequence(stream, ExtendedBlockIdPattern);

            ExtendedData extended = null;
            if (blockPosition >= 0)
            {
                stream.Position = blockPosition;
                extended = ReadExtendedBlock(reader);
            }

            if (parseEmbeddedCharacter)
            {
                if (extended == null)
                    extended = new ExtendedData();

                AddEmbeddedCharacterData(stream, pngEnd, extended);
            }

            return extended;
        }

        /// <summary>
        /// Scans the scene body for embedded character Sideloader data and merges it in.
        /// Only runs when the user enables parsing characters in scenes.
        /// </summary>
        private static void AddEmbeddedCharacterData(FileStream stream, long searchStart, ExtendedData extended)
        {
            var key = SideloaderParser.UniversalAutoResolverId;

            while (true)
            {
                stream.Position = searchStart;
                var keyPosition = PngUtil.SearchForSequence(stream, SideloaderKeyPattern);
                if (keyPosition < 0) return;

                searchStart = keyPosition + SideloaderKeyPattern.Length;

                // Inlined FindMessagePackMapStart: the dictionary header can be 1, 3, or 5 bytes
                // immediately before its first key.
                var mapStart = -1L;
                var buffer = new byte[5];
                for (var headerLength = 1; headerLength <= 5; headerLength += 2)
                {
                    if (keyPosition < headerLength) continue;

                    // Inlined TryReadAt: fill the header bytes without reading past EOF.
                    stream.Position = keyPosition - headerLength;
                    var total = 0;
                    while (total < headerLength)
                    {
                        var read = stream.Read(buffer, total, headerLength - total);
                        if (read <= 0) break;
                        total += read;
                    }
                    if (total != headerLength) continue;

                    if (MessagePackUtil.GetMapHeaderLength(buffer[0]) == headerLength)
                    {
                        mapStart = keyPosition - headerLength;
                        break;
                    }
                }

                // Inlined TryReadExtendedDataMap: when the Sideloader id is the first key, read the
                // whole dictionary to collect every plugin key and the zipmod list in one pass.
                var mapHandled = false;
                if (mapStart >= 0)
                {
                    try
                    {
                        stream.Position = mapStart;
                        var map = MessagePackSerializer.Deserialize<Dictionary<string, PluginData>>(stream);
                        if (map != null && map.TryGetValue(key, out var pluginData))
                        {
                            foreach (var pluginGUID in map.Keys)
                            {
                                if (!extended.PluginGUIDs.Contains(pluginGUID))
                                    extended.PluginGUIDs.Add(pluginGUID);
                            }

                            extended.ZipmodGUIDs.AddRange(SideloaderParser.ExtractZipmodGUIDs(pluginData));
                            mapHandled = true;
                        }
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                        // Fall back to decoding the value alone.
                    }
                }

                if (mapHandled) continue;

                // Inlined ReadExtendedDataValue: decode only the Sideloader value when the map header
                // could not be located or the dictionary was malformed.
                try
                {
                    stream.Position = keyPosition + SideloaderKeyPattern.Length;
                    var pluginData = MessagePackSerializer.Deserialize<PluginData>(stream);
                    if (pluginData?.Data == null) continue;

                    if (!extended.PluginGUIDs.Contains(key))
                        extended.PluginGUIDs.Add(key);

                    extended.ZipmodGUIDs.AddRange(SideloaderParser.ExtractZipmodGUIDs(pluginData));
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    // Ignore malformed embedded data and keep scanning.
                }
            }
        }

        /// <summary>
        /// Reads the "KKEx" extended block used by coordinates and scenes:
        /// string "KKEx", int32 version, int32 length, MessagePack payload.
        /// Returns null if the block is absent or invalid.
        /// </summary>
        private static ExtendedData ReadExtendedBlock(BinaryReader reader)
        {
            string id;
            try
            {
                id = ReadString(reader);
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is InvalidDataException)
            {
                return null;
            }

            if (id != ExtendedBlockName)
                return null;

            reader.ReadInt32(); // version
            var length = reader.ReadInt32();
            if (length <= 0 || length > reader.BaseStream.Length - reader.BaseStream.Position)
                return null;

            return ReadExtendedPayload(reader, length);
        }

        private static ExtendedData ReadExtendedPayload(BinaryReader reader, int length)
        {
            var end = reader.BaseStream.Position + length;
            var data = new ExtendedData();
            var count = MessagePackUtil.ReadMapHeader(reader);

            for (long i = 0; i < count; i++)
            {
                var key = MessagePackUtil.ReadString(reader);
                data.PluginGUIDs.Add(key);

                if (SideloaderParser.IsSideloaderKey(key))
                {
                    using (var value = new MemoryStream())
                    {
                        MessagePackUtil.SkipValue(reader, value);

                        try
                        {
                            var pluginData = MessagePackSerializer.Deserialize<PluginData>(value.ToArray());
                            data.ZipmodGUIDs.AddRange(SideloaderParser.ExtractZipmodGUIDs(pluginData));
                        }
                        catch (MessagePackSerializationException)
                        {
                        }
                    }
                }
                else
                {
                    MessagePackUtil.SkipValue(reader);
                }
            }

            var remaining = end - reader.BaseStream.Position;
            if (remaining < 0)
                throw new InvalidDataException("Extended data exceeds block length");
            if (remaining > 0)
                MessagePackUtil.SkipBytes(reader, remaining);

            return data;
        }

        private static byte[] BuildLengthPrefixedIdPattern(string id)
        {
            var bytes = Encoding.UTF8.GetBytes(id);
            var pattern = new byte[1 + bytes.Length];
            pattern[0] = (byte)bytes.Length;
            Buffer.BlockCopy(bytes, 0, pattern, 1, bytes.Length);
            return pattern;
        }

        private sealed class ExtendedData
        {
            public List<string> PluginGUIDs { get; } = new List<string>();
            public List<string> ZipmodGUIDs { get; } = new List<string>();
        }

        private static List<string> SortedDistinct(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Reads a 7-bit length-prefixed UTF-8 string (BinaryReader.ReadString format).
        /// Empty strings are allowed (coordinate names can be empty).
        /// Throws InvalidDataException if the length is invalid.
        /// </summary>
        private static string ReadString(BinaryReader reader)
        {
            var length = 0;
            var shift = 0;
            for (var i = 0; i < 5; i++)
            {
                var b = reader.ReadByte();
                length |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    if (length < 0 || length > MaxStringLength)
                        throw new InvalidDataException("Invalid string length");
                    if (length == 0) return string.Empty;

                    var bytes = reader.ReadBytes(length);
                    if (bytes.Length != length)
                        throw new EndOfStreamException("String is truncated");
                    return Encoding.UTF8.GetString(bytes);
                }
                shift += 7;
            }
            throw new InvalidDataException("Invalid string length prefix");
        }

        private static void SetError(CardInfo info, string message)
        {
            info.Error = message;
            info.CharacterInfo = null;
            info.CoordinateName = null;
            info.Plugins = new List<string>();
            info.Zipmods = new List<string>();
        }
    }
}
