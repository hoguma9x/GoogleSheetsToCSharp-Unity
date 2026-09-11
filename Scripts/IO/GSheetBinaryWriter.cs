using System;
using System.IO;
using LWSerializer;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gsheets.IO
{
    public class GSheetBinaryWriter : LwBinaryWriter
    {
        private string _fileName = "";
        
        private GSheetBinaryWriter() : base(1024)
        {
        }

        public static GSheetBinaryWriter Create(string filename)
        {
            GSheetBinaryWriter result = new();
            result._fileName = filename;
            return result;
        }

        public void Save()
        {
#if UNITY_EDITOR
            var path = Path.Combine(Application.dataPath, _fileName);
            ReadOnlySpan<byte> binarySpan = ToPtr().AsSpan(Length);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            try
            {
                using (FileStream fs = File.Create(path, Length))
                {
                    fs.Write(binarySpan);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            AssetDatabase.ImportAsset("Assets/"+_fileName, ImportAssetOptions.ForceSynchronousImport);
#else
            throw new NotImplementedException("Not implemented");            
#endif
        }

    }
}
