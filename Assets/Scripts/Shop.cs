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
        //при входе в зону магазина включает взаимодействие с ним
        if (collision.CompareTag("Player"))
        {
            m_text.ShowShopText(true, transform.position);
            m_input.EnableShop(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //при выходе из зоны магазина выключает взаимодействие с ним
        if (collision.CompareTag("Player"))
        {
            m_text.ShowShopText(false, Vector3.zero);
            m_input.EnableShop(false);
        }
    }
    /// <summary>
    /// Сигнализирует, что магазина больше нет
    /// </summary>
    private void OnDestroy()
    {
        m_lvlBuilder.ShopDestroyed();
    }
}
