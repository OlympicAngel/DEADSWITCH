using System;
using System.Collections.Generic;

namespace Deadswitch.Sim.Persistence
{
    /// <summary>Little-endian writer over a growable buffer. Explicit byte order so saves are identical on every platform.</summary>
    internal sealed class ByteWriter
    {
        private readonly List<byte> _bytes = new List<byte>(4096);

        public int Length => _bytes.Count;

        public void U8(byte v)
        {
            _bytes.Add(v);
        }

        public void U16(ushort v)
        {
            _bytes.Add((byte)v);
            _bytes.Add((byte)(v >> 8));
        }

        public void I32(int v)
        {
            U32(unchecked((uint)v));
        }

        public void U32(uint v)
        {
            for (int i = 0; i < 4; i++)
            {
                _bytes.Add((byte)(v >> (i * 8)));
            }
        }

        public void I64(long v)
        {
            U64(unchecked((ulong)v));
        }

        public void U64(ulong v)
        {
            for (int i = 0; i < 8; i++)
            {
                _bytes.Add((byte)(v >> (i * 8)));
            }
        }

        public byte[] ToArray()
        {
            return _bytes.ToArray();
        }

        public ulong Checksum()
        {
            return Fnv.Hash(_bytes, 0, _bytes.Count);
        }
    }

    /// <summary>Little-endian reader that throws <see cref="SaveLoadException"/> (Truncated) instead of reading past the end.</summary>
    internal sealed class ByteReader
    {
        private readonly byte[] _bytes;
        private readonly int _end;
        private int _pos;

        public ByteReader(byte[] bytes, int start, int end)
        {
            _bytes = bytes;
            _pos = start;
            _end = end;
        }

        public int Position => _pos;

        public int Remaining => _end - _pos;

        public byte U8()
        {
            Need(1);
            return _bytes[_pos++];
        }

        public ushort U16()
        {
            Need(2);
            ushort v = (ushort)(_bytes[_pos] | (_bytes[_pos + 1] << 8));
            _pos += 2;
            return v;
        }

        public int I32()
        {
            return unchecked((int)U32());
        }

        public uint U32()
        {
            Need(4);
            uint v = 0;
            for (int i = 0; i < 4; i++)
            {
                v |= (uint)_bytes[_pos + i] << (i * 8);
            }

            _pos += 4;
            return v;
        }

        public long I64()
        {
            return unchecked((long)U64());
        }

        public ulong U64()
        {
            Need(8);
            ulong v = 0;
            for (int i = 0; i < 8; i++)
            {
                v |= (ulong)_bytes[_pos + i] << (i * 8);
            }

            _pos += 8;
            return v;
        }

        /// <summary>Reads a collection length and checks it could possibly fit in the remaining bytes.</summary>
        public int Count(int minBytesPerItem)
        {
            int count = I32();
            if (count < 0 || (long)count * Math.Max(1, minBytesPerItem) > Remaining)
            {
                throw new SaveLoadException(SaveLoadError.Corrupted, "Collection length " + count + " is impossible at byte " + _pos + ".");
            }

            return count;
        }

        private void Need(int n)
        {
            if (_end - _pos < n)
            {
                throw new SaveLoadException(SaveLoadError.Truncated, "Unexpected end of save data at byte " + _pos + ".");
            }
        }
    }

    internal static class Fnv
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Hash(IReadOnlyList<byte> bytes, int start, int end)
        {
            ulong h = Offset;
            unchecked
            {
                for (int i = start; i < end; i++)
                {
                    h ^= bytes[i];
                    h *= Prime;
                }
            }

            return h;
        }
    }
}
