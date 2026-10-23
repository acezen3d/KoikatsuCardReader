using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace KoikatsuCardReader.Core
{
    /// <summary>
    /// One resolved zipmod: the chosen file name and how many zipmod files share the GUID.
    /// </summary>
    public sealed class ZipmodMapEntry
    {
        public string FileName { get; set; }

        /// <summary>Number of zipmod files with this GUID (1 when unique).</summary>
        public int FileCount { get; set; }
    }

    /// <summary>
    /// Maps zipmod GUIDs to zipmod file names by reading manifest.xml from each .zipmod.
    /// Duplicate GUIDs keep the highest manifest version; when no version is available the
    /// last scanned file wins. Each build is saved next to the executable as a headerless CSV
    /// (guid,file name,file count).
    /// </summary>
    public static class ZipmodMap
    {
        public const string FilePrefix = "ZipmodMap_";

        public static string DirectoryPath => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>Returns the newest ZipmodMap_*.csv file, or null when none exists.</summary>
        public static string FindLatestFile()
        {
            var directory = new DirectoryInfo(DirectoryPath);
            if (!directory.Exists) return null;

            return directory
                .GetFiles(FilePrefix + "*.csv", SearchOption.TopDirectoryOnly)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.Ordinal)
                .Select(file => file.FullName)
                .FirstOrDefault();
        }

        public static Dictionary<string, ZipmodMapEntry> LoadLatest()
        {
            var path = FindLatestFile();
            return path == null
                ? new Dictionary<string, ZipmodMapEntry>(StringComparer.OrdinalIgnoreCase)
                : Load(path);
        }

        public static Dictionary<string, ZipmodMapEntry> Load(string path)
        {
            var map = new Dictionary<string, ZipmodMapEntry>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return map;

            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var firstComma = line.IndexOf(',');
                if (firstComma <= 0 || firstComma == line.Length - 1) continue;

                var guid = line.Substring(0, firstComma).Trim();
                var rest = line.Substring(firstComma + 1);

                // File names may contain commas, so the file count is the last field.
                var lastComma = rest.LastIndexOf(',');
                string fileName;
                var fileCount = 1;

                if (lastComma < 0)
                {
                    fileName = rest.Trim();
                }
                else
                {
                    fileName = rest.Substring(0, lastComma).Trim();
                    if (!int.TryParse(rest.Substring(lastComma + 1).Trim(), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out fileCount) || fileCount < 1)
                        fileCount = 1;
                }

                if (guid.Length == 0 || fileName.Length == 0) continue;

                map[guid] = new ZipmodMapEntry
                {
                    FileName = fileName,
                    FileCount = fileCount,
                };
            }

            return map;
        }

        /// <summary>Saves the map as ZipmodMap_yyyy-MM-dd HH-mm-ss.csv and returns the file path.</summary>
        public static string Save(Dictionary<string, ZipmodMapEntry> map)
        {
            var path = Path.Combine(DirectoryPath, $"{FilePrefix}{DateTime.Now:yyyy-MM-dd HH-mm-ss}.csv");
            var lines = map
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "," + pair.Value.FileName + "," + pair.Value.FileCount);

            File.WriteAllLines(path, lines, new UTF8Encoding(false));
            return path;
        }

        public static Dictionary<string, ZipmodMapEntry> Build(string rootFolder)
        {
            var map = new Dictionary<string, ZipmodMapEntry>(StringComparer.OrdinalIgnoreCase);
            var versions = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in EnumerateZipmods(rootFolder))
            {
                if (!TryReadManifest(file, out var guid, out var versionText)) continue;

                var fileName = Path.GetFileName(file);
                var version = ParseVersion(versionText);

                if (!map.TryGetValue(guid, out var entry))
                {
                    map[guid] = new ZipmodMapEntry { FileName = fileName, FileCount = 1 };
                    if (version != null) versions[guid] = version;
                    continue;
                }

                entry.FileCount++;
                versions.TryGetValue(guid, out var currentVersion);

                if (version != null)
                {
                    if (currentVersion == null || CompareVersions(version, currentVersion) > 0)
                    {
                        entry.FileName = fileName;
                        versions[guid] = version;
                    }
                }
                else if (currentVersion == null)
                {
                    // No version on either side: last scanned file wins.
                    entry.FileName = fileName;
                }
            }

            return map;
        }

        private static IEnumerable<string> EnumerateZipmods(string rootFolder)
        {
            var pending = new Stack<string>();
            pending.Push(rootFolder);

            while (pending.Count > 0)
            {
                var directory = pending.Pop();

                string[] files;
                try
                {
                    files = Directory.GetFiles(directory, "*.zipmod", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    continue;
                }

                foreach (var file in files)
                    yield return file;

                string[] subdirectories;
                try
                {
                    subdirectories = Directory.GetDirectories(directory);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    continue;
                }

                foreach (var subdirectory in subdirectories)
                    pending.Push(subdirectory);
            }
        }

        private static bool TryReadManifest(string zipmodPath, out string guid, out string version)
        {
            guid = null;
            version = null;

            try
            {
                using (var archive = ZipFile.OpenRead(zipmodPath))
                {
                    var manifest = archive.Entries.FirstOrDefault(entry =>
                        string.Equals(entry.FullName, "manifest.xml", StringComparison.OrdinalIgnoreCase));
                    if (manifest == null) return false;

                    using (var stream = manifest.Open())
                    {
                        var document = new XmlDocument { XmlResolver = null };
                        document.Load(stream);

                        var guidNode = document.SelectSingleNode("/manifest/guid")
                                       ?? document.GetElementsByTagName("guid").Item(0);
                        guid = guidNode?.InnerText.Trim();
                        if (string.IsNullOrEmpty(guid)) return false;

                        var versionNode = document.SelectSingleNode("/manifest/version")
                                          ?? document.GetElementsByTagName("version").Item(0);
                        version = versionNode?.InnerText.Trim();
                        return true;
                    }
                }
            }
            catch (Exception ex) when (
                ex is IOException
                || ex is UnauthorizedAccessException
                || ex is InvalidDataException
                || ex is XmlException)
            {
                return false;
            }
        }

        private static int[] ParseVersion(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Trim();
            if (text.Length > 1 && (text[0] == 'v' || text[0] == 'V'))
                text = text.Substring(1);

            var parts = text.Split('.');
            if (parts.Length == 0 || parts.Length > 4) return null;

            var numbers = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                    return null;
            }

            return numbers;
        }

        private static int CompareVersions(int[] left, int[] right)
        {
            var length = Math.Max(left.Length, right.Length);
            for (var i = 0; i < length; i++)
            {
                var l = i < left.Length ? left[i] : 0;
                var r = i < right.Length ? right[i] : 0;
                if (l != r) return l < r ? -1 : 1;
            }

            return 0;
        }
    }
}
