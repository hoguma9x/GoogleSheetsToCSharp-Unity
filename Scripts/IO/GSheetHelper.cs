using System;
using System.Collections;
using System.Collections.Generic;
using LWSerializer;
using UnityEngine;

namespace Gsheets.IO
{
    public static class GSheetHelper
    {
        //WriteDirect 참조
        public static object ReadSheet(GSheetBinaryReader reader)
        {
            List<object> list = new();
            Dictionary<string, ILwSerializable> dic = new();
            reader.Read(out GSheetInfo info);
            for (int j = 0; j < info.DataCount; j++)
            {
                var type = info.GetSheetType();
                var instance = Activator.CreateInstance(info.GetSheetType());
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
                    Debug.LogError("SheetType 에 해당하는 형식은 ILwSerializable가 구현되어야합니다");
                }
            }
            reader.ReadPadding(32);
            return info.IsDictionary ? dic : list.ToArray();
        }
        
        public static T ReadSheet<T>(GSheetBinaryReader reader)
        {
            var sheetResult = ReadSheet(reader);
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
    }
}
