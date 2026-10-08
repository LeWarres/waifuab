using System.Collections.Generic;
using UnityEngine.Localization.Settings;

// Text lookups for the code-drawn HUD and menus. All text lives in the "Game" string table
// (source: Assets/Localization/strings.tsv). The language itself is the package's SelectedLocale.
public static class L10n
{
    const string Table = "Game";
    static readonly Dictionary<string, string> cache = new(); // one table lookup per key per language
    static bool hooked;

    public static string T(string key)
    {
        if (!hooked)
        {
            hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => cache.Clear();
        }
        if (!cache.TryGetValue(key, out string value))
            cache[key] = value = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
        return value;
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    // In-game language switch: steps the package's selected locale.
    public static void NextLocale()
    {
        List<UnityEngine.Localization.Locale> locales = LocalizationSettings.AvailableLocales.Locales;
        int current = locales.IndexOf(LocalizationSettings.SelectedLocale);
        LocalizationSettings.SelectedLocale = locales[(current + 1) % locales.Count];
    }
}
