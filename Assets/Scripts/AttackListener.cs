using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AttackListener : MonoBehaviour
{
    [SerializeField]
    int m_damage = 1;
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Damagable>(out var damagable))
        {
            //если объект не в статусе НЕУЯЗВИМ, или в статусе НЕУЯЗВИМ, но повернут к атаке спиной - проходит урон
            if (!damagable.Invinsible || damagable.Invinsible && (Vector2.Dot(collision.transform.right, transform.right) == 1))
            {
                damagable.ApplyDamage(m_damage);
            }
        }
    }
    /// <summary>
    /// Повышает урон от данной атаки на 1
    /// </summary>
    public void IncreaseDamage()
    {
        m_damage++;
    }
}
