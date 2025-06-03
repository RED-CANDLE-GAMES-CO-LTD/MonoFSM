using System;
using UnityEngine;

namespace MonoFSM.Localization
{
    //介面怎麼呈現？先照著i2的規格？和i2的bridge還要放在package外
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct LocalizedString
    {
        [SerializeField] private string termKey;

        // [SerializeField] private bool ignoreRTLFix;
        // [SerializeField] private int maxRTLLineLength;
        // [SerializeField] private bool convertRTLNumbers;
        // [SerializeField] private bool dontLocalizeParameters;
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
            // ignoreRTLFix = false;
            // maxRTLLineLength = 0;
            // convertRTLNumbers = true;
            // dontLocalizeParameters = false;
        }

        public LocalizedString(LocalizedString other)
        {
            termKey = other.termKey;
            // ignoreRTLFix = other.ignoreRTLFix;
            // maxRTLLineLength = other.maxRTLLineLength;
            // convertRTLNumbers = other.convertRTLNumbers;
            // dontLocalizeParameters = other.dontLocalizeParameters;
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

            // var translation = _localizationManager.GetTranslation(
            //     termKey,
            //     !ignoreRTLFix,
            //     maxRTLLineLength,
            //     !convertRTLNumbers
            // );
            var translation = _localizationManager.GetTranslation(
                termKey
                // !ignoreRTLFix,
                // maxRTLLineLength,
                // !convertRTLNumbers
            );


            if (string.IsNullOrEmpty(translation))
                return fallbackText;

            if (translation.Contains("$blank")) //刻意留空
                return "";

            // if (!dontLocalizeParameters)
            //     translation = _localizationManager.ApplyLocalizationParams(translation);


            return translation;
        }
    }
}