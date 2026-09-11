using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace Gsheets
{
    public static class TypeFinder
    {
        private static List<Assembly> _assemblies = null;
        private static Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>()
        {
            {"string", typeof(string)},
            {"string[]", typeof(string[])},
            {"byte", typeof(byte)},
            {"byte[]", typeof(byte[])},
            {"short", typeof(short)},
            {"short[]", typeof(short[])},
            {"int", typeof(int)},
            {"int[]", typeof(int[])},
            {"float", typeof(float)},
            {"float[]", typeof(float[])},
            {"double", typeof(double)},
            {"double[]", typeof(double[])},
            {"long", typeof(long)},
            {"long[]", typeof(long[])},
            {"decimal", typeof(decimal)},
            {"decimal[]", typeof(decimal[])},
            {"char", typeof(char)},
            {"char[]", typeof(char[])},
            {"bool", typeof(bool)},
            {"bool[]", typeof(bool[])},
            
            {"Vector2", typeof(Vector2)},
            {"Vector2[]", typeof(Vector2[])},
            {"Vector2Int", typeof(Vector2Int)},
            {"Vector2Int[]", typeof(Vector2Int[])},
            {"Vector3", typeof(Vector3)},
            {"Vector3[]", typeof(Vector3[])},
            {"Vector3Int", typeof(Vector3Int)},
            {"Vector3Int[]", typeof(Vector3Int[])},
            
            {"Quaternion", typeof(Quaternion)},
            {"Quaternion[]", typeof(Quaternion[])},
            {"NativeArray`1", typeof(NativeArray<>)},
            {"Color", typeof(Color)},
        };
        
        private static void RefreshAssemblies()
        {
            _assemblies = new();
            //Find Unity Type ->  asd 
            //6.8 이후 CoreCLR 사용시 UnityEngine.Assemblies.CurrentAssemblies로 대체
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic)
                    continue;
                //var fullname = assembly.FullName;
                //var location = assembly.Location;
                var targetStr = assembly.Location;
                bool isCustomLibrary = targetStr.Contains("Library") && !(targetStr.Contains("Unity") || targetStr.Contains("unity"));
                bool isTarget = targetStr.Contains("Assets") ||
                                isCustomLibrary ||
                                targetStr.Contains("Assembly-CSharp") ||
                                targetStr.Contains("Unity.Collections") ||
                                targetStr.Contains($"{nameof(Gsheets)}") ||
                                targetStr.Contains($"{nameof(Gsheets)}.Editor");
                if (isTarget)
                    _assemblies.Add(assembly);
            }
            
        }
        
        public static Type Find(string typeName)
        {
            if(_assemblies == null)
                RefreshAssemblies();
            if(_cachedTypes.TryGetValue(typeName, out Type result))
                return result;
            bool isArray = false;
            if (typeName.Contains("[]"))
            {
                isArray = true;
                typeName = typeName.Replace("[]","");
            }
            
            foreach (var ass in _assemblies)
            {
                var export = ass.ExportedTypes;
                foreach (var type in export)
                {
                    if (type.Name == typeName)
                    {
                        var finType = type;
                        if (isArray)
                        {
                            finType = ass.GetType($"{type.FullName}[]");
                            typeName = typeName + "[]";
                        }
                        _cachedTypes.Add(typeName, finType);
                        return finType;
                    }
                }
            }
            return result;
        }

        public static Type[] GetAssignableFroms<T>()
        {
            var baseType = typeof(T);
            if(_assemblies == null)
                RefreshAssemblies();
            List<Type> result = new List<Type>();
            foreach (var ass in _assemblies)
            {
                var export = ass.ExportedTypes;
                foreach (var type in export)
                {
                    if(type.IsInterface || type.IsAbstract)
                        continue;
                    if (baseType != type && baseType.IsAssignableFrom(type))
                        result.Add(type);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// T 에 해당하는 어트리뷰트를 선언한 타입을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public static Type[] GetAssignableFromAttributes<T>()
        {
            var baseType = typeof(T);
            if(_assemblies == null)
                RefreshAssemblies();
            List<Type> result = new List<Type>();
            foreach (var ass in _assemblies)
            {
                var export = ass.ExportedTypes;
                foreach (var type in export)
                    if (type.GetCustomAttributes(typeof(T), false).Length > 0)
                        result.Add(type);
            }

            return result.ToArray();
        }
        
        

        public static (string containerType, string genericType) GetGenericType(string typeString)
        {
            int end = typeString.IndexOf(">", StringComparison.Ordinal);
            int start = typeString.IndexOf("<", StringComparison.Ordinal);
            string targetType = typeString.Substring(start + 1, (end - start)-1);
            var containerType = typeString.Substring(0, start);
            return  (containerType, targetType);
        }
        
        public static bool IsUnmanaged(Type type)
        {
            if (type.IsPrimitive || type.IsEnum) return true;
            if (!type.IsValueType) return false;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!IsUnmanaged(field.FieldType))
                    return false;
            }
            return true;
        }
    }
}