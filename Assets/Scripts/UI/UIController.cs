using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
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

    [Header("Die")]
    [SerializeField]
    GameObject m_dieLayout;
    [SerializeField]
    RectTransform m_dielButtonslayout;
    [SerializeField]
    GameObject m_continueButton;


    List<Image> m_hearts;
    int m_currentHeart;
    readonly Vector3 m_heratSize = new Vector3(32.5f, 27);

    //Текущее здоровье
    public int CurrentHearts => m_currentHeart + 1;
    //Максимальное здоровье
    public int AllHerats => m_hearts.Count;

    int m_money = 0;
    int m_currentMoney = 0;

    bool m_boss = false;

    public int CurrentLanguage { get; set; }
    [Inject]
    PlayerInput m_input;
    [Inject]
    ShopLayout m_shop;

    private void Awake()
    {
        m_hearts = m_healthLayout.GetComponentsInChildren<Image>().ToList();
        m_currentHeart = m_hearts.Count - 1;
    }

    /// <summary>
    /// Добавляет сердце к макс количеству сердец
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
    /// Прибавляет деньги
    /// </summary>
    /// <param name="amount"></param>
    public void AddMoney(int amount)
    {
        //рассчитывает длительность анимации
        float baseDuration = Mathf.Abs(amount) * Time.deltaTime;
        float duration = Mathf.Clamp(baseDuration, 0.5f, 3f);
        m_currentMoney = m_money;
        m_money += amount;
        //медленно пребавляет деньги, чтобы было видно игроку
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
    /// Обновляет текст денег
    /// </summary>
    void UpdateMoneyText()
    {
        m_moneyText.text = m_currentMoney.ToString();
    }

    public float GetMoney() => m_money;

    /// <summary>
    /// Убирает или добавляет здоровье игрока
    /// </summary>
    /// <param name="damage">урон или здоровье</param>
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
    /// Заполнение шкалы рывка
    /// </summary>
    /// <param name="fill">насколько заполнен</param>
    public void SetDashSprite(float fill)
    {
        m_dashBar.sprite = m_dashSprites[Mathf.FloorToInt(fill * (m_dashSprites.Length - 1))];
    }
    /// <summary>
    /// Показывает или скрывает здоровье, деньги и шкалу рывка
    /// </summary>
    /// <param name="set">показать</param>
    public void SetStats(bool set)
    {
        m_healthLayout.SetActive(set);
        m_dashBar.gameObject.SetActive(set);
        m_moneyLayout.SetActive(set);
    }
    /// <summary>
    /// Открывает меню магазина
    /// </summary>
    public void OpenShop()
    {
        m_shopLayout.SetActive(!m_shopLayout.activeInHierarchy);
        m_firstShopItem.Select();
        m_shop.Greet();
    }
    /// <summary>
    /// Запускает текст о выигрыше и меню возвращения в меню
    /// </summary>
    public void Win()
    {
        m_input.LockInput(true);
        m_winLayout.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    /// <summary>
    /// Запускает текст о смерти и меню перезапуска
    /// </summary>
    /// <param name="active"></param>
    public void Die(bool active)
    {
        m_dieLayout.SetActive(active);
        Cursor.visible = active;
        m_input.LockInput(active);
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
        //если умекр во время битвы с боссом - нельзя продожить, только перезапуск
        m_continueButton.SetActive(!m_boss);
        m_dielButtonslayout.sizeDelta = m_boss ? new Vector2(m_dielButtonslayout.sizeDelta.x, 90) : new Vector2(m_dielButtonslayout.sizeDelta.x, 170);
    }


    private void OnApplicationPause(bool pause)
    {
        AudioListener.pause = pause;
    }

    private void OnApplicationFocus(bool focus)
    {
        AudioListener.pause = !focus;
    }
    /// <summary>
    /// Если игрок умер во время битвы с боссом
    /// </summary>
    internal void Boss()
    {
        m_boss = true;
    }
}
