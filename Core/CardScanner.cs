using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KoikatsuCardReader.Core
{
    /// <summary>
    /// Recursively collects *.png files and parses them in parallel.
    /// Runs on a worker thread. Returns null when cancelled (no partial result is returned).
    /// </summary>
    public class CardScanner
    {
        public CardScanResult Scan(string rootPath, bool recursive, bool parseEmbeddedCharacter, IProgress<(int Done, int Total)> progress, CancellationToken token)
        {
            var skipped = new List<string>();
            var files = EnumeratePngFiles(rootPath, recursive, skipped, token);
            if (token.IsCancellationRequested)
                return null;

            var total = files.Count;
            var done = 0;
            var bag = new ConcurrentBag<CardInfo>();
            var reportStep = Math.Max(1, total / 200);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(),
            };

            // Do not throw OperationCanceledException from worker threads. Parallel.ForEach would
            // surface it through the task boundary (and the debugger breaks on it first). Checking
            // the token and stopping the loop keeps cancellation non-throwing; Scan returns null.
            Parallel.ForEach(files, options, (file, loopState) =>
            {
                if (token.IsCancellationRequested)
                {
                    loopState.Break();
                    return;
                }

                bag.Add(CardParser.Parse(file, parseEmbeddedCharacter));

                var current = Interlocked.Increment(ref done);
                if (current == 1 || current == total || current % reportStep == 0)
                    progress?.Report((current, total));
            });

            if (token.IsCancellationRequested)
                return null;

            var cards = bag
                .Where(card => card.Type != null)
                .OrderBy(card => card.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CardScanResult
            {
                ScanRoot = rootPath,
                ScannedAt = DateTime.Now,
                CardCount = cards.Count,
                NonCardCount = files.Count - cards.Count,
                Cards = cards,
                SkippedDirectories = skipped,
            };
        }

        private static int GetMaxDegreeOfParallelism()
        {
            var value = ConfigurationManager.AppSettings["MaxDegreeOfParallelism"];
            return int.TryParse(value, out var result) && result > 0 ? result : 2;
        }

        /// <summary>
        /// Enumerates *.png files without aborting on unreadable directories.
        /// Unreadable directories are added to skipped.
        /// </summary>
        private static List<string> EnumeratePngFiles(string rootPath, bool recursive, List<string> skipped, CancellationToken token)
        {
            var files = new List<string>();
            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                if (token.IsCancellationRequested)
                    break;

                var dir = pending.Pop();

                try
                {
                    files.AddRange(Directory.EnumerateFiles(dir, "*.png", SearchOption.TopDirectoryOnly));
                }
                catch (UnauthorizedAccessException)
                {
                    skipped.Add(dir);
                    continue;
                }
                catch (IOException)
                {
                    skipped.Add(dir);
                    continue;
                }

                if (!recursive) continue;

                try
                {
                    foreach (var sub in Directory.EnumerateDirectories(dir))
                        pending.Push(sub);
                }
                catch (UnauthorizedAccessException)
                {
                    skipped.Add(dir);
                }
                catch (IOException)
                {
                    skipped.Add(dir);
                }
            }

            return files;
        }
    }

    /// <summary>
    /// Result of one scan. Serialized to JSON (camelCase) for display and export.
    /// </summary>
    public class CardScanResult
    {
        public string ScanRoot { get; set; }

        public DateTime ScannedAt { get; set; }

        public int CardCount { get; set; }

        /// <summary>PNG files that were not recognized as cards. Not exported to JSON.</summary>
        [JsonIgnore]
        public int NonCardCount { get; set; }

        public List<CardInfo> Cards { get; set; } = new List<CardInfo>();

        /// <summary>Directories that could not be read. Not exported to JSON.</summary>
        [JsonIgnore]
        public List<string> SkippedDirectories { get; set; } = new List<string>();
    }
}
