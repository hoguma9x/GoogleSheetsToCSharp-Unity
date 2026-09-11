using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Gsheets.Editor.DiffView
{
    public class GsheetDiffHelper
    {
        /// <summary>
        /// 임의의 객체를 Diff 뷰어가 읽을 수 있는 메모리 트리 구조로 깊은 복사(Deep Copy)합니다.
        /// </summary>
        public static object Capture(object obj)
        {
            if (obj == null) return null;

            Type type = obj.GetType();

            // 1. 기본 원시 타입 및 문자열, 열거형은 그대로 반환
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                return obj;
            }

            
                
            // 2. Dictionary 구조 처리 (IDictionary 인터페이스 활용)
            if (obj is IDictionary dictObj)
            {
                var dict = new Dictionary<string, object>();
                foreach (DictionaryEntry kvp in dictObj)
                {
                    // 키는 string으로 변환하여 사용
                    dict[kvp.Key.ToString()] = Capture(kvp.Value);
                }
                return dict;
            }

            // 3. List 및 Array 구조 처리 (IEnumerable 인터페이스 활용)
            if (obj is IEnumerable enumerableObj)
            {
                var list = new List<object>();
                foreach (var item in enumerableObj)
                {
                    list.Add(Capture(item));
                }
                return list;
            }

            // 4. 일반 클래스/구조체 처리 (ExampleClass, ExampleStruct 등)
            var result = new Dictionary<string, object>();
        
            // 생성된 클래스의 Public 인스턴스 필드만 추출
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                //Dispose 구현된 객체는 생략
                if (typeof(IDisposable).IsAssignableFrom(field.FieldType))
                    continue;
                //FixedString(Native) 대응
                if (field.FieldType.Name.Contains("FixedString"))
                {
                    result[field.Name] = field.GetValue(obj).ToString();
                    continue;
                }
                if(field.FieldType.Name.Contains("Color"))
                {
                    result[field.Name] = field.GetValue(obj).ToString();
                    continue;
                } 
                // _instance 같은 private 필드는 자연스럽게 무시됨
                result[field.Name] = Capture(field.GetValue(obj));
            }

            return result;
        }
    }
}
