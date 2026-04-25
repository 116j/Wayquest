using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackListener : MonoBehaviour
{
    [SerializeField]
    int m_damage = 1;

    Damagable m_damagable;

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out m_damagable) ||
            collision.transform.parent != null && collision.transform.parent.CompareTag("Enemy") && collision.transform.parent.TryGetComponent(out m_damagable))
        {
            //if an object is not in the INVINCIBLE status, or in the INVINCIBLE status but turned his back to the attack - damage passes
            if (!m_damagable.Invincible || m_damagable.Invincible && (Vector2.Dot(collision.transform.right, transform.right) == 1))
            {
                m_damagable.ApplyDamage(m_damage);
            }
        }
    }
    /// <summary>
    /// Increases damage by 1
    /// </summary>
    public void IncreaseDamage()
    {
        m_damage++;
    }
}
