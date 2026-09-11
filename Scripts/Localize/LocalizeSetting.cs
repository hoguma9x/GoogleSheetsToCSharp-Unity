using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Gsheets.Localize
{
    [Serializable]
    public class LocalizeSetting
    {
        [SerializeField] private string _sheetName = "Localize";
        [SerializeField, HideInInspector] private string[] _languageCodes = new string[0];
        [SerializeField] private List<LocalizeFontSet> _fontSets = new();
        
        public string SheetName => _sheetName;
        public string[] LanguageCodes { get => _languageCodes; set => _languageCodes = value; }
        public List<LocalizeFontSet> FontSets => _fontSets;
    }
    
    [Serializable]
    public struct LocalizeFontSet
    {
        public TMP_FontAsset Font;
        public LangCode Mode;
    }
}
