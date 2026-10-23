using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KoikatsuCardReader.Util
{
    /// <summary>
    /// Stream reader for the MessagePack primitives, values and containers used by card data.
    /// Skip operations can optionally capture the raw encoded bytes.
    /// </summary>
    internal static class MessagePackUtil
    {
        private const int MaxStringLength = 64 * 1024;

        public sealed class MessagePackExtension
        {
            public sbyte TypeCode { get; }
            public byte[] Data { get; }

            public MessagePackExtension(sbyte typeCode, byte[] data)
            {
                TypeCode = typeCode;
                Data = data;
            }
        }

        public static void ReadNil(BinaryReader reader)
        {
            if (reader.ReadByte() != 0xc0)
                throw new InvalidDataException("MessagePack value is not nil");
        }

        public static bool ReadBoolean(BinaryReader reader)
        {
            var code = reader.ReadByte();
            if (code == 0xc2) return false;
            if (code == 0xc3) return true;
            throw new InvalidDataException("MessagePack value is not a boolean");
        }

        public static byte ReadByte(BinaryReader reader)
        {
            return checked((byte)ReadUInt64(reader));
        }

        public static sbyte ReadSByte(BinaryReader reader)
        {
            return checked((sbyte)ReadInt64(reader));
        }

        public static short ReadInt16(BinaryReader reader)
        {
            return checked((short)ReadInt64(reader));
        }

        public static ushort ReadUInt16(BinaryReader reader)
        {
            return checked((ushort)ReadUInt64(reader));
        }

        public static int ReadInt32(BinaryReader reader)
        {
            return checked((int)ReadInt64(reader));
        }

        public static uint ReadUInt32(BinaryReader reader)
        {
            return checked((uint)ReadUInt64(reader));
        }

        public static long ReadInt64(BinaryReader reader)
        {
            return ReadInt64(reader, reader.ReadByte());
        }

        public static ulong ReadUInt64(BinaryReader reader)
        {
            return ReadUInt64(reader, reader.ReadByte());
        }

        public static float ReadSingle(BinaryReader reader)
        {
            if (reader.ReadByte() != 0xca)
                throw new InvalidDataException("MessagePack value is not a float");

            return ToSingle(ReadRawBytes(reader, 4));
        }

        public static double ReadDouble(BinaryReader reader)
        {
            if (reader.ReadByte() != 0xcb)
                throw new InvalidDataException("MessagePack value is not a double");

            return ToDouble(ReadRawBytes(reader, 8));
        }

        public static string ReadString(BinaryReader reader)
        {
            return ReadString(reader, reader.ReadByte());
        }

        public static byte[] ReadBytes(BinaryReader reader)
        {
            return ReadBytes(reader, reader.ReadByte());
        }

        public static long ReadArrayHeader(BinaryReader reader)
        {
            return GetArrayLength(reader, reader.ReadByte());
        }

        public static int GetArrayHeaderLength(byte code)
        {
            if (code >= 0x90 && code <= 0x9f) return 1;
            if (code == 0xdc) return 3;
            if (code == 0xdd) return 5;
            return 0;
        }

        public static object[] ReadArray(BinaryReader reader)
        {
            return ReadArray(reader, ReadArrayHeader(reader));
        }

        public static long ReadMapHeader(BinaryReader reader)
        {
            return GetMapLength(reader, reader.ReadByte());
        }

        public static int GetMapHeaderLength(byte code)
        {
            if (code >= 0x80 && code <= 0x8f && (code & 0x0f) > 0) return 1;
            if (code == 0xde) return 3;
            if (code == 0xdf) return 5;
            return 0;
        }

        public static Dictionary<object, object> ReadMap(BinaryReader reader)
        {
            return ReadMap(reader, ReadMapHeader(reader));
        }

        public static byte[] ReadExtension(BinaryReader reader, out sbyte typeCode)
        {
            return ReadExtension(reader, reader.ReadByte(), out typeCode);
        }

        public static object ReadValue(BinaryReader reader)
        {
            var code = reader.ReadByte();

            switch (code)
            {
                case <= 0x7f:
                    return (long)code;
                case >= 0xe0:
                    return (long)unchecked((sbyte)code);
                case >= 0x80 and <= 0x8f:
                    return ReadMap(reader, code & 0x0f);
                case >= 0x90 and <= 0x9f:
                    return ReadArray(reader, code & 0x0f);
                case >= 0xa0 and <= 0xbf:
                    return ReadString(reader, code);
                case 0xc0:
                    return null;
                case 0xc2:
                    return false;
                case 0xc3:
                    return true;
                case 0xc4:
                case 0xc5:
                case 0xc6:
                    return ReadBytes(reader, code);
                case 0xc7:
                case 0xc8:
                case 0xc9:
                case 0xd4:
                case 0xd5:
                case 0xd6:
                case 0xd7:
                case 0xd8:
                    return ReadExtensionValue(reader, code);
                case 0xca:
                    return ToSingle(ReadRawBytes(reader, 4));
                case 0xcb:
                    return ToDouble(ReadRawBytes(reader, 8));
                case 0xcc:
                case 0xcd:
                case 0xce:
                    return (long)ReadUInt64(reader, code);
                case 0xcf:
                    return ReadUInt64(reader, code);
                case 0xd0:
                case 0xd1:
                case 0xd2:
                case 0xd3:
                    return ReadInt64(reader, code);
                case 0xd9:
                case 0xda:
                case 0xdb:
                    return ReadString(reader, code);
                case 0xdc:
                case 0xdd:
                    return ReadArray(reader, GetArrayLength(reader, code));
                case 0xde:
                case 0xdf:
                    return ReadMap(reader, GetMapLength(reader, code));
                default:
                    throw new InvalidDataException($"Unsupported MessagePack code 0x{code:X2}");
            }
        }

        public static void SkipValue(BinaryReader reader)
        {
            SkipValue(reader, null);
        }

        public static void SkipValue(BinaryReader reader, MemoryStream capture)
        {
            var code = ReadRawByte(reader, capture);
            SkipValue(reader, capture, code);
        }

        public static void SkipArray(BinaryReader reader, long count)
        {
            SkipArray(reader, null, count);
        }

        public static void SkipMap(BinaryReader reader, long count)
        {
            SkipMap(reader, null, count);
        }

        public static void SkipBytes(BinaryReader reader, long count)
        {
            SkipBytes(reader, count, null);
        }

        public static byte[] BuildStringPattern(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length <= 31)
            {
                var pattern = new byte[1 + bytes.Length];
                pattern[0] = (byte)(0xA0 | bytes.Length);
                Buffer.BlockCopy(bytes, 0, pattern, 1, bytes.Length);
                return pattern;
            }

            if (bytes.Length <= byte.MaxValue)
            {
                var pattern = new byte[2 + bytes.Length];
                pattern[0] = 0xD9;
                pattern[1] = (byte)bytes.Length;
                Buffer.BlockCopy(bytes, 0, pattern, 2, bytes.Length);
                return pattern;
            }

            var longPattern = new byte[3 + bytes.Length];
            longPattern[0] = 0xDA;
            longPattern[1] = (byte)(bytes.Length >> 8);
            longPattern[2] = (byte)bytes.Length;
            Buffer.BlockCopy(bytes, 0, longPattern, 3, bytes.Length);
            return longPattern;
        }

        private static long ReadInt64(BinaryReader reader, byte code)
        {
            if (code <= 0x7f) return code;
            if (code >= 0xe0) return unchecked((sbyte)code);

            switch (code)
            {
                case 0xcc: return ReadRawByte(reader);
                case 0xcd: return ReadRawUInt16(reader);
                case 0xce: return ReadRawUInt32(reader);
                case 0xcf: return checked((long)ReadRawUInt64(reader));
                case 0xd0: return ReadRawSByte(reader);
                case 0xd1: return ReadRawInt16(reader);
                case 0xd2: return ReadRawInt32(reader);
                case 0xd3: return ReadRawInt64(reader);
                default: throw new InvalidDataException("MessagePack value is not an integer");
            }
        }

        private static ulong ReadUInt64(BinaryReader reader, byte code)
        {
            if (code <= 0x7f) return code;

            switch (code)
            {
                case 0xcc: return ReadRawByte(reader);
                case 0xcd: return ReadRawUInt16(reader);
                case 0xce: return ReadRawUInt32(reader);
                case 0xcf: return ReadRawUInt64(reader);
                default: throw new InvalidDataException("MessagePack value is not an unsigned integer");
            }
        }

        private static long GetArrayLength(BinaryReader reader, byte code)
        {
            if (code >= 0x90 && code <= 0x9f) return code & 0x0f;
            if (code == 0xdc) return ReadRawUInt16(reader);
            if (code == 0xdd) return ReadRawUInt32(reader);
            throw new InvalidDataException("MessagePack value is not an array");
        }

        private static long GetMapLength(BinaryReader reader, byte code)
        {
            if (code >= 0x80 && code <= 0x8f) return code & 0x0f;
            if (code == 0xde) return ReadRawUInt16(reader);
            if (code == 0xdf) return ReadRawUInt32(reader);
            throw new InvalidDataException("MessagePack value is not a map");
        }

        private static string ReadString(BinaryReader reader, byte code)
        {
            long length;
            if (code >= 0xa0 && code <= 0xbf)
                length = code & 0x1f;
            else if (code == 0xd9)
                length = ReadRawByte(reader);
            else if (code == 0xda)
                length = ReadRawUInt16(reader);
            else if (code == 0xdb)
                length = ReadRawUInt32(reader);
            else
                throw new InvalidDataException("MessagePack value is not a string");

            if (length < 0 || length > MaxStringLength)
                throw new InvalidDataException("Invalid string length");

            var bytes = ReadRawBytes(reader, (int)length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static byte[] ReadBytes(BinaryReader reader, byte code)
        {
            long length;
            if (code == 0xc4)
                length = ReadRawByte(reader);
            else if (code == 0xc5)
                length = ReadRawUInt16(reader);
            else if (code == 0xc6)
                length = ReadRawUInt32(reader);
            else
                throw new InvalidDataException("MessagePack value is not binary");

            if (length < 0 || length > int.MaxValue)
                throw new InvalidDataException("Invalid binary length");

            return ReadRawBytes(reader, (int)length);
        }

        private static byte[] ReadExtension(BinaryReader reader, byte code, out sbyte typeCode)
        {
            long length;

            if (code >= 0xd4 && code <= 0xd8)
            {
                length = 1L << (code - 0xd4);
            }
            else if (code == 0xc7)
            {
                length = ReadRawByte(reader);
            }
            else if (code == 0xc8)
            {
                length = ReadRawUInt16(reader);
            }
            else if (code == 0xc9)
            {
                length = ReadRawUInt32(reader);
            }
            else
            {
                throw new InvalidDataException("MessagePack value is not an extension");
            }

            if (length < 0 || length > int.MaxValue)
                throw new InvalidDataException("Invalid extension length");

            typeCode = ReadRawSByte(reader);
            return ReadRawBytes(reader, (int)length);
        }

        private static MessagePackExtension ReadExtensionValue(BinaryReader reader, byte code)
        {
            var data = ReadExtension(reader, code, out var typeCode);
            return new MessagePackExtension(typeCode, data);
        }

        private static object[] ReadArray(BinaryReader reader, long count)
        {
            if (count < 0 || count > int.MaxValue)
                throw new InvalidDataException("Invalid array length");

            var values = new object[count];
            for (long i = 0; i < count; i++)
                values[i] = ReadValue(reader);

            return values;
        }

        private static Dictionary<object, object> ReadMap(BinaryReader reader, long count)
        {
            var values = new Dictionary<object, object>();
            for (long i = 0; i < count; i++)
            {
                var key = ReadValue(reader);
                if (key == null)
                    throw new InvalidDataException("MessagePack map key is nil");

                values[key] = ReadValue(reader);
            }

            return values;
        }

        private static byte ReadRawByte(BinaryReader reader, MemoryStream capture = null)
        {
            var value = reader.ReadByte();
            capture?.WriteByte(value);
            return value;
        }

        private static sbyte ReadRawSByte(BinaryReader reader, MemoryStream capture = null)
        {
            return unchecked((sbyte)ReadRawByte(reader, capture));
        }

        private static short ReadRawInt16(BinaryReader reader, MemoryStream capture = null)
        {
            return unchecked((short)ReadRawUInt16(reader, capture));
        }

        private static ushort ReadRawUInt16(BinaryReader reader, MemoryStream capture = null)
        {
            var bytes = ReadRawBytes(reader, 2, capture);
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        private static int ReadRawInt32(BinaryReader reader, MemoryStream capture = null)
        {
            return unchecked((int)ReadRawUInt32(reader, capture));
        }

        private static uint ReadRawUInt32(BinaryReader reader, MemoryStream capture = null)
        {
            var bytes = ReadRawBytes(reader, 4, capture);
            return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        }

        private static long ReadRawInt64(BinaryReader reader, MemoryStream capture = null)
        {
            return unchecked((long)ReadRawUInt64(reader, capture));
        }

        private static ulong ReadRawUInt64(BinaryReader reader, MemoryStream capture = null)
        {
            var bytes = ReadRawBytes(reader, 8, capture);
            return ((ulong)bytes[0] << 56) | ((ulong)bytes[1] << 48) | ((ulong)bytes[2] << 40) | ((ulong)bytes[3] << 32)
                 | ((ulong)bytes[4] << 24) | ((ulong)bytes[5] << 16) | ((ulong)bytes[6] << 8) | bytes[7];
        }

        private static byte[] ReadRawBytes(BinaryReader reader, int count, MemoryStream capture = null)
        {
            var bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
                throw new EndOfStreamException("MessagePack data is truncated");

            capture?.Write(bytes, 0, count);
            return bytes;
        }

        private static float ToSingle(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToSingle(bytes, 0);
        }

        private static double ToDouble(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToDouble(bytes, 0);
        }

        private static void SkipValue(BinaryReader reader, MemoryStream capture, byte code)
        {
            switch (code)
            {
                case <= 0x7f:
                case >= 0xe0:
                    return;
                case >= 0x80 and <= 0x8f:
                    SkipMap(reader, capture, code & 0x0f);
                    return;
                case >= 0x90 and <= 0x9f:
                    SkipArray(reader, capture, code & 0x0f);
                    return;
                case >= 0xa0 and <= 0xbf:
                    SkipBytes(reader, code & 0x1f, capture);
                    return;
                case 0xc0:
                case 0xc2:
                case 0xc3:
                    return;
                case 0xc4:
                    SkipBytes(reader, ReadRawByte(reader, capture), capture);
                    return;
                case 0xc5:
                    SkipBytes(reader, ReadRawUInt16(reader, capture), capture);
                    return;
                case 0xc6:
                    SkipBytes(reader, ReadRawUInt32(reader, capture), capture);
                    return;
                case 0xc7:
                    SkipBytes(reader, 1 + ReadRawByte(reader, capture), capture);
                    return;
                case 0xc8:
                    SkipBytes(reader, 1 + (long)ReadRawUInt16(reader, capture), capture);
                    return;
                case 0xc9:
                    SkipBytes(reader, 1 + (long)ReadRawUInt32(reader, capture), capture);
                    return;
                case 0xca:
                case 0xce:
                case 0xd2:
                    SkipBytes(reader, 4, capture);
                    return;
                case 0xcb:
                case 0xcf:
                case 0xd3:
                    SkipBytes(reader, 8, capture);
                    return;
                case 0xcc:
                case 0xd0:
                    SkipBytes(reader, 1, capture);
                    return;
                case 0xcd:
                case 0xd1:
                    SkipBytes(reader, 2, capture);
                    return;
                case 0xd4:
                    SkipBytes(reader, 2, capture);
                    return;
                case 0xd5:
                    SkipBytes(reader, 3, capture);
                    return;
                case 0xd6:
                    SkipBytes(reader, 5, capture);
                    return;
                case 0xd7:
                    SkipBytes(reader, 9, capture);
                    return;
                case 0xd8:
                    SkipBytes(reader, 17, capture);
                    return;
                case 0xd9:
                    SkipBytes(reader, ReadRawByte(reader, capture), capture);
                    return;
                case 0xda:
                    SkipBytes(reader, ReadRawUInt16(reader, capture), capture);
                    return;
                case 0xdb:
                    SkipBytes(reader, ReadRawUInt32(reader, capture), capture);
                    return;
                case 0xdc:
                    SkipArray(reader, capture, ReadRawUInt16(reader, capture));
                    return;
                case 0xdd:
                    SkipArray(reader, capture, ReadRawUInt32(reader, capture));
                    return;
                case 0xde:
                    SkipMap(reader, capture, ReadRawUInt16(reader, capture));
                    return;
                case 0xdf:
                    SkipMap(reader, capture, ReadRawUInt32(reader, capture));
                    return;
                default:
                    throw new InvalidDataException($"Unsupported MessagePack code 0x{code:X2}");
            }
        }

        private static void SkipMap(BinaryReader reader, MemoryStream capture, long count)
        {
            for (long i = 0; i < count; i++)
            {
                SkipValue(reader, capture);
                SkipValue(reader, capture);
            }
        }

        private static void SkipArray(BinaryReader reader, MemoryStream capture, long count)
        {
            for (long i = 0; i < count; i++)
                SkipValue(reader, capture);
        }

        private static void SkipBytes(BinaryReader reader, long count, MemoryStream capture)
        {
            if (count < 0)
                throw new InvalidDataException("Invalid data length");

            if (capture == null)
            {
                var stream = reader.BaseStream;
                if (count > stream.Length - stream.Position)
                    throw new EndOfStreamException("MessagePack data is truncated");

                stream.Seek(count, SeekOrigin.Current);
                return;
            }

            var buffer = new byte[16 * 1024];
            while (count > 0)
            {
                var read = reader.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                if (read <= 0)
                    throw new EndOfStreamException("MessagePack data is truncated");

                capture.Write(buffer, 0, read);
                count -= read;
            }
        }
    }
}
