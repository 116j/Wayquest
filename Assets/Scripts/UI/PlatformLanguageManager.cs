using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Zenject;

public class PlatformLanguageManager : MonoBehaviour
{
    [Inject]
    UIController m_UI;

    Dictionary<string, int> m_yandexToLocaleIndex = new()
    {
        {"en", 0},
        {"pt", 1},
        {"ru", 2},
        {"es", 3},
        {"tr", 4},
    };

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetYandexLanguage();
#endif

    IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        if (CheckYandexPlatform())
        {
            ApplyYandexLanguage();
        }
        else
        {
            ApplySystemLanguage();
        }
    }

    bool CheckYandexPlatform()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string url = Application.absoluteURL;
        if (!string.IsNullOrEmpty(url) &&
            (url.Contains("yandex") || url.Contains("yandexgames")))
        {
            return true;
        }
        return false;
#else
        return false;
#endif
    }

    void ApplyYandexLanguage()
    {
        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string yandexLang = GetYandexLanguage();
            if (string.IsNullOrEmpty(yandexLang))
            {
                throw new System.Exception("Failed to get Yandex SDK");
            }
            if(m_yandexToLocaleIndex.TryGetValue(yandexLang, out var localeIndex))
            {
                m_UI.CurrentLanguage = localeIndex;
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
            }
            else
            {
                throw new System.Exception($"Incorrect language: {yandexLang}");
            }
#else
            throw new System.Exception($"Not WebGL");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to get language from Yandex: {e.Message}. The system language is used.");
            ApplySystemLanguage();
        }
    }

    void ApplySystemLanguage()
    {
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; ++i)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[i];
            if (LocalizationSettings.SelectedLocale == locale)
            {
                m_UI.CurrentLanguage = i;
                return;
            }
        }
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[0];
        m_UI.CurrentLanguage = 0;
    }
}
