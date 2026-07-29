using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using MuonDetectorReader.Services.Interfaces;

namespace MuonDetectorReader.Services
{
    public class LocalizationService : ILocalizationService
    {
        private static ILocalizationService _current;
        public static ILocalizationService Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new LocalizationService(new SettingsService());
                }
                return _current;
            }
        }

        private readonly ISettingsService _settingsService;
        private string _currentLanguage;

        public string CurrentLanguage => _currentLanguage;
        public event EventHandler LanguageChanged;

        public LocalizationService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? new SettingsService();
        }

        public void Initialize()
        {
            var settings = _settingsService.Load();
            string lang = settings.Language;

            if (string.IsNullOrWhiteSpace(lang))
            {
                string sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                lang = (sysLang == "it") ? "it" : "en";
            }
            else
            {
                lang = lang.ToLower().Trim();
                if (lang != "it" && lang != "en")
                {
                    lang = "en";
                }
            }

            ApplyLanguage(lang, false);
        }

        public void ChangeLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return;
            string lang = languageCode.ToLower().Trim();
            if (lang != "it" && lang != "en") lang = "en";

            ApplyLanguage(lang, true);
        }

        private void ApplyLanguage(string lang, bool saveToSettings)
        {
            _currentLanguage = lang;

            string dictUri = $"/UI/Localization/Strings.{lang}.xaml";

            if (Application.Current != null && Application.Current.Resources != null)
            {
                var existingDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/UI/Localization/Strings."));

                var newDict = new ResourceDictionary
                {
                    Source = new Uri(dictUri, UriKind.RelativeOrAbsolute)
                };

                if (existingDict != null)
                {
                    int index = Application.Current.Resources.MergedDictionaries.IndexOf(existingDict);
                    Application.Current.Resources.MergedDictionaries[index] = newDict;
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(newDict);
                }
            }

            if (saveToSettings)
            {
                var settings = _settingsService.Load();
                settings.Language = lang;
                _settingsService.Save(settings);
            }

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key) || Application.Current == null) 
                return key;

            try
            {
                var res = Application.Current.TryFindResource(key);
                if (res is string str)
                {
                    return str;
                }
            }
            catch
            {
                // Ignore errors
            }

            return key;
        }
    }
}
