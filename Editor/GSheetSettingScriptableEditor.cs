using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsheets.Editor.DiffView;
using Gsheets.Editor.Generator;
using Gsheets.Editor.Utils;
using Gsheets.Editor.DownLoader;
using Gsheets.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Gsheets.Editor
{
    [CustomEditor(typeof(GSheetSettingScriptable))]
    [CanEditMultipleObjects]
    public class GSheetSettingScriptableEditor : UnityEditor.Editor
    {
        public const string MENU_ITEM_PATH = "Tools/- Gsheet -/";
        public const string LOG_KEY = "GSHEETLOGKEY";
        
        #region EditorFunc
        [MenuItem(MENU_ITEM_PATH+"Generate", priority = -1)]
        public static void Menu_Generate()
        {
            _ = GsheetGenerator.Run(GSheetSettingScriptable.Instance);
        }
        
        [MenuItem(MENU_ITEM_PATH+"View GoogleSheet", priority = 0)]
        public static void Menu_OpenSheet()
        {
            Application.OpenURL($"https://docs.google.com/spreadsheets/d/{GSheetSettingScriptable.Instance.SheetID}/edit");
        }
        
        [MenuItem(MENU_ITEM_PATH+"GsheetSetting", priority = -2)]
        public static void Menu_ShowSetting()
        {
            EditorWindow.CreateWindow<GsheetSettingWindow>().Show();
        }
        [MenuItem(MENU_ITEM_PATH+"View Log-Diff")]
        public static void Menu_LogAndDiff()
        {
            EditorWindow.GetWindow<DiffViewerWindow>().Show();
        }
       
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (GSheetSettingScriptable.Instance == null)
            {
                ScriptableCreator.Create<GSheetSettingScriptable>($"Assets/Resources/{GSheetSettingScriptable.FileName}.asset");
            }
        }
        #endregion
        
        public override void OnInspectorGUI()
        {
            GUILayout.Label("GSheet Setting", EditorStyles.largeLabel);
            base.OnInspectorGUI();
            GSheetSettingScriptable scriptable = (GSheetSettingScriptable)target;
            if (scriptable == null)
                return;
            GUILayout.Space(20);
            GUILayout.Label("- LastUpdateInfo \n" + EditorPrefs.GetString(LOG_KEY, "Empty Log"));
            GUILayout.Space(20);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OpenSheet"))
                Menu_OpenSheet();
            
            if (GUILayout.Button("Generator"))
                _ = GsheetGenerator.Run(scriptable);
            GUILayout.EndHorizontal();
            scriptable.OnInspectorGUI();
        }

        public static GSheetSettingScriptable GetScriptable()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(GSheetSettingScriptable)}");
            var asset = AssetDatabase.LoadAssetAtPath<GSheetSettingScriptable>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return asset;
        }
    }
    
    public class GsheetSettingWindow : EditorWindow
    {
        public void CreateGUI()
        {
            var asset = GSheetSettingScriptableEditor.GetScriptable();
            titleContent = new GUIContent(nameof(GsheetSettingWindow));
            // 래퍼 요소 생성: 이 한 줄로 인스펙터가 통째로 들어옵니다.
            InspectorElement inspector = new InspectorElement(asset);
            rootVisualElement.Add(inspector);
        }
    }
}