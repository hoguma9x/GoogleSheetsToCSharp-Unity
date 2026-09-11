using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gsheets.Editor.Utils
{
    //!!Copy To Rui.Core.IOUtils
    internal class IOUtils
    {

        /// <summary> Asset의 AssetPath를 가져옵니다 AssetDataBase.GetAssetPath와 동일한 결과 </summary>
        public static string GetAssetPath(UnityEngine.Object obj)
        {
            return AssetDatabase.GetAssetPath(obj);
        }

        /// <summary> C/ 부터 시작하는 경로를 완성시켜 반환합니다, subPath에 파일과 확장자를 포함시켜야함</summary>
        public static string GetSystemPath(string subPath)
        {
            subPath = subPath.Replace("Assets/", "");

            return $"{Application.dataPath}/{subPath}";
        }

        /// <summary>
        ///  AssetDataBase를 통해 에셋을 임포트하고 로드해 반환합니다
        /// </summary>
        /// <param name="assetPath">Project/ 으로 시작하는 경로 작성</param>
        public static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        /// <summary> 경로에 바이너리 데이터를 작성합니다 </summary>
        public static void SaveFile(string systemPath, byte[] binary)
        {
            var dir = Path.GetDirectoryName(systemPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            try
            {
                using (FileStream fs = File.Create(systemPath, binary.Length))
                {
                    fs.Write(binary, 0, binary.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        /// <summary> 경로에 바이너리 데이터를 작성합니다 (관리되지않는 배열)</summary>
        public static unsafe void SaveFile(string systemPath, byte* binary, int len)
        {
            ReadOnlySpan<byte> binarySpan = new(binary, len);
            var dir = Path.GetDirectoryName(systemPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            try
            {
                using (FileStream fs = File.Create(systemPath, len))
                {
                    fs.Write(binarySpan);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        /// <summary> 경로에 해당하는 폴더를 제거합니다</summary>
        public static void DeleteDirectory(string systemPath)
        {
            Directory.Delete(systemPath);
        }

        /// <summary> 경로에 해당하는 폴더를 가져옵니다</summary>
        public static string GetDirectory(string systemPath)
        {
            return Path.GetDirectoryName(systemPath);
        }

        #region Find

        /// <summary> 본인을 제외한 하위폴더를 포함하여 조건에 맞는 에셋을 탐색합니다. </summary>
        public static List<T> GetAssetForFolderAllChildren<T>(string foldername, string filterExt = ".asset",
            params Type[] ignoreTypes) where T : UnityEngine.Object
        {
            var allFolders = new List<string>();
            FindAllSubFolders(foldername, allFolders);
            var result = new List<T>();
            foreach (var folder in allFolders)
                result.AddRange(GetAssetsForFolder<T>(folder, filterExt, ignoreTypes));
            return result;
        }

        /// <summary> 지정된 폴더 안의 에셋을 탐색합니다.</summary>
        public static List<T> GetAssetsForFolder<T>(string assetPath, string filterExt = ".asset",
            params Type[] ignoreTypes) where T : UnityEngine.Object
        {
            string originalPath = assetPath;
            assetPath = assetPath.Replace("Assets/", "");
            assetPath = assetPath.Replace("Assets\\", "");
            var result = new List<T>();
            string folderName = $"{Application.dataPath}/{assetPath}/";
            if (!System.IO.Directory.Exists(folderName))
                return result;
            var files = System.IO.Directory.GetFiles(folderName);
            foreach (var f in files)
            {
                var ext = System.IO.Path.GetExtension(f);
                if (filterExt != "" && ext != filterExt)
                    continue;
                if (ext == ".meta")
                    continue;
                var name = System.IO.Path.GetFileName(f);
                var aset = AssetDatabase.LoadMainAssetAtPath($"{originalPath}/{name}");
                if (aset is T)
                {
                    bool Pass = false;
                    foreach (var ignoreT in ignoreTypes)
                        if (ignoreT == aset.GetType())
                        {
                            Pass = true;
                            break;
                        }

                    if (!Pass)
                        result.Add(aset as T);
                }
            }

            return result;
        }

        static void FindAllSubFolders(string folderName, List<string> folders)
        {
            var childs = AssetDatabase.GetSubFolders(folderName);
            folders.AddRange(childs);
            foreach (var child in childs)
                FindAllSubFolders(child, folders);
        }

        #endregion
    }
}