using Gsheets.Localize;
using Localize.Elements;
using TMPro;
using UnityEngine;

namespace Gsheets.SheetData
{
    public class TextMeshProLocalizeUGUI : TextMeshProUGUI, ILocalizeListener
    {
        [SerializeField] private LocalizeString _localizeKey = new LocalizeString();
        
        public LocalizeString text_local
        {
            get
            {
                return _localizeKey;
            }
            set
            {
                _localizeKey = value;
                text = LocalizeManager.Instance.Localize(_localizeKey.LocalizeKey);
            }
        }
        public string text_localizeKey
        {
            get
            {
                return _localizeKey.LocalizeKey;
            }
            set
            {
                _localizeKey ??= new LocalizeString();
                _localizeKey.LocalizeKey = value;
                text = LocalizeManager.Instance.Localize(this, _localizeKey.LocalizeKey);
            }
        }
        
        public LocalizeString LocalizeKey
        {
            get => _localizeKey;
            set => _localizeKey = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            LocalizeManager.Instance.AddListener(this);
        }

        protected override void OnDisable()
        {
            LocalizeManager.Instance.RemoveListener(this);
            base.OnDisable();
        }

        public void RefreshLocalize()
        {
            if (LocalizeManager.Instance is { })
                RefreshLocalize(RefreshMode.All);
        }
        
        public void RefreshLocalize(RefreshMode mode)
        {
            if (_localizeKey == null)
                return;
            
            text = LocalizeManager.Instance.Localize(this, _localizeKey.LocalizeKey);
        }
    }
}