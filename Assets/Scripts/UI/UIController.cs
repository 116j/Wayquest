using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UIController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField]
    GameObject m_healthLayout;
    [SerializeField]
    Sprite m_fullHeart;
    [SerializeField]
    Sprite m_emptyHeart;

    [Header("Dash")]
    [SerializeField]
    Image m_dashBar;
    [SerializeField]
    Sprite[] m_dashSprites;

    [Header("Money")]
    [SerializeField]
    GameObject m_moneyLayout;
    [SerializeField]
    TextMeshProUGUI m_moneyText;

    [Header("Shop")]
    [SerializeField]
    GameObject m_shopLayout;
    [SerializeField]
    Button m_firstShopItem;

    [Header("Win")]
    [SerializeField]
    GameObject m_winLayout;
    [SerializeField]
    Button m_mainMenuButton;

    [Header("Die")]
    [SerializeField]
    GameObject m_dieLayout;
    [SerializeField]
    RectTransform m_dieButtonsLayout;
    [SerializeField]
    Button m_restartButton;
    [SerializeField]
    GameObject m_continueButton;


    List<Image> m_hearts;
    int m_currentHeart;
    readonly Vector3 m_heratSize = new Vector3(32.5f, 27);

    //Current health
    public int CurrentHearts => m_currentHeart + 1;
    //Max health
    public int AllHerats => m_hearts.Count;

    int m_money = 0;
    int m_currentMoney = 0;

    bool m_boss = false;
    bool m_ad = false;
    bool m_menu = false;

    public int CurrentLanguage { get; set; }
    [Inject]
    PlayerInput m_input;
    [Inject]
    ShopLayout m_shop;
    [Inject]
    PlatformManager m_platform;

    AudioSource m_moneyAudio;

    private void Awake()
    {
        m_hearts = m_healthLayout.GetComponentsInChildren<Image>().ToList();
        m_currentHeart = m_hearts.Count - 1;
    }

    private void Start()
    {
        m_moneyAudio = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Adds a heart to the max number of hearts
    /// </summary>
    public void AddHeart()
    {
        GameObject heart = new GameObject();
        Image image = heart.AddComponent<Image>();
        if (m_currentHeart < m_hearts.Count - 1)
        {
            image.sprite = m_emptyHeart;
        }
        else
        {
            image.sprite = m_fullHeart;
        }
        heart.transform.SetParent(m_healthLayout.transform, false);
        image.rectTransform.sizeDelta = m_heratSize;
        m_hearts.Add(image);
    }
    /// <summary>
    /// Adds money
    /// </summary>
    /// <param name="amount"></param>
    public void AddMoney(int amount, bool playSound = false)
    {
        if (playSound)
        {
            m_moneyAudio.Play();
        }
        //calculates the duration of the animation
        float baseDuration = Mathf.Abs(amount) * Time.deltaTime;
        float duration = Mathf.Clamp(baseDuration, 0.5f, 3f);
        m_currentMoney = m_money;
        m_money += amount;
        //slowly adds money to make it visible to the player
        DOTween.To(
            () => m_currentMoney,
            x =>
            {
                m_currentMoney = x;
                UpdateMoneyText();
            },
            m_money,
            duration
        ).SetEase(Ease.OutQuad)
        .SetUpdate(true);

    }
    /// <summary>
    /// Updates the money text
    /// </summary>
    void UpdateMoneyText()
    {
        m_moneyText.text = m_currentMoney.ToString();
    }

    public float GetMoney() => m_money;

    /// <summary>
    /// Decreases or increases player's health
    /// </summary>
    /// <param name="damage">damage or health</param>
    public void ChangeHearts(int damage)
    {
        for (int i = 0; i < Mathf.Abs(damage); i++)
        {
            if (m_currentHeart < m_hearts.Count - 1 && damage > 0)
            {
                m_currentHeart++;
                m_hearts[m_currentHeart].sprite = m_fullHeart;
            }
            else if (damage < 0 && m_currentHeart >= 0)
            {
                m_hearts[m_currentHeart].sprite = m_emptyHeart;
                m_currentHeart--;
            }
        }
    }
    /// <summary>
    /// Filling in the dash scale
    /// </summary>
    /// <param name="fill">how full is it</param>
    public void SetDashSprite(float fill)
    {
        m_dashBar.sprite = m_dashSprites[Mathf.FloorToInt(fill * (m_dashSprites.Length - 1))];
    }
    /// <summary>
    /// Shows or hides health, money, and the dash scale
    /// </summary>
    /// <param name="set"></param>
    public void SetStats(bool set)
    {
        m_healthLayout.SetActive(set);
        m_dashBar.gameObject.SetActive(set);
        m_moneyLayout.SetActive(set);
    }
    /// <summary>
    /// Opens the shop's menu
    /// </summary>
    public void OpenShop()
    {
        m_shopLayout.SetActive(!m_shopLayout.activeInHierarchy);
        m_firstShopItem.Select();
        m_shop.Greet();
    }
    /// <summary>
    /// Launches the winning text and the return menu 
    /// </summary>
    public void Win()
    {
        m_input.LockInput(true);
        m_winLayout.SetActive(true);
        m_mainMenuButton.Select();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    /// <summary>
    /// Launches the death text and the restart menu
    /// </summary>
    /// <param name="active"></param>
    public void Die(bool active)
    {
        m_dieLayout.SetActive(active);
        m_restartButton.Select();
        Cursor.visible = active;
        m_input.LockInput(active);
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        //If dies during a boss battle - cannot continue, only restart 
        //m_continueButton.SetActive(!m_boss);
        //m_dieButtonsLayout.sizeDelta = m_boss ? new Vector2(m_dieButtonsLayout.sizeDelta.x, 90) : new Vector2(m_dieButtonsLayout.sizeDelta.x, 170);
    }


    private void OnApplicationPause(bool pause)
    {
        Pause(pause);
    }

    private void OnApplicationFocus(bool focus)
    {
        Pause(!focus);
    }

    public void Pause(bool pause)
    {
        AudioListener.pause = pause || m_ad;
        m_platform.SetGameGameplay(!pause || !m_ad || !m_menu);
        Time.timeScale = pause || m_ad || m_menu ? 0 : 1;
    }
    /// <summary>
    /// If the player died during a boss battle
    /// </summary>
    internal void Boss()
    {
        m_boss = true;
    }

    public void SetAd(bool ad)
    {
        m_ad = ad;
    }

    public void SetMenuPause(bool menu)
    {
        m_menu = menu;
    }
}
