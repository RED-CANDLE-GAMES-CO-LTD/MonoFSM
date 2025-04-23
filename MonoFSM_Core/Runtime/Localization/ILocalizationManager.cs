namespace MonoFSM.Localization
{
    public interface ILocalizationManager
    {
        string GetTranslation(string termKey, bool rtlFix = true, int maxLineLength = 0, bool convertNumbers = true);
        string GetTranslation(string termKey);
        string ApplyLocalizationParams(string text);
        void SetLanguage(string languageCode);
        string CurrentLanguage { get; }
    }
}