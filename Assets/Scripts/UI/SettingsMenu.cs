using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;
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

    KeyValuePair<int, int> m_currentResolution;
    bool m_fullScreen = true;
    //Количество чанков по умолчанию
    readonly int m_defaultChunksCount = 50;
    //Значения весов стратегий по умолчанию
    readonly float[] m_defaultStrategyWeights = { 0.6f, 0.15f, 0.3f, 0.15f, 0.3f };
    //Названия разделов меню
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

    [Inject]
    PlayerInput m_input;
    [Inject]
    UIController m_UI;

    void Start()
    {
        m_fullScreenToggle.isOn = m_fullScreen = Screen.fullScreen;
        m_currentLanguageInd = m_UI.CurrentLanguage;
        m_languageText.text = LocalizationSettings.AvailableLocales.Locales[m_currentLanguageInd].name.ToUpper();
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
        m_languageText.text = LocalizationSettings.AvailableLocales.Locales[m_UI.CurrentLanguage].name.ToUpper();
    }

    public void FullScreen(bool full)
    {
        m_fullScreen = full;
    }

    public void SetLanguageUp()
    {
        if (m_currentLanguageInd < LocalizationSettings.AvailableLocales.Locales.Count - 1)
        {
            m_languageText.text = LocalizationSettings.AvailableLocales.Locales[++m_currentLanguageInd].name.ToUpper();
        }
    }

    public void SetLanguageDown()
    {
        if (m_currentLanguageInd > 0)
        {
            m_languageText.text = LocalizationSettings.AvailableLocales.Locales[--m_currentLanguageInd].name.ToString();
        }
    }

    public void SaveDisplay()
    {
        m_UI.CurrentLanguage = m_currentLanguageInd;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[m_currentLanguageInd];
        Screen.SetResolution(m_currentResolution.Key, m_currentResolution.Key, m_fullScreen);
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
        m_mixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
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
        m_mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
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
        m_mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
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
    /// Обновляет значения слайдеров в соотвествии с глобальной переменной
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
    /// Устанавливает веса и количество комнат в глобальную переменную
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
    /// Сбрасыввет значения количества комнат и весов стратегий по умолчанию
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
