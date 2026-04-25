using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Zenject;

public class SettingsMenu : MonoBehaviour
{
    [Header("Layouts")]
    [SerializeField]
    TextMeshProUGUI m_header;
    [SerializeField]
    RectTransform m_layout;

    [Header("Audio")]
    [SerializeField]
    AudioMixer m_mixer;
    [SerializeField]
    Transform m_gameVolumeFill;
    [SerializeField]
    Slider m_gameVolumeSlider;
    [SerializeField]
    Transform m_musicVolumeFill;
    [SerializeField]
    Slider m_musicVolumeSlider;
    [SerializeField]
    Transform m_sfxVolumeFill;
    [SerializeField]
    Slider m_sfxVolumeSlider;

    [Header("Display")]
    [SerializeField]
    TextMeshProUGUI m_languageText;
    [SerializeField]
    Toggle m_fullScreenToggle;

    [Header("Controls")]
    [SerializeField]
    GameObject m_gamepadContent;
    [SerializeField]
    GameObject m_keyboardContent;

    [Header("Level Builder")]
    [SerializeField]
    Transform m_chunksCountFill;
    [SerializeField]
    Slider m_chunksCountSlider;
    [SerializeField]
    TextMeshProUGUI m_chunksCountText;
    [SerializeField]
    TextMeshProUGUI[] m_chunkStrategyWeightTexts;
    [SerializeField]
    Slider[] m_chunkStrategySliders;
    [SerializeField]
    LevelValues m_values;

    int m_currentLanguageInd = 0;

    bool m_fullScreen = true;
    //Ddefault number of chunks
    readonly int m_defaultChunksCount = 50;
    //Default strategy weights
    readonly float[] m_defaultStrategyWeights = { 0.6f, 0.15f, 0.3f, 0.15f, 0.3f };
    //Menu section names
    string[][] m_layoutNames =
    {
        new string[]
        {
            "DISPLAY",
            "PANTALHA",
            "ЭКРАН",
            "PANTALLA",
            "EKRAN"
        },
        new string[]
        {
            "AUDIO",
            "ÁUDIO",
            "АУДИО",
            "AUDIO",
            "SES"
        },
        new string[]
        {
            "CONTROLS",
            "CONTROLOS",
            "УПРАВЛЕНИЕ",
            "CONTROLES",
            "KONTROLLER"
        },
        new string[]
        {
            "LEVEL BUILDER",
            "CONSTRUTOR DE NÍVEIS",
            "СОЗДАТЕЛЬ УРОВНЕЙ",
            "CONSTRUCTOR DE NIVELES",
            "SEVİYE OLUŞTURUCU"
        }
    };

    string[][] m_localizationNames =
    {
        new string[]
        {
            "ENGLISH (EN)",
            "PORTUGUESE (PT)",
            "RUSSIAN (RU)",
            "SPANISH (ES)",
            "TURKISH (TR)"
        },
        new string[]
        {
            "INGLÊS (EN)",
            "PORTUGUÊS (PT)",
            "RUSSO (RU)",
            "ESPANHOL (ES)",
            "TURCO (TR)"
        },
        new string[]
        {
            "АНГЛИЙСКИЙ (EN)",
            "ПОРТУГАЛЬСКИЙ (PT)",
            "РУССКИЙ (RU)",
            "ИСПАНСКИЙ (ES)",
            "ТУРЕЦКИЙ (TR)"
        },
        new string[]
        {
            "INGLÉS (EN)",
            "PORTUGUÉS (PT)",
            "RUSA (RU)",
            "ESPAÑOL (ES)",
            "TURCA (TR)"
        },
        new string[]
        {
            "İNGİLİZ (EN)",
            "PORTEKİZ (PT)",
            "RUS (RU)",
            "İSPANYOL (ES)",
            "TÜRK (TR)"
        },
    };

    [Inject]
    PlayerInput m_input;
    [Inject]
    UIController m_UI;

    void Start()
    {
        m_fullScreenToggle.isOn = m_fullScreen = Screen.fullScreen;
        m_currentLanguageInd = m_UI.CurrentLanguage;
        m_languageText.text = m_localizationNames[m_currentLanguageInd][m_currentLanguageInd];
        LocalizationSettings.SelectedLocaleChanged += (locale) =>
        {
            m_currentLanguageInd = m_UI.CurrentLanguage;
            m_languageText.text = m_localizationNames[m_UI.CurrentLanguage][m_currentLanguageInd];
        };
    }

    public void Display()
    {
        m_header.text = m_layoutNames[0][m_UI.CurrentLanguage];
        m_layout.sizeDelta = new Vector2(m_layout.sizeDelta.x, 220);
        SetDisplayValues();
    }

    void SetDisplayValues()
    {
        m_fullScreenToggle.isOn = m_fullScreen = Screen.fullScreen;
        m_languageText.text = m_localizationNames[m_UI.CurrentLanguage][m_UI.CurrentLanguage];
    }

    public void FullScreen(bool full)
    {
        m_fullScreen = full;
    }

    public void SetLanguageUp()
    {
        if (m_currentLanguageInd < m_localizationNames[m_UI.CurrentLanguage].Length - 1)
        {
            m_languageText.text = m_localizationNames[m_UI.CurrentLanguage][++m_currentLanguageInd];
        }
    }

    public void SetLanguageDown()
    {
        if (m_currentLanguageInd > 0)
        {
            m_languageText.text = m_localizationNames[m_UI.CurrentLanguage][--m_currentLanguageInd];
        }
    }

    public void SaveDisplay()
    {
        m_UI.CurrentLanguage = m_currentLanguageInd;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[m_currentLanguageInd];
        m_languageText.text = m_localizationNames[m_UI.CurrentLanguage][m_UI.CurrentLanguage];
        Screen.fullScreen = m_fullScreen;
    }

    public void Audio()
    {
        m_header.text = m_layoutNames[1][m_UI.CurrentLanguage];
        m_layout.sizeDelta = new Vector2(m_layout.sizeDelta.x, 290);
    }

    public void ChangeGameVolume(float value)
    {
        int num = Mathf.RoundToInt(value / m_gameVolumeSlider.maxValue * m_gameVolumeFill.childCount) - 1;
        for (int i = 0; i <= num; i++)
        {
            m_gameVolumeFill.GetChild(i).gameObject.SetActive(true);
        }

        for (int i = num + 1; i < m_gameVolumeFill.childCount; i++)
        {
            m_gameVolumeFill.GetChild(i).gameObject.SetActive(false);
        }
        float dB = Mathf.Lerp(-80f, 5f, Mathf.Log10(value * 9f + 1f) / Mathf.Log10(10f));
        m_mixer.SetFloat("MasterVolume", dB);
    }

    public void ChangeMusicVolume(float value)
    {
        int num = Mathf.RoundToInt(value / m_musicVolumeSlider.maxValue * m_musicVolumeFill.childCount) - 1;
        for (int i = 0; i <= num; i++)
        {
            m_musicVolumeFill.GetChild(i).gameObject.SetActive(true);
        }

        for (int i = num + 1; i < m_musicVolumeFill.childCount; i++)
        {
            m_musicVolumeFill.GetChild(i).gameObject.SetActive(false);
        }
        float dB = Mathf.Lerp(-80f, -7f, Mathf.Log10(value * 9f + 1f) / Mathf.Log10(10f));
        m_mixer.SetFloat("MusicVolume", dB);
    }
    public void ChangeEffectsVolume(float value)
    {
        int num = Mathf.RoundToInt(value / m_sfxVolumeSlider.maxValue * m_sfxVolumeFill.childCount) - 1;
        for (int i = 0; i <= num; i++)
        {
            m_sfxVolumeFill.GetChild(i).gameObject.SetActive(true);
        }

        for (int i = num + 1; i < m_sfxVolumeFill.childCount; i++)
        {
            m_sfxVolumeFill.GetChild(i).gameObject.SetActive(false);
        }
        float dB = Mathf.Lerp(-80f, -4f, Mathf.Log10(value * 9f + 1f) / Mathf.Log10(10f));
        m_mixer.SetFloat("SFXVolume", dB);
    }

    public void Mute(bool mute)
    {
        AudioListener.volume = mute ? 0 : 1;
    }

    public void Controls()
    {
        m_header.text = m_layoutNames[2][m_UI.CurrentLanguage];
        if (m_input.GetCurrentDeviceType() == "Gamepad")
        {
            m_layout.sizeDelta = new Vector2(m_layout.sizeDelta.x, 380);
            m_gamepadContent.SetActive(true);
            m_keyboardContent.SetActive(false);
        }
        else
        {
            m_layout.sizeDelta = new Vector2(m_layout.sizeDelta.x, 430);
            m_gamepadContent.SetActive(false);
            m_keyboardContent.SetActive(true);
        }
    }

    public void LvlBuilder()
    {
        m_header.text = m_layoutNames[3][m_UI.CurrentLanguage];
        m_layout.sizeDelta = new Vector2(m_layout.sizeDelta.x, 490);
        SetSliders();
    }

    public void ChangeChunksCount(float value)
    {
        m_chunksCountText.text = value.ToString();

        int num = Mathf.RoundToInt(value / m_chunksCountSlider.maxValue * m_chunksCountFill.childCount) - 1;
        for (int i = 0; i <= num; i++)
        {
            m_chunksCountFill.GetChild(i).gameObject.SetActive(true);
        }

        for (int i = num + 1; i < m_chunksCountFill.childCount; i++)
        {
            m_chunksCountFill.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ChangeBaseChunkWeight(float value)
    {
        m_chunkStrategyWeightTexts[0].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeCeilChunkWeight(float value)
    {
        m_chunkStrategyWeightTexts[1].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeGridChunkWeight(float value)
    {
        m_chunkStrategyWeightTexts[2].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeMovingPlatformChunkWeight(float value)
    {
        m_chunkStrategyWeightTexts[3].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeDestroyableChunkWeight(float value)
    {
        m_chunkStrategyWeightTexts[4].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }
    /// <summary>
    /// Updates slider values according to the global variable
    /// </summary>
    void SetSliders()
    {
        for (int i = 0; i < m_chunkStrategySliders.Length; i++)
        {
            m_chunkStrategySliders[i].value = m_values.m_strategyWeights[i];
        }
        m_chunksCountSlider.value = m_values.m_chunksCount;
    }
    /// <summary>
    /// Sets the weights and number of rooms to a global variable
    /// </summary>
    public void SaveLevelBuilder()
    {
        for (int i = 0; i < m_values.m_strategyWeights.Length; i++)
        {
            m_values.m_strategyWeights[i] = m_chunkStrategySliders[i].value;
        }
        m_values.m_chunksCount = (int)m_chunksCountSlider.value;
    }
    /// <summary>
    /// Resets the default values for the number of rooms and strategy weights
    /// </summary>
    public void SetDefault()
    {
        for (int i = 0; i < m_values.m_strategyWeights.Length; i++)
        {
            m_values.m_strategyWeights[i] = m_defaultStrategyWeights[i];
        }

        m_values.m_chunksCount = m_defaultChunksCount;

        SetSliders();
    }
}
