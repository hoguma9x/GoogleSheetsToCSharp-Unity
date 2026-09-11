using System;
using Gsheets.Localize;
using UnityEngine;

namespace Localize.Elements
{
    [Serializable]
    public class LocalizeString
    {
        [SerializeField] private string _localizeKey;

        public string LocalizeKey
        {
            get => _localizeKey;
            set => _localizeKey = value;
        }

        public LocalizeString()
        {
            _localizeKey = "";
        }
        
        public LocalizeString(string immediateString)
        {
            _localizeKey = immediateString;
        }
        
        public string GetString()
        {
            return Trans_str(_localizeKey);
        }
        
        #region static
        public static string Trans_str(string key)
        {
            return LocalizeManager.Instance.Localize(key);
        }
        public static LocalizeString Trans_ref(string key)
        {
            return new LocalizeString(key);
        }
        #endregion
    }
}