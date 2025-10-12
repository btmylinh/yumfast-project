using System.Globalization;
using System.Text.Json;

namespace WebApp.Services
{
    public interface IJsonLocalizationService
    {
        string GetLocalizedString(string key);
        string GetLocalizedString(string key, string culture);
    }

    public class JsonLocalizationService : IJsonLocalizationService
    {
        private readonly IWebHostEnvironment _env;
        private readonly Dictionary<string, Dictionary<string, string>> _localizations;

        public JsonLocalizationService(IWebHostEnvironment env)
        {
            _env = env;
            _localizations = new Dictionary<string, Dictionary<string, string>>();
            LoadLocalizations();
        }

        private void LoadLocalizations()
        {
            var localizationPath = Path.Combine(_env.ContentRootPath, "Resources", "Localization");
            
            if (!Directory.Exists(localizationPath))
                return;

            var jsonFiles = Directory.GetFiles(localizationPath, "*.json");
            
            foreach (var file in jsonFiles)
            {
                var culture = Path.GetFileNameWithoutExtension(file);
                var jsonContent = File.ReadAllText(file);
                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                
                if (translations != null)
                {
                    _localizations[culture] = translations;
                }
            }
        }

        public string GetLocalizedString(string key)
        {
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return GetLocalizedString(key, currentCulture);
        }

        public string GetLocalizedString(string key, string culture)
        {
            if (_localizations.TryGetValue(culture, out var translations) &&
                translations.TryGetValue(key, out var value))
            {
                return value;
            }

            // Fallback to English if key not found in current culture
            if (culture != "en" && _localizations.TryGetValue("en", out var englishTranslations) &&
                englishTranslations.TryGetValue(key, out var englishValue))
            {
                return englishValue;
            }

            // Return key if no translation found
            return key;
        }
    }
}
