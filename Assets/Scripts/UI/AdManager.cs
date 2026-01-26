using System.Runtime.InteropServices;
using UnityEngine;
using Zenject;

public class AdManager : MonoBehaviour
{
    [Inject]
    UIController m_UI;
    [Inject]
    PlayerController m_player;

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
    /// <summary>
    /// Отправляет в index.html запрос на просмотр рекламы
    /// </summary>
    /// <param name="purpose">номер причины просмотра рекламы</param>
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
        //если не получилось загрузить рекламу - выход
        if (result != "reward_success")
        {
            Debug.Log("An ad isn't watched.");
            return;
        }


        Debug.Log("An ad is watched.");

        switch (m_currentAd)
        {
            //игрок возраждается с полным здоровьем
            case AdPurpose.Revive:
                m_UI.Die(false);
                m_player.Restart(false);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            //добавляет 500 монет
            case AdPurpose.Bonus:
                m_UI.AddMoney(500);
                break;
        }
    }
}
