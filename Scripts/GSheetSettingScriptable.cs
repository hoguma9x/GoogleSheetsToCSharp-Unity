using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gsheets.IO;
using Gsheets.Localize;
using Localize.Elements;
using UnityEngine;

namespace Gsheets
{
    /// <summary>
    /// 설정파일이 누락될경우 다음 컴파일 시기에 SheetDataSettingScriptableEditor 에 의해 자동생성됩니다.
    /// </summary>
    public class GSheetSettingScriptable : ScriptableObject
    {
        public const string GeneratorAssemblyName = "Assembly-CSharp";
        public const string FileName = "GsheetSetting";
        public const string BinaryFileName = "GsheetData";
        public const string AnnotationText = "//";
        private readonly Regex SheetIdRegex = new Regex(@"/d/([a-zA-Z0-9-_]+)", RegexOptions.Compiled);
        
        [SerializeField] private string _codeGenerationPath = "Scripts/Generator";
        [SerializeField] private string _sheetID = "1188AKPfAl2taqn6G-JDENJF-WeO_YA_gE4SRYzMRZBc";
        [SerializeField] private LocalizeSetting _localizeSetting;
        [Space(20), SerializeField] private List<GSheetInfo> _sheetInfos = new List<GSheetInfo>();
        
        [SerializeField, HideInInspector] private List<string> _assemblies = new List<string>();
        
        private static GSheetSettingScriptable _instance = null;
        public string SheetID => _sheetID;
        public string GeneratorNameSpace => _codeGenerationPath.Replace("Scripts/", "").Replace("/", ".");//_generatorNameSpace;
        public string CodeGenerationPath => _codeGenerationPath;
        public List<GSheetInfo> SheetInfos => _sheetInfos;
        public LocalizeSetting LocalizeSetting => _localizeSetting;
        public Action GsheetReLoadFunc { get; set; }
        public List<string> ReferencedAssemblies => _assemblies;
        

        public static GSheetSettingScriptable Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GSheetSettingScriptable>(FileName);
                }
                return _instance;
            }
        }
        
        public object FindGSheetInstance()
        {
            var type = GetGsheetType();
            if (type == null)
                return null;
            return type.GetProperty("Instance")?.GetValue(null);
        }

        public Type GetGsheetType()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType($"{GeneratorNameSpace}.Gsheet"))
                .FirstOrDefault(type => type != null);
        }

        public Type GetLocalizeType()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType($"{GeneratorNameSpace}.{LocalizeSetting.SheetName}"))
                .FirstOrDefault(type => type != null);
        }
        
        public virtual void OnBeginGenerator()
        {
            
        }

        public virtual void OnEndGenerator()
        {
#if UNITY_EDITOR
            Debug.Log("OnEndGenerator");
            var assemblies = new HashSet<string>();
            var sheetType = GetGsheetType();
            var asms = sheetType.Assembly.GetReferencedAssemblies();
            foreach (var assembly in asms)
                assemblies.Add(assembly.Name);
            var allFields= sheetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in allFields)
                if (field.ReflectedType?.Assembly != null)
                    assemblies.Add(field.ReflectedType?.Assembly.GetName(true).Name);
            _assemblies = assemblies.ToList();
            LocalizeSheetBinder.Initialize();
            GsheetReLoadFunc?.Invoke();
            LocalizeManager.Instance.RefreshListener(RefreshMode.All);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public virtual void OnInspectorGUI()
        {
            _sheetID = ExtractSheetId(_sheetID);
        }
        
        private string ExtractSheetId(string input)
        {
            input = input.Trim();
            Match match = SheetIdRegex.Match(input);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return input;
        }
    }
}