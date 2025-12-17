using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class FloatingCanvas : MonoBehaviour
{
    [SerializeField]
    Transform m_player;
    [SerializeField]
    TextMeshProUGUI m_playerTextG;
    [SerializeField]
    TextMeshProUGUI m_playerTextKB;
    [SerializeField]
    TextMeshProUGUI m_shopTextG;
    [SerializeField]
    TextMeshProUGUI m_shopTextKB;
    [SerializeField]
    Image m_healthBar;
    [SerializeField]
    Sprite[] m_healthSprites;

    bool m_showPlayerText;
    bool m_showShopText;
    bool m_showDinoBar;

    Vector3 m_shopOffset;
    Vector3 m_dinoBarOffset = new(1.8f, 0);

    [Inject]
    PlayerInput m_input;

    Transform m_dino;

    void Update()
    {
        if (m_showPlayerText)
        {
            transform.position = m_player.position + Vector3.up * 2f;
        }
        else if (m_showShopText)
        {
            transform.position = m_shopOffset + new Vector3(0.7629f, 1);
        }
        else if (m_showDinoBar)
        {
            transform.position = m_dino.position + m_dinoBarOffset;
        }
    }

    public void ShowPlayerText(bool show)
    {
        m_playerTextG.gameObject.SetActive(show && m_input.GetCurrentDeviceType() == "Gamepad");
        m_playerTextKB.gameObject.SetActive(show && !m_playerTextG.isActiveAndEnabled);
        m_showPlayerText = show;
    }

    public void ShowShopText(bool show, Vector3 shopLocation)
    {
        m_shopTextG.gameObject.SetActive(show && m_input.GetCurrentDeviceType() == "Gamepad");
        m_shopTextKB.gameObject.SetActive(show && !m_shopTextG.isActiveAndEnabled);
        m_showPlayerText = show;
        m_shopOffset = shopLocation;
    }

    public void ShowBar(Transform enemy)
    {
        SetHealthSprite(1);
        m_showDinoBar = true;
        m_dino = enemy;
        m_healthBar.gameObject.SetActive(true);
    }

    public void HideBar()
    {
        m_showDinoBar = false;
        m_healthBar.gameObject.SetActive(false);
    }

    public void SetHealthSprite(float fill)
    {
        m_healthBar.sprite = m_healthSprites[Mathf.FloorToInt(fill * (m_healthSprites.Length - 1))];
    }
}
