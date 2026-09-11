using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Localize.Elements;

namespace Gsheets.Localize
{
    /// <summary>
    /// 자동으로 생성된 Gsheet의 로컬라이징 데이터를 찾아 로컬라이징 데이터로 활용할수있게 합니다.
    /// </summary>
    public static class LocalizeSheetBinder
    {
        private static Dictionary<LangCode, PropertyInfo> _langPropertyAccessor;
        private static object _gsheetInstance;
        private static PropertyInfo _gsheetLocalizeDictionaryField;

        public static void Initialize()
        {
            _gsheetInstance = null;
        }
        
        private static bool CheckAndInitialize()
        {
            if (_gsheetInstance == null)
            {
                _langPropertyAccessor = new();
                var gsheetData = GSheetSettingScriptable.Instance;
                if (string.IsNullOrEmpty(gsheetData.LocalizeSetting.SheetName))
                {
                    _gsheetInstance = null;
                }
                else
                {
                    _gsheetInstance = gsheetData.FindGSheetInstance();
                    if (_gsheetInstance == null)
                        return false;
                    _gsheetLocalizeDictionaryField = gsheetData.GSheetType.GetProperty(gsheetData.LocalizeSetting.SheetName);
                    var localizeType = Type.GetType($"{gsheetData.GeneratorNameSpace}.{gsheetData.LocalizeSetting.SheetName}, {GSheetSettingScriptable.GeneratorAssemblyName}");
                    var allProperties = localizeType.GetProperties();
                    EnumCache<LangCode> langCodeCache = new EnumCache<LangCode>();
                    foreach (var property in allProperties)
                    {
                        if (langCodeCache.HasEnum(property.Name))
                            _langPropertyAccessor.Add(langCodeCache.ToEnum(property.Name), property);
                    }
                }
            }
            return _gsheetInstance != null && _gsheetLocalizeDictionaryField != null;
        }
        
        public static string GetLocalizeString(LangCode lang, string key)
        {
            string result = key;
            if (CheckAndInitialize())
            {
                var dic = (System.Collections.IDictionary)_gsheetLocalizeDictionaryField.GetValue(_gsheetInstance);
                if (dic!=null && dic.Contains(key))
                    result = (string)_langPropertyAccessor[lang].GetValue(dic[key]);
            }
            return result;
        }
        
        public static string GetLocalizeString(string key)
        {
            if (CheckAndInitialize())
            {
                var lanCode = _langPropertyAccessor.Count > 0 ? _langPropertyAccessor.First().Key : 0;
                return GetLocalizeString(lanCode, key);
            }
            return key;
        }
    }
}