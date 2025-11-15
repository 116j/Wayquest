using ModestTree;
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
    Transform m_roomsCountFill;
    [SerializeField]
    Slider m_roomsCountSlider;
    [SerializeField]
    TextMeshProUGUI m_roomsCountText;
    [SerializeField]
    TextMeshProUGUI[] m_roomStrategyWeightTexts;
    [SerializeField]
    Slider[] m_roomStrategySliders;
    [SerializeField]
    LevelValues m_values;

    int m_currentLanguageInd = 0;
    int m_currentResolutionInd;

    KeyValuePair<int, int> m_currentResolution;
    bool m_fullScreen = true;

    readonly int m_defaultRoomsCount = 50;
    readonly float[] m_defaultStrategyWeights = { 0.6f, 0.15f, 0.3f, 0.15f, 0.3f };

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

    public void ChangeRoomsCount(float value)
    {
        m_roomsCountText.text = value.ToString();

        int num = Mathf.RoundToInt(value / m_roomsCountSlider.maxValue * m_roomsCountFill.childCount) - 1;
        for (int i = 0; i <= num; i++)
        {
            m_roomsCountFill.GetChild(i).gameObject.SetActive(true);
        }

        for (int i = num + 1; i < m_roomsCountFill.childCount; i++)
        {
            m_roomsCountFill.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ChangeBaseRoomWeight(float value)
    {
        m_roomStrategyWeightTexts[0].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeCeilRoomWeight(float value)
    {
        m_roomStrategyWeightTexts[1].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeGridRoomWeight(float value)
    {
        m_roomStrategyWeightTexts[2].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeMovingPlatformRoomWeight(float value)
    {
        m_roomStrategyWeightTexts[3].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    public void ChangeDestroyableRoomWeight(float value)
    {
        m_roomStrategyWeightTexts[4].text = value.ToString("F3", CultureInfo.InvariantCulture);
    }

    void SetSliders()
    {
        for (int i = 0; i < m_roomStrategySliders.Length; i++)
        {
            m_roomStrategySliders[i].value = m_values.m_strategyWeights[i];
        }
        m_roomsCountSlider.value = m_values.m_roomsCount;
    }

    public void SaveLevelBuilder()
    {
        for (int i = 0; i < m_values.m_strategyWeights.Length; i++)
        {
            m_values.m_strategyWeights[i] = m_roomStrategySliders[i].value;
        }
        m_values.m_roomsCount = (int)m_roomsCountSlider.value;
    }

    public void SetDefault()
    {
        for (int i = 0; i < m_values.m_strategyWeights.Length; i++)
        {
            m_values.m_strategyWeights[i] = m_defaultStrategyWeights[i];
        }

        m_values.m_roomsCount = m_defaultRoomsCount;

        SetSliders();
    }
}
