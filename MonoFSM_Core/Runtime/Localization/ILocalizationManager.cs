namespace RCGMakerFSMCore.Runtime.Localization
{
    public interface ILocalizationManager
    {
        string GetTranslation(string termKey, bool rtlFix = true, int maxLineLength = 0, bool convertNumbers = true);
        string ApplyLocalizationParams(string text);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
    }
}