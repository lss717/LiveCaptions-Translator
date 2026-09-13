using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace LiveCaptionsTranslator.i18n
{
    public sealed class LocalizationService
    {
        private const string DefaultCulture = "en-US";
        private static readonly string[] SupportedCultures = { "en-US", "zh-CN" };

        private ResourceDictionary? currentDictionary;

        public static LocalizationService Instance { get; } = new();

        public event Action? LanguageChanged;

        public string CurrentCulture { get; private set; } = DefaultCulture;

        private LocalizationService() { }

        public void Initialize(string? code)
        {
            Apply(code, raiseEvent: false);
        }

        public void Apply(string? code)
        {
            Apply(code, raiseEvent: true);
        }

        private void Apply(string? code, bool raiseEvent)
        {
            string resolved = Resolve(code);
            CurrentCulture = resolved;

            var culture = new CultureInfo(resolved);
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;

            var dictionary = new ResourceDictionary
            {
                Source = new Uri(
                    $"pack://application:,,,/LiveCaptionsTranslator;component/src/i18n/Strings.{resolved}.xaml",
                    UriKind.Absolute)
            };

            var merged = Application.Current.Resources.MergedDictionaries;
            if (currentDictionary != null)
                merged.Remove(currentDictionary);
            merged.Add(dictionary);
            currentDictionary = dictionary;

            if (raiseEvent)
                LanguageChanged?.Invoke();
        }

        private static string Resolve(string? code)
        {
            if (!string.IsNullOrWhiteSpace(code))
                return SupportedCultures.Contains(code) ? code : DefaultCulture;

            var system = CultureInfo.CurrentUICulture;
            string name = system.Name;
            if (SupportedCultures.Contains(name))
                return name;

            string twoLetter = system.TwoLetterISOLanguageName;
            string? match = SupportedCultures.FirstOrDefault(s =>
                string.Equals(new CultureInfo(s).TwoLetterISOLanguageName, twoLetter,
                    StringComparison.OrdinalIgnoreCase));
            return match ?? DefaultCulture;
        }

        public string T(string key)
        {
            if (Application.Current?.TryFindResource(key) is string value)
                return value;
            return key;
        }

        public string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
