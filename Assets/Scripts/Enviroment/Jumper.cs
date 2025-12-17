using UnityEngine;

public class Jumper : Trap
{
    readonly float m_jumpPower = 2f;
    int m_wallHeigh = 10;
    GameObject m_target;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            m_anim.SetTrigger("Attack");
            m_target = collision.gameObject;
            m_target.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            m_anim.ResetTrigger("Attack");
            m_target.transform.SetParent(null);
            m_target = null;
        }
    }

    public void Jump()
    {
        if (m_target != null)
        {
            m_target.GetComponent<PlayerController>().Jump();
            m_target.GetComponent<Rigidbody2D>().velocity = new Vector2(m_target.GetComponent<Rigidbody2D>().velocity.x, m_jumpPower * m_wallHeigh);
        }
    }
    /// <summary>
    /// Устанавливает высоту стены, на которую нужно запрыгнуть
    /// </summary>
    /// <param name="height"></param>
    public void SetWallHeight(int height)
    {
        m_wallHeigh = height;
    }
}
