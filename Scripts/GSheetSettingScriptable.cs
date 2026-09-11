using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gsheets.IO;
using Gsheets.Localize;
using Localize.Elements;
using TextMateSharp.Grammars;
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
      
        private static GSheetSettingScriptable _instance = null;
        public string SheetID => _sheetID;
        public string GeneratorNameSpace => _codeGenerationPath.Replace("Scripts/", "").Replace("/", ".");//_generatorNameSpace;
        public string CodeGenerationPath => _codeGenerationPath;
        public List<GSheetInfo> SheetInfos => _sheetInfos;
        public LocalizeSetting LocalizeSetting => _localizeSetting;
        public Action GsheetReLoadFunc { get; set; }
        public Type GSheetType => Type.GetType($"{GeneratorNameSpace}.Gsheet, {GeneratorAssemblyName}");
        

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
            if (GSheetType == null)
                return null;
            return GSheetType.GetProperty("Instance")?.GetValue(null);
        }
        
        public virtual void OnBeginGenerator()
        {
            
        }

        public virtual void OnEndGenerator()
        {
            LocalizeSheetBinder.Initialize();
            GsheetReLoadFunc?.Invoke();
            LocalizeManager.Instance.RefreshListener(RefreshMode.All);
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