using Gsheets.SheetData;
using Localize;
using Localize.Elements;
using UnityEditor;
using UnityEngine;
using TMPro.EditorUtilities;

namespace Gsheets.Editor.Localize
{
    [CustomEditor(typeof(TextMeshProLocalizeUGUI), true), CanEditMultipleObjects]
    public class LocalizeTMPTextEditor : TMP_EditorPanelUI
    {
        static bool LocalizeFoldout = true;
        SerializedProperty localizeKeyProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            localizeKeyProp = serializedObject.FindProperty("_localizeKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Rect rect = EditorGUILayout.GetControlRect(false, 24);
            if (GUI.Button(rect, new GUIContent("<b>Localize(Tofu)</b>"), TMP_UIStyleManager.sectionHeader))
                LocalizeFoldout = !LocalizeFoldout;

            var tmpt = target as TextMeshProLocalizeUGUI;

            if (tmpt.LocalizeKey == null)
                tmpt.LocalizeKey = new LocalizeString();

            if (LocalizeFoldout)
            {
                if (localizeKeyProp != null)
                {
                    // true 파라미터를 넘겨 클래스 내부의 하위 프로퍼티를 트리 형태로 모두 그려줍니다.
                    EditorGUILayout.PropertyField(localizeKeyProp, new GUIContent("Localize Key"), true);
                }
                else
                {
                    EditorGUILayout.HelpBox("Cannot find the LocalizeKey property. Please check the field name",
                        MessageType.Warning);
                }
            }

            bool isModified = serializedObject.ApplyModifiedProperties();

            if (isModified)
            {
                tmpt.text = tmpt.LocalizeKey.GetString();
                EditorUtility.SetDirty(tmpt);
            }

            base.OnInspectorGUI();
        }
    }
}