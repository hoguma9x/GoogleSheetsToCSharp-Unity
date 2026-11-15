using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Gsheets.Editor.DownLoader;
using Gsheets.Parsing;
using UnityEngine;

namespace Gsheets.Editor.Generator
{
    /// <summary>
    /// 바이너리와 같은 시트 데이터를 타 플랫폼/백엔드에서 읽을 수 있는 JSON으로 내보냅니다.
    /// 셀 값은 바이너리 기록과 동일한 ParserFormatter로 해석한 뒤 JSON 값으로 변환합니다.
    /// </summary>
    /// <remarks>
    /// 출력 형식
    /// {
    ///   "version": 1,
    ///   "generatedAt": "2026-11-15T12:00:00Z",
    ///   "sheets": {
    ///     "SheetName": {
    ///       "kind": "class" | "struct" | "external",
    ///       "collection": "dictionary" | "list",
    ///       "fields": [ { "name": "hp", "type": "int" }, ... ],
    ///       "data": { "KEY": { "hp": 10, ... } }   // list 시트는 [ { ... }, ... ]
    ///     }
    ///   }
    /// }
    /// enum은 이름 문자열, Vector는 {x,y[,z]}, Color는 {r,g,b,a}(0~1), 배열/NativeArray는 JSON 배열,
    /// FixedString은 문자열, 그 외 사용자 타입은 public 필드 객체로 기록됩니다.
    /// </remarks>
    public static class GsheetJsonExporter
    {
        public const int FormatVersion = 1;

        public static string Build(IReadOnlyList<SheetRawData> sheetDatas)
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\n");
            sb.Append("  \"version\": ").Append(FormatVersion).Append(",\n");
            sb.Append("  \"generatedAt\": ");
            WriteString(sb, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            sb.Append(",\n");
            sb.Append("  \"sheets\": {");
            for (int s = 0; s < sheetDatas.Count; s++)
            {
                sb.Append(s == 0 ? "\n" : ",\n");
                WriteSheet(sb, sheetDatas[s]);
            }
            sb.Append(sheetDatas.Count > 0 ? "\n  }\n" : "}\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        static void WriteSheet(StringBuilder sb, SheetRawData sheet)
        {
            var headers = sheet.Headers;
            var rows = sheet.Rows;
            bool isDictionary = sheet.IsDictionary();

            sb.Append("    ");
            WriteString(sb, sheet.SheetName);
            sb.Append(": {\n");

            sb.Append("      \"kind\": ");
            WriteString(sb, sheet.TypeKeyword);
            sb.Append(",\n");

            sb.Append("      \"collection\": ");
            WriteString(sb, isDictionary ? "dictionary" : "list");
            sb.Append(",\n");

            sb.Append("      \"fields\": [");
            for (int j = 1; j < headers.Count; j++)
            {
                sb.Append(j == 1 ? "\n" : ",\n");
                sb.Append("        { \"name\": ");
                WriteString(sb, headers[j].memberName);
                sb.Append(", \"type\": ");
                WriteString(sb, headers[j].typeString);
                sb.Append(" }");
            }
            sb.Append(headers.Count > 1 ? "\n      ],\n" : "],\n");

            sb.Append("      \"data\": ").Append(isDictionary ? '{' : '[');
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                sb.Append(i == 1 ? "\n" : ",\n");
                sb.Append("        ");
                if (isDictionary)
                {
                    WriteString(sb, row[0] ?? "");
                    sb.Append(": ");
                }
                sb.Append('{');
                for (int j = 1; j < row.Length; j++)
                {
                    if (j > 1) sb.Append(", ");
                    WriteString(sb, headers[j].memberName);
                    sb.Append(": ");
                    WriteCell(sb, sheet.SheetName, row[0], headers[j], row[j]);
                }
                sb.Append('}');
            }
            if (rows.Count > 1) sb.Append("\n      ");
            sb.Append(isDictionary ? '}' : ']').Append('\n');
            sb.Append("    }");
        }

        static void WriteCell(StringBuilder sb, string sheetName, string key, HeaderType header, string content)
        {
            object value;
            try
            {
                value = ParserFormatter.Get(header.type).ToData(content ?? "");
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"[Json] {sheetName}/{key}/{header.memberName} ('{content}') parse failed: {e.Message}", e);
            }

            try
            {
                WriteValue(sb, value, 0);
            }
            finally
            {
                // NativeArray 등 ToData가 Allocator.Temp로 생성한 객체 정리
                if (value is IDisposable disposable && value.GetType().IsValueType)
                {
                    try { disposable.Dispose(); }
                    catch (Exception) { /* 이미 해제됐거나 Temp 범위 밖인 경우 무시 */ }
                }
            }
        }

        const int MaxDepth = 32;

        static void WriteValue(StringBuilder sb, object value, int depth)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }
            if (depth > MaxDepth)
                throw new Exception($"[Json] max depth exceeded ({value.GetType().Name})");

            switch (value)
            {
                case string str: WriteString(sb, str); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case char c: WriteString(sb, c.ToString()); return;
                case float f: WriteFloat(sb, f, f.ToString("R", CultureInfo.InvariantCulture)); return;
                case double d: WriteFloat(sb, d, d.ToString("R", CultureInfo.InvariantCulture)); return;
                case decimal m: sb.Append(m.ToString(CultureInfo.InvariantCulture)); return;
                case Enum e: WriteString(sb, e.ToString()); return;
                case Vector2 v2:
                    sb.Append("{\"x\": "); WriteValue(sb, v2.x, depth + 1);
                    sb.Append(", \"y\": "); WriteValue(sb, v2.y, depth + 1);
                    sb.Append('}');
                    return;
                case Vector3 v3:
                    sb.Append("{\"x\": "); WriteValue(sb, v3.x, depth + 1);
                    sb.Append(", \"y\": "); WriteValue(sb, v3.y, depth + 1);
                    sb.Append(", \"z\": "); WriteValue(sb, v3.z, depth + 1);
                    sb.Append('}');
                    return;
                case Vector2Int v2i:
                    sb.Append("{\"x\": ").Append(v2i.x.ToString(CultureInfo.InvariantCulture));
                    sb.Append(", \"y\": ").Append(v2i.y.ToString(CultureInfo.InvariantCulture));
                    sb.Append('}');
                    return;
                case Vector3Int v3i:
                    sb.Append("{\"x\": ").Append(v3i.x.ToString(CultureInfo.InvariantCulture));
                    sb.Append(", \"y\": ").Append(v3i.y.ToString(CultureInfo.InvariantCulture));
                    sb.Append(", \"z\": ").Append(v3i.z.ToString(CultureInfo.InvariantCulture));
                    sb.Append('}');
                    return;
                case Color col:
                    sb.Append("{\"r\": "); WriteValue(sb, col.r, depth + 1);
                    sb.Append(", \"g\": "); WriteValue(sb, col.g, depth + 1);
                    sb.Append(", \"b\": "); WriteValue(sb, col.b, depth + 1);
                    sb.Append(", \"a\": "); WriteValue(sb, col.a, depth + 1);
                    sb.Append('}');
                    return;
            }

            var type = value.GetType();
            if (type.IsPrimitive) // byte, sbyte, short, ushort, int, uint, long, ulong
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            // FixedStringNNBytes는 IEnumerable<Rune>을 구현하므로 컬렉션보다 먼저 처리
            if (type.Name.StartsWith("FixedString", StringComparison.Ordinal))
            {
                WriteString(sb, value.ToString());
                return;
            }

            if (value is IDictionary dict)
            {
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry kv in dict)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    WriteString(sb, Convert.ToString(kv.Key, CultureInfo.InvariantCulture));
                    sb.Append(": ");
                    WriteValue(sb, kv.Value, depth + 1);
                }
                sb.Append('}');
                return;
            }

            if (value is IEnumerable enumerable)
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in enumerable)
                {
                    if (!first) sb.Append(", ");
                    first = false;
                    WriteValue(sb, item, depth + 1);
                }
                sb.Append(']');
                return;
            }

            // 사용자 정의 타입: public 인스턴스 필드를 객체로 기록
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            if (fields.Length == 0)
            {
                WriteString(sb, value.ToString());
                return;
            }
            sb.Append('{');
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                WriteString(sb, fields[i].Name);
                sb.Append(": ");
                WriteValue(sb, fields[i].GetValue(value), depth + 1);
            }
            sb.Append('}');
        }

        static void WriteFloat(StringBuilder sb, double value, string text)
        {
            // JSON은 NaN/Infinity를 표현할 수 없음
            if (double.IsNaN(value) || double.IsInfinity(value))
                sb.Append("null");
            else
                sb.Append(text);
        }

        static void WriteString(StringBuilder sb, string str)
        {
            if (str == null)
            {
                sb.Append("null");
                return;
            }
            sb.Append('"');
            foreach (var c in str)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
