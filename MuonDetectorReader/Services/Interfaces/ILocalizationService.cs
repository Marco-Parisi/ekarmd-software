using System;

namespace MuonDetectorReader.Services.Interfaces
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        void Initialize();
        void ChangeLanguage(string languageCode);
        string GetString(string key);
        event EventHandler LanguageChanged;
    }
}
