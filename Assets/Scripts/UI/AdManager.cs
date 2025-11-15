using UnityEngine;
using Zenject;
using System.Runtime.InteropServices;

public class AdManager : MonoBehaviour
{
    [Inject]
    UIController m_UI;
    [Inject]
    PlayerController m_player;
    [Inject]
    PlayerInput m_input;

    public enum AdPurpose
    {
        Revive,
        Bonus
    }

    AdPurpose m_currentAd;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShowRewardedAdForRevive();

    [DllImport("__Internal")]
    private static extern void ShowRewardedAdForBonus();
#endif

    public void ShowAd(int purpose)
    {
        m_currentAd = (AdPurpose)purpose;
#if UNITY_WEBGL && !UNITY_EDITOR
        if (m_currentAd == AdPurpose.Revive)
            ShowRewardedAdForRevive();
        else
            ShowRewardedAdForBonus();
#else
        Debug.Log("The Ad is awailable only in Web");
#endif
    }

    public void OnAdResult(string result)
    {
        if (result != "reward_success")
        {
            Debug.Log("An ad isn't watched.");
            return;
        }


        Debug.Log("An ad is watched.");

        switch (m_currentAd)
        {
            case AdPurpose.Revive:
                m_UI.Die(false);
                m_player.Restart(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case AdPurpose.Bonus:
                m_UI.AddMoney(500);
                break;
        }
    }
}
