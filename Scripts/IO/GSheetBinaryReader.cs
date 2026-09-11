using System;
using System.IO;
using System.Threading.Tasks;
using LWSerializer;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

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

        public static async Task<GSheetBinaryReader> CreateAsync(string fileName)
        {
            var path = Path.Combine(Application.dataPath, fileName);
            var bytes = await File.ReadAllBytesAsync(path);
            GSheetBinaryReader result = new(bytes);
            return result;
        }
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
    }
}
