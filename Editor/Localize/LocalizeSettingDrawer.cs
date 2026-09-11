#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Gsheets.Localize;
//Gemini Generator
namespace Gsheets.Localize.Editor
{
    // 1. LocalizeSetting 자체를 그리는 Drawer
    [CustomPropertyDrawer(typeof(LocalizeSetting))]
    public class LocalizeSettingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sheetNameProp = property.FindPropertyRelative("_sheetName");
            SerializedProperty langCodesProp = property.FindPropertyRelative("_languageCodes");
            SerializedProperty fontSetsProp = property.FindPropertyRelative("_fontSets");

            // 기본 Foldout 그리기
            Rect currentRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label, true);
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // 1. 커스텀 라벨 출력 (요청하신 부분)
                string sheetName = sheetNameProp.stringValue;
                int langCount = langCodesProp.arraySize;
                string infoText = $"[ {sheetName} Sheet : {langCount} translations in use ]";
                
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.normal.textColor = new Color(0.3f, 0.7f, 0.4f); // 가독성을 위한 색상 포인트
                
                EditorGUI.LabelField(currentRect, infoText, labelStyle);
                currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // 2. SheetName 필드
                EditorGUI.PropertyField(currentRect, sheetNameProp);
                currentRect.y += EditorGUI.GetPropertyHeight(sheetNameProp) + EditorGUIUtility.standardVerticalSpacing;

                // 3. FontSets 리스트 필드
                float fontSetsHeight = EditorGUI.GetPropertyHeight(fontSetsProp, true);
                Rect fontSetsRect = new Rect(currentRect.x, currentRect.y, currentRect.width, fontSetsHeight);
                EditorGUI.PropertyField(fontSetsRect, fontSetsProp, true);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            SerializedProperty sheetNameProp = property.FindPropertyRelative("_sheetName");
            SerializedProperty fontSetsProp = property.FindPropertyRelative("_fontSets");

            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Custom Label
            height += EditorGUI.GetPropertyHeight(sheetNameProp) + EditorGUIUtility.standardVerticalSpacing; // SheetName
            height += EditorGUI.GetPropertyHeight(fontSetsProp, true) + EditorGUIUtility.standardVerticalSpacing; // FontSets

            return height;
        }
    }

    // 2. FontSets 리스트 내부의 개별 요소를 예쁘게(한 줄로) 그리는 Drawer
    [CustomPropertyDrawer(typeof(LocalizeFontSet))]
    public class LocalizeFontSetDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty fontProp = property.FindPropertyRelative("Font");
            SerializedProperty modeProp = property.FindPropertyRelative("Mode");

            // 요소 간의 간격 설정
            float padding = 5f;
            float halfWidth = (position.width - padding) / 2f;

            // 라벨을 제외하고 Font와 Mode를 1:1 비율로 한 줄에 배치
            Rect fontRect = new Rect(position.x, position.y, halfWidth, position.height);
            Rect modeRect = new Rect(position.x + halfWidth + padding, position.y, halfWidth, position.height);

            // GUIContent.none을 전달하여 변수명 라벨을 숨기고 깔끔하게 컴포넌트만 출력
            EditorGUI.PropertyField(fontRect, fontProp, GUIContent.none);
            EditorGUI.PropertyField(modeRect, modeProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 한 줄 높이만 사용
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif