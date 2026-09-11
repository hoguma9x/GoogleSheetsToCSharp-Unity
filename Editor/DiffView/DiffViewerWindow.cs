using System.Collections.Generic;
using System.IO;
using System.Text;
using Gsheets.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Gsheets.Editor.DiffView
{
    public class DiffViewerWindow : EditorWindow
    {
        private string _diffText;

        public void Refresh(object beforeGsheetData)
        {
            var before = (Dictionary<string, object>)beforeGsheetData;
            var after = (Dictionary<string, object>)GsheetDiffHelper.Capture(GSheetSettingScriptable.Instance
                .FindGSheetInstance());
            _diffText = JsonDiffDrawer.GenerateDiffText(before, after);

            IOUtils.SaveFile(GetHistoryFilePath("DiffViewerLog"), Encoding.UTF8.GetBytes(_diffText));
        }

        private void OnEnable()
        {
            string LoadHistory(string name)
            {
                if (File.Exists(GetHistoryFilePath(name)))
                    return Encoding.UTF8.GetString(File.ReadAllBytes(GetHistoryFilePath(name)));
                return null;
            }
            _diffText = LoadHistory("DiffViewerLog");
        }

        string GetHistoryFilePath(string name)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "UserSettings",
                $"diffHistory_{name}.json");
        }

        private void OnGUI()
        {
            GUILayout.Label("- DiffViewerWindow - ");
            GUILayout.Label(EditorPrefs.GetString(GSheetSettingScriptableEditor.LOG_KEY));
            GUILayout.Space(10);
            JsonDiffDrawer.DrawTextLog(_diffText);
        }
    }
}
