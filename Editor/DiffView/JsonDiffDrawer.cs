using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Gsheets.Editor.DiffView
{
    public static class JsonDiffDrawer
    {
        private static Vector2 scrollPos;

        // =========================================================
        // [1] 데이터 비교 및 텍스트 로그 생성 파트
        // =========================================================

        public static string GenerateDiffText(object oldData, object newData)
        {
            if (oldData == null && newData == null) return string.Empty;

            if (!HasChanges(oldData, newData))
            {
                return "변경된 데이터가 없습니다.";
            }

            StringBuilder sb = new StringBuilder();
            BuildNodeDiffFlat(sb, "Root", oldData, newData);
            return sb.ToString();
        }

        private static void BuildNodeDiffFlat(StringBuilder sb, string path, object a, object b)
        {
            if (!HasChanges(a, b)) return;

            if (a != null && b != null && a.GetType() != b.GetType())
            {
                sb.AppendLine($"[TYPEChange] {path.Replace("Root.", "")} ({a.GetType().Name} -> {b.GetType().Name})");
                return;
            }

            bool isDict = (a is Dictionary<string, object>) || (b is Dictionary<string, object>);
            bool isList = (a is List<object>) || (b is List<object>);

            if (isDict)
            {
                var dictA = a as Dictionary<string, object> ?? new Dictionary<string, object>();
                var dictB = b as Dictionary<string, object> ?? new Dictionary<string, object>();

                HashSet<string> allKeys = new HashSet<string>(dictA.Keys);
                allKeys.UnionWith(dictB.Keys);

                foreach (var key in allKeys)
                {
                    dictA.TryGetValue(key, out object valA);
                    dictB.TryGetValue(key, out object valB);
                    BuildNodeDiffFlat(sb, $"{path}.{key}", valA, valB);
                }
            }
            else if (isList)
            {
                var listA = a as List<object>;
                var listB = b as List<object>;

                if (IsSimpleList(listA) && IsSimpleList(listB))
                {
                    string strA = GetSimpleListString(listA);
                    string strB = GetSimpleListString(listB);

                    if (a == null && b != null)
                        sb.AppendLine($"[ADD] {path.Replace("Root.", "")}: {strB}");
                    else if (a != null && b == null)
                        sb.AppendLine($"[REMOVE] {path.Replace("Root.", "")}: {strA}");
                    else
                        sb.AppendLine($"[INSERT] {path.Replace("Root.", "")}: {strA} -> {strB}");
                }
                else
                {
                    var safeListA = listA ?? new List<object>();
                    var safeListB = listB ?? new List<object>();

                    int maxCount = Mathf.Max(safeListA.Count, safeListB.Count);
                    for (int i = 0; i < maxCount; i++)
                    {
                        object itemA = i < safeListA.Count ? safeListA[i] : null;
                        object itemB = i < safeListB.Count ? safeListB[i] : null;
                        BuildNodeDiffFlat(sb, $"{path}[{i}]", itemA, itemB);
                    }
                }
            }
            else
            {
                // FormatValue 함수를 통해 문자열인 경우 따옴표 처리
                if (a == null && b != null)
                    sb.AppendLine($"[ADD] {path.Replace("Root.", "")}: {FormatValue(b)}");
                else if (a != null && b == null)
                    sb.AppendLine($"[REMOVE] {path.Replace("Root.", "")}: {FormatValue(a)}");
                else if (!a.Equals(b))
                    sb.AppendLine($"[INSERT] {path.Replace("Root.", "")}: {FormatValue(a)} -> {FormatValue(b)}");
            }
        }

        private static bool HasChanges(object a, object b)
        {
            if (a == null && b == null) return false;
            if (a == null || b == null) return true; 
            if (a.GetType() != b.GetType()) return true; 

            if (a is Dictionary<string, object> dictA && b is Dictionary<string, object> dictB)
            {
                HashSet<string> allKeys = new HashSet<string>(dictA.Keys);
                allKeys.UnionWith(dictB.Keys);
                foreach (var key in allKeys)
                {
                    dictA.TryGetValue(key, out object valA);
                    dictB.TryGetValue(key, out object valB);
                    if (HasChanges(valA, valB)) return true;
                }
                return false;
            }

            if (a is List<object> listA && b is List<object> listB)
            {
                int maxCount = Mathf.Max(listA.Count, listB.Count);
                for (int i = 0; i < maxCount; i++)
                {
                    object itemA = i < listA.Count ? listA[i] : null;
                    object itemB = i < listB.Count ? listB[i] : null;
                    if (HasChanges(itemA, itemB)) return true;
                }
                return false;
            }

            return !a.Equals(b);
        }

        private static bool IsSimpleList(List<object> list)
        {
            if (list == null) return true;
            foreach (var item in list)
            {
                if (item is Dictionary<string, object> || item is List<object>) return false;
            }
            return true;
        }

        private static string GetSimpleListString(List<object> list)
        {
            if (list == null) return "null";
            if (list.Count == 0) return "[]";
            var stringElements = new List<string>();
            // 단순 리스트 요소를 출력할 때도 FormatValue를 타도록 수정
            foreach (var item in list) stringElements.Add(FormatValue(item));
            return string.Join(", ", stringElements);
        }

        /// <summary>
        /// 값이 string 타입일 경우 앞뒤로 작은따옴표를 붙여 가독성을 높입니다.
        /// </summary>
        private static string FormatValue(object obj)
        {
            if (obj == null) return "null";
            if (obj is string str) return $"'{str}'";
            return obj.ToString();
        }

        // =========================================================
        // [2] 텍스트 로그 렌더링 (GUI) 파트
        // =========================================================

        public static void DrawTextLog(string diffText)
        {
            if (string.IsNullOrWhiteSpace(diffText)) return;

            if (diffText == "변경된 데이터가 없습니다.")
            {
                EditorGUILayout.HelpBox(diffText, MessageType.Info);
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "box", GUILayout.ExpandHeight(true));
            
            string[] lines = diffText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                Color color = Color.white;

                if (line.StartsWith("[ADD]")) color = Color.green;
                else if (line.StartsWith("[REMOVE]")) color = new Color(1f, 0.4f, 0.4f);
                else if (line.StartsWith("[INSERT]") || line.StartsWith("[TYPEChange]")) color = Color.yellow;

                DrawColorLine(line, color);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private static void DrawColorLine(string text, Color color)
        {
            var prevColor = GUI.contentColor;
            GUI.contentColor = color;
            EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);
            GUI.contentColor = prevColor;
        }
    }
}