using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsheets.Internal.LWSerializer;
using SheetData.Editor.DiffView;
using SheetData.Editor.DownLoader;
using SheetData.Editor.Utils;
using SheetData.IO;
using UnityEditor;
using UnityEngine;

namespace SheetData.Editor.Generator
{
    public static class GsheetGenerator
    {
        private static string ProgressTitle = "Gsheet Generator";
        /// <summary> 데이터를 생성하고 Gsheet 클래스를 생성합니다 </summary>
        public static async Task Run(SheetDataSettingScriptable target)
        {
            EditorUtility.DisplayProgressBar(ProgressTitle, "Start Generate", 0.1f);
            try
            {
                EditorUtility.DisplayProgressBar(ProgressTitle, "Capture BeforeData", 0.3f);
                var beforeGsheetData = GsheetDiffHelper.Capture(SheetDataSettingScriptable.Instance.FindGSheetInstance());
                EditorUtility.DisplayProgressBar(ProgressTitle, "Refresh GoogleSheet Names", 0.3f);
                target.OnBeginGenerator();
                bool successRefresh = await RefreshSheetNames(target);
                if (!successRefresh)
                    throw new Exception("sheet does not exist. Please check the URL");
                EditorUtility.DisplayProgressBar(ProgressTitle, "Refresh And Parsing GoogleSheet Datas", 0.5f);
                List<SheetRawData> sheetDatas = new();
                for (int i = 0; i < target.SheetInfos.Count; i++)
                {
                    var raw = await SheetLoader.Load(target.SheetID, target.SheetInfos[i]);
                    sheetDatas.Add(raw);
                    foreach (var header in raw.Headers)
                    {
                        if (header.IsMissingType)
                            throw new Exception($"Header '{header.originalText}' is missing type");
                    }
                }
                EditorUtility.DisplayProgressBar(ProgressTitle, "Create LwBinary Data", 0.7f);
                Dictionary<string, TypeModel> modelMap = new Dictionary<string, TypeModel>();
                SheetBinaryWriter writer = SheetBinaryWriter.Create($"Resources/{SheetDataSettingScriptable.BinaryFileName}.bytes");
                writer.Write(sheetDatas.Count);
                foreach (var sheetData in sheetDatas)
                {
                    if (!sheetData.IsExternalSheet())
                        modelMap.Add(sheetData.SheetName, sheetData.ClassGenerator(target.GeneratorNameSpace));
                    sheetData.WriteDirect(writer);
                    if (sheetData.SheetName == target.LocalizeSetting.SheetName)
                    {
                        target.LocalizeSetting.LanguageCodes =
                            sheetData.Headers.Skip(1).Select(o => o.memberName).ToArray();
                        CreateLocalizeEnums(target);
                    }
                }
                EditorUtility.DisplayProgressBar(ProgressTitle, "Save LwBinary Data", 0.7f);
                int writerSize = writer.Length;
                Debug.Log($"size {writerSize}");
                writer.Save();
                writer.Dispose();
                
                EditorUtility.DisplayProgressBar(ProgressTitle, "Generating Cshape Script", 0.9f);
                foreach (var sheetData in sheetDatas)
                {
                    if(sheetData.IsExternalSheet())
                        continue;
                    var generatorCode = modelMap[sheetData.SheetName].Generator();
                    if (generatorCode != "")
                    {
                        string path = IOUtils.GetSystemPath($"{target.CodeGenerationPath}/{sheetData.SheetName}.cs");
                        IOUtils.SaveFile(path, Encoding.UTF8.GetBytes(generatorCode));
                    }
                }

                GSheetModel model = new GSheetModel(sheetDatas.ToArray(), target.GeneratorNameSpace);
                IOUtils.SaveFile(IOUtils.GetSystemPath($"{target.CodeGenerationPath}/{GSheetModel.NAME}.cs"),
                    Encoding.UTF8.GetBytes(model.Generator()));
                EditorPrefs.SetString(SheetDataSettingScriptableEditor.LOG_KEY,
                    $"BinarySize - {writerSize:N0} bytes, Updated - {DateTime.Now.ToString()}");
                AssetDatabase.Refresh();
                EditorUtility.DisplayProgressBar(ProgressTitle, "Call OnEndGenerator()", 1f);
                target.OnEndGenerator();

                var win = EditorWindow.GetWindow<DiffViewerWindow>();
                win.Refresh(beforeGsheetData);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            EditorUtility.ClearProgressBar();
        }

        /// <summary> 시트의 이름들을 갱신하고 Scriptable에 메타데이터로 등록합니다 </summary>
        static async Task<bool> RefreshSheetNames(SheetDataSettingScriptable target)
        {
            target.SheetInfos.Clear();
            var names = await SheetLoader.GetSheetNames(target.SheetID);
            if (names.Count > 0)
            {
                target.SheetInfos.AddRange(names);
                EditorUtility.SetDirty(target);
                return true;
            }
            return false;
        }
        
        /// <summary> 지정된 Localize Sheet를 참조해 번역대상 언어코드를 생성합니다. </summary>
        static void CreateLocalizeEnums(SheetDataSettingScriptable target)
        {
            if(string.IsNullOrEmpty(target.LocalizeSetting.SheetName))
                return;
            var langs = Enum.GetNames(typeof(LangCode)).ToHashSet();
            foreach (var code in target.LocalizeSetting.LanguageCodes)
                if (langs.Contains(code))
                    langs.Remove(code);
                else 
                    langs.Add(code);
            if (langs.Count > 0)
            {
                string targetDirectory = "Assets/Plugins/GoogleSheetToData/";
                string[] guids = AssetDatabase.FindAssets("LangCode t:script");

                if (guids.Length > 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    targetDirectory = Path.GetDirectoryName(assetPath).Replace("\\", "/") + "/";
                }
                else
                {
                    Debug.LogWarning("LangCode.cs 파일을 찾을 수 없어 기본 경로에 생성합니다.");
                }
                EnumCreator creator = new EnumCreator("LangCode", targetDirectory, "SheetData");
                foreach (var code in target.LocalizeSetting.LanguageCodes)
                    creator.AddEnum(code);
                creator.Generator();
            }
        }
    }
}
