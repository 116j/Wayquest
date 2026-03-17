using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Zenject;

public class AdManager : MonoBehaviour
{
    [Inject]
    UIController m_UI;
    [Inject]
    PlayerController m_player;
    [Inject]
    PlatformManager m_platform;

    public enum AdPurpose
    {
        Revive,
        Bonus
    }

    AdPurpose m_currentAd;
    Action m_adResult;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShowRewardedAdForRevive();

    [DllImport("__Internal")]
    private static extern void ShowRewardedAdForBonus();
#endif
    /// <summary>
    /// Отправляет в index.html запрос на просмотр рекламы
    /// </summary>
    /// <param name="purpose">номер причины просмотра рекламы</param>
    public void ShowAd(int purpose)
    {
        m_currentAd = (AdPurpose)purpose;
        m_adResult = null;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (m_currentAd == AdPurpose.Revive)
            ShowRewardedAdForRevive();
        else
            ShowRewardedAdForBonus();
#else
        Debug.Log("The Ad is awailable only in Web");
        OnAdResult("ad_opened");
        OnAdResult("reward_success");
        OnAdResult("ad_closed");
#endif
    }

    public void OnAdResult(string result)
    {
        if (result == "ad_opened")
        {
            Debug.Log("An ad is opened.");
            m_UI.SetAd(true);
            m_UI.Pause(true);
        }
        if (result == "ad_closed" || result == "reward_fail")
        {
            Debug.Log("An ad is closed or failed.");
            m_UI.SetAd(false);
            if (Application.isFocused)
            {
                m_UI.Pause(false);
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
            m_adResult();
        }

        if (result == "reward_success")
        {
            Debug.Log("An ad is watched.");
            switch (m_currentAd)
            {
                //игрок возраждается с полным здоровьем
                case AdPurpose.Revive:
                    m_adResult = Revive;
                    break;
                //добавляет 500 монет
                case AdPurpose.Bonus:
                    m_adResult = AdBonus;
                    break;
            }
        }
    }

    void AdBonus()
    {
        m_UI.AddMoney(500, true);
    }

    void Revive()
    {
        m_UI.Die(false);
        m_player.Restart(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
