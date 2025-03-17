using System;
using UnityEngine;
namespace RCGMakerFSMCore.Runtime.Localization
{

    public interface ILocalizationManager
    {
        string GetTranslation(string termKey, bool rtlFix = true, int maxLineLength = 0, bool convertNumbers = true);
        string ApplyLocalizationParams(string text);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
    }
    [Serializable]
    public struct LocalizedString
    {
        [SerializeField] private string termKey;
        [SerializeField] private bool ignoreRTLFix;
        [SerializeField] private int maxRTLLineLength;
        [SerializeField] private bool convertRTLNumbers;
        [SerializeField] private bool dontLocalizeParameters;
        [SerializeField] private string fallbackText;
        
        // Static accessor for the localization manager (set via DI)
        private static ILocalizationManager _localizationManager;
        
        //給LocalizationManager設定 ex: I2.Loc.LocalizationManager
        public static ILocalizationManager LocalizationManager
        {
            get => _localizationManager;
            set => _localizationManager = value; 
        }

        public string TermKey => termKey;
        public string FallbackText => fallbackText;

        public static implicit operator string(LocalizedString s)
        {
            return s.ToString();
        }

        public static implicit operator LocalizedString(string term)
        {
            return new LocalizedString { termKey = term };
        }

        public LocalizedString(string key, string fallback = "")
        {
            termKey = key;
            fallbackText = fallback;
            ignoreRTLFix = false;
            maxRTLLineLength = 0;
            convertRTLNumbers = true;
            dontLocalizeParameters = false;
        }

        public LocalizedString(LocalizedString other)
        {
            termKey = other.termKey;
            ignoreRTLFix = other.ignoreRTLFix;
            maxRTLLineLength = other.maxRTLLineLength;
            convertRTLNumbers = other.convertRTLNumbers;
            dontLocalizeParameters = other.dontLocalizeParameters;
            fallbackText = other.fallbackText;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(termKey) || termKey == "-")
                return fallbackText;
                
            if (_localizationManager == null)
            {
                Debug.LogWarning("LocalizationManager not set. Returning fallback text.");
                return fallbackText;
            }

            string translation = _localizationManager.GetTranslation(
                termKey,
                rtlFix: !ignoreRTLFix,
                maxLineLength: maxRTLLineLength,
                convertNumbers: !convertRTLNumbers
            );
            
            if (string.IsNullOrEmpty(translation))
                return fallbackText;
                
            if (!dontLocalizeParameters)
                translation = _localizationManager.ApplyLocalizationParams(translation);
                
            if (translation.Contains("$blank"))
                return "";
                
            return translation;
        }
    }
}