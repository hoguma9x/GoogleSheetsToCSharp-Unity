using System.Collections.Generic;
using Gsheets.SheetData;
using Localize.Elements;
using TMPro;

namespace Gsheets.Localize
{
    public class LocalizeManager : NativeSingleton<LocalizeManager>
    {
        private Dictionary<LangCode, TMP_FontAsset> _fontDic;
        private HashSet<ILocalizeListener> _listeners;
        private LangCode _lang = LangCode.EN;
        public LangCode Language => _lang;

        protected override void OnCreateInstance()
        {
            base.OnCreateInstance();
            _listeners = new();
            _fontDic = new();
            foreach (var set in GSheetSettingScriptable.Instance.LocalizeSetting.FontSets)
                _fontDic.Add(set.Mode, set.Font);
        }

        public void AddListener(ILocalizeListener target)
        {
            _listeners.Add(target);
            target.RefreshLocalize(RefreshMode.All);
        }

        public void RemoveListener(ILocalizeListener target)
        {
            _listeners.Remove(target);
        }

        public void SetLanguage(LangCode mode)
        {
            bool dirty = _lang != mode;
            _lang = mode;
            if (dirty)
                RefreshListener(RefreshMode.Lang);
        }

        public string Localize(TextMeshProLocalizeUGUI tmpText, string localizeKey)
        {
            if (_fontDic.TryGetValue(_lang, out TMP_FontAsset font))
            {
                tmpText.font = font;
            }
            else
            {
                //Debug.LogError("Undefined TMP_FontAsset");
            }
           
            return LocalizeSheetBinder.GetLocalizeString(_lang, localizeKey);
        }
        
        public string Localize(string localizeKey)
        {
            return LocalizeSheetBinder.GetLocalizeString(_lang, localizeKey);
        }

        public void RefreshListener(RefreshMode mode)
        {
            foreach (var l in _listeners)
                l.RefreshLocalize(mode);
        }
    }
}