using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LWSerializer;

namespace Gsheets.IO
{
    public static class GSheetHelper
    {
        //WriteDirect 참조
        public static object ReadSheet(GSheetBinaryReader reader)
        {
            return ReadSheet(reader, TypeFinder.Find);
        }

        public static object ReadSheet(GSheetBinaryReader reader, Func<string, Type> resolveType)
        {
            List<object> list = new();
            Dictionary<string, ILwSerializable> dic = new();
            reader.Read(out GSheetInfo info);
            if (info.DataCount < 0) throw new InvalidDataException("Negative GSheet row count.");
            var type = resolveType(info.SheetName);
            if (type == null || !typeof(ILwSerializable).IsAssignableFrom(type))
                throw new InvalidDataException($"Sheet '{info.SheetName}' requires a registered ILwSerializable type.");
            for (int j = 0; j < info.DataCount; j++)
            {
                var instance = Activator.CreateInstance(type);
                if (instance is ILwSerializable serializable)
                {
                    if (info.IsDictionary)
                    {
                        reader.Read(out string key);
                        serializable.OnNativeRead(reader);
                        dic.Add(key, serializable);
                    }
                    else
                    {
                        serializable.OnNativeRead(reader);
                        list.Add(serializable);
                    }
                }
                else
                {
                    throw new InvalidDataException($"Sheet '{info.SheetName}' does not implement ILwSerializable.");
                }
            }
            reader.ReadPadding(32);
            return info.IsDictionary ? dic : list.ToArray();
        }
        
        public static T ReadSheet<T>(GSheetBinaryReader reader)
        {
            return ReadSheet<T>(reader, TypeFinder.Find);
        }

        public static T ReadSheet<T>(GSheetBinaryReader reader, Func<string, Type> resolveType)
        {
            var sheetResult = ReadSheet(reader, resolveType);
            var targetType = typeof(T);
            
            if (typeof(IList).IsAssignableFrom(targetType))
            {
                IList result = (IList)Activator.CreateInstance<T>();
                foreach (var itm in (IList)sheetResult)
                    result.Add(itm);
                return (T)result;
            }
            else if (typeof(IDictionary).IsAssignableFrom(targetType))
            {
                IDictionary result = (IDictionary)Activator.CreateInstance<T>();
                foreach (DictionaryEntry key in (IDictionary)sheetResult)
                    result.Add(key.Key, key.Value);
                return (T)result;
            }
            else if (targetType.IsArray)
            {
                return (T)sheetResult;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        /// <summary> 버킷 혹은 쉼표로 구분된 값을 분리해서 반환합니다 </summary>
        public static List<string> Convert(string content)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(content))
                return result;
            int depth = 0;
            int startIndex = 0;
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    // 괄호 밖(depth == 0)에서 쉼표를 만났을 때만 분리
                    string segment = content.Substring(startIndex, i - startIndex);
                    result.Add(segment.Trim());
                    startIndex = i + 1; // 다음 시작 위치 갱신
                }
            }
            if (startIndex < content.Length)
            {
                result.Add(content.Substring(startIndex).Trim());
            }
            else if (content.EndsWith(","))
            {
                result.Add("");
            }
            return result;
        }

        public static string RemoveBucket(string str)
        {
            if (str.First() == '{')
                str = str.Remove(0, 1);
            if(str.Last() == '}')
                str = str.Remove(str.Length - 1, 1);
            return str;
        }
    }
}
