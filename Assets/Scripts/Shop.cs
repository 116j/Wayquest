using UnityEngine;
using Zenject;

public class Shop : MonoBehaviour
{
    [Inject]
    LevelBuilder m_lvlBuilder;
    [Inject]
    FloatingCanvas m_text;
    [Inject]
    PlayerInput m_input;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //when entering the shop area, it enables interaction with it
        if (collision.CompareTag("Player"))
        {
            m_text.ShowShopText(true, transform.position);
            m_input.EnableShop(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //when leaving the shop area, it disables interaction with it
        if (collision.CompareTag("Player"))
        {
            m_text.ShowShopText(false, Vector3.zero);
            m_input.EnableShop(false);
        }
    }
    /// <summary>
    /// Indicates that the shop is no longer there
    /// </summary>
    private void OnDestroy()
    {
        m_lvlBuilder.ShopDestroyed();
    }
}
