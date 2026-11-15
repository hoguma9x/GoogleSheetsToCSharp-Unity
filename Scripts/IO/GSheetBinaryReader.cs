using System;
using System.IO;
using System.Threading.Tasks;
using LWSerializer;
#if UNITY_5_3_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
#endif

namespace Gsheets.IO
{
    public class GSheetBinaryReader : LwBinaryReader
    {
        private GSheetBinaryReader(LwNativePointer<byte> span) : base(span)
        { }
        private GSheetBinaryReader(IntPtr binaryData) : base(binaryData)
        { }
        private GSheetBinaryReader(byte[] binaryData) : base(binaryData)
        { }

        public static GSheetBinaryReader FromBytes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < sizeof(int)) throw new InvalidDataException("Missing GSheet file header.");
            return new GSheetBinaryReader(data);
        }

        public static GSheetBinaryReader FromFile(string path) => FromBytes(File.ReadAllBytes(path));

        public static async Task<GSheetBinaryReader> CreateAsync(string fileName)
        {
            var path = fileName;
#if UNITY_5_3_OR_NEWER
            path = Path.Combine(Application.dataPath, fileName);
#endif
            var bytes = await File.ReadAllBytesAsync(path);
            return FromBytes(bytes);
        }
#if UNITY_5_3_OR_NEWER
        /// <summary> Resource.Load 를 이용해 바이너리를 불러옵니다 </summary>
        public static GSheetBinaryReader Create(string resourceName)
        {
            unsafe
            {
                var textAsset = Resources.Load<TextAsset>(resourceName);
                if (textAsset == null)
                    return null;
                var bytes =  textAsset.GetData<byte>();
                GSheetBinaryReader result = new GSheetBinaryReader(new IntPtr(bytes.GetUnsafePtr()));
                return result;
            }
        }
#endif
    }
}
