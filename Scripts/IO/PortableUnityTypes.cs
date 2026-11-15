#if !UNITY_5_3_OR_NEWER
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Gsheets.Compatibility
{
    // Match Unity's unmanaged wire layouts; these types do not provide Unity engine behavior.
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2
    {
        public float x,y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public float x,y,z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4
    {
        public float x,y,z,w;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion
    {
        public float x,y,z,w;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2Int
    {
        public int x,y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3Int
    {
        public int x,y,z;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Color
    {
        public float r,g,b,a;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public unsafe struct FixedString32Bytes
    {
        private ushort _length;
        private fixed byte _bytes[30];

        public override string ToString()
        {
            if (_length > 29) throw new InvalidDataException("Invalid FixedString length.");
            fixed (byte* bytes = _bytes) return Encoding.UTF8.GetString(bytes, _length);
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    public unsafe struct FixedString64Bytes
    {
        private ushort _length;
        private fixed byte _bytes[62];

        public override string ToString()
        {
            if (_length > 61) throw new InvalidDataException("Invalid FixedString length.");
            fixed (byte* bytes = _bytes) return Encoding.UTF8.GetString(bytes, _length);
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 128)]
    public unsafe struct FixedString128Bytes
    {
        private ushort _length;
        private fixed byte _bytes[126];

        public override string ToString()
        {
            if (_length > 125) throw new InvalidDataException("Invalid FixedString length.");
            fixed (byte* bytes = _bytes) return Encoding.UTF8.GetString(bytes, _length);
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 512)]
    public unsafe struct FixedString512Bytes
    {
        private ushort _length;
        private fixed byte _bytes[510];

        public override string ToString()
        {
            if (_length > 509) throw new InvalidDataException("Invalid FixedString length.");
            fixed (byte* bytes = _bytes) return Encoding.UTF8.GetString(bytes, _length);
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 4096)]
    public unsafe struct FixedString4096Bytes
    {
        private ushort _length;
        private fixed byte _bytes[4094];

        public override string ToString()
        {
            if (_length > 4093) throw new InvalidDataException("Invalid FixedString length.");
            fixed (byte* bytes = _bytes) return Encoding.UTF8.GetString(bytes, _length);
        }
    }

}
#endif
