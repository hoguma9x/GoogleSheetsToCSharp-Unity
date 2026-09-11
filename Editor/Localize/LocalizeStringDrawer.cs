using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections;
using Localize.Elements;

namespace Gsheets.Editor.Localize
{
    
    [CustomPropertyDrawer(typeof(LocalizeString))]
    public class LocalizeStringDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1. LocalizeString 클래스 내부의 '_localizeKey' 직렬화 필드를 찾습니다.
            // (실제 클래스 내부 변수명과 일치해야 합니다)
            SerializedProperty keyProp = property.FindPropertyRelative("_localizeKey");

            // 2. 첫 번째 줄: 텍스트 필드가 그려질 영역 계산
            Rect keyRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // SirenixEditorFields.TextField 역할을 하는 순수 유니티 필드
            EditorGUI.PropertyField(keyRect, keyProp, label);

            // 3. 두 번째 줄: 프리뷰 라벨이 그려질 영역 계산 (여백 2px 추가)
            Rect labelRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width,
                EditorGUIUtility.singleLineHeight);

            // 일반 필드들과 시작 선을 맞추기 위해 들여쓰기(Indent)가 적용된 Rect 추출
            Rect indentedRect = EditorGUI.IndentedRect(labelRect);

            // 4. Odin의 DrawGUI 스타일을 순수 IMGUI로 재현
            var originalColor = GUI.color;
            GUI.color = Color.cyan;

            string previewText;
            if (keyProp == null || string.IsNullOrEmpty(keyProp.stringValue))
            {
                previewText = "- Translation is not used.";
            }
            else
            {
                // SerializedProperty로부터 실제 LocalizeString 인스턴스를 역추적하여 GetString()을 호출합니다.
                var targetInstance = PropertyUtils.GetTargetObjectOfProperty(property) as LocalizeString;
                previewText =
                    "- " + (targetInstance != null ? targetInstance.GetString() : "Error: Instance not found");
            }

            // 글자 크기나 스타일을 가볍게 조절하고 싶다면 EditorStyles.miniLabel 등을 활용할 수 있습니다.
            EditorGUI.LabelField(indentedRect, previewText, EditorStyles.label);

            GUI.color = originalColor;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 텍스트 필드(1줄) + 프리뷰 라벨(1줄) + 줄 사이 여백(2px)
            return EditorGUIUtility.singleLineHeight * 2 + 2;
        }
    }
}