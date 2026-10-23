using System.IO;

namespace KoikatsuCardReader.Util
{
    /// <summary>
    /// PNG and byte-sequence helpers. Streams are read in chunks, the whole file is never loaded.
    /// </summary>
    public static class PngUtil
    {
        private const int BufferSize = 64 * 1024;

        /// <summary>PNG file signature.</summary>
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Checks whether the stream starts with the PNG signature. Restores the original position.
        /// </summary>
        public static bool IsPng(Stream stream)
        {
            var start = stream.Position;
            stream.Position = 0;
            var header = new byte[PngSignature.Length];
            var read = ReadFully(stream, header);
            stream.Position = start;
            if (read != header.Length) return false;

            for (var i = 0; i < header.Length; i++)
                if (header[i] != PngSignature[i]) return false;
            return true;
        }

        /// <summary>
        /// Walks the PNG chunk list and returns the absolute position right after IEND,
        /// or -1 if the file is not a valid PNG or a chunk is truncated.
        /// Walking chunks skips their payloads instead of scanning every byte (same approach
        /// as koikatu.js scanPngIend and kkloader get_png_length).
        /// </summary>
        public static long SearchForPngEnd(Stream stream)
        {
            var origin = stream.Position;
            try
            {
                if (!IsPng(stream)) return -1;

                stream.Position = PngSignature.Length;
                var chunkHeader = new byte[8];

                while (true)
                {
                    if (ReadFully(stream, chunkHeader) != chunkHeader.Length)
                        return -1;

                    // Chunk = 4-byte big-endian length + 4-byte type + data + 4-byte CRC.
                    var length = ((uint)chunkHeader[0] << 24)
                                 | ((uint)chunkHeader[1] << 16)
                                 | ((uint)chunkHeader[2] << 8)
                                 | chunkHeader[3];

                    var isIend = chunkHeader[4] == (byte)'I'
                                 && chunkHeader[5] == (byte)'E'
                                 && chunkHeader[6] == (byte)'N'
                                 && chunkHeader[7] == (byte)'D';

                    var chunkEnd = stream.Position + length + 4;
                    if (chunkEnd > stream.Length) return -1;
                    if (isIend) return chunkEnd;

                    stream.Position = chunkEnd;
                }
            }
            finally
            {
                stream.Position = origin;
            }
        }

        /// <summary>
        /// Searches the stream from its current position for the byte sequence (streaming, KMP).
        /// Returns the absolute start position of the first match, or -1. Restores the original position.
        /// </summary>
        public static long SearchForSequence(Stream stream, byte[] pattern)
        {
            var origin = stream.Position;
            var failure = BuildFailureTable(pattern);
            var buffer = new byte[BufferSize];
            var matched = 0;
            long consumed = 0;
            int read;

            try
            {
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (var i = 0; i < read; i++)
                    {
                        var b = buffer[i];
                        while (matched > 0 && b != pattern[matched])
                            matched = failure[matched - 1];

                        if (b == pattern[matched])
                            matched++;

                        if (matched == pattern.Length)
                            return origin + consumed + i + 1 - pattern.Length;
                    }
                    consumed += read;
                }

                return -1;
            }
            finally
            {
                stream.Position = origin;
            }
        }

        private static int[] BuildFailureTable(byte[] pattern)
        {
            var table = new int[pattern.Length];
            var k = 0;
            for (var i = 1; i < pattern.Length; i++)
            {
                while (k > 0 && pattern[i] != pattern[k])
                    k = table[k - 1];

                if (pattern[i] == pattern[k])
                    k++;

                table[i] = k;
            }
            return table;
        }

        private static int ReadFully(Stream stream, byte[] buffer)
        {
            return ReadFully(stream, buffer, buffer.Length);
        }

        private static int ReadFully(Stream stream, byte[] buffer, int count)
        {
            var total = 0;
            int read;
            while (total < count && (read = stream.Read(buffer, total, count - total)) > 0)
                total += read;
            return total;
        }
    }
}
