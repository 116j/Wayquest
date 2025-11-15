using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeListener : MonoBehaviour
{
    [SerializeField]
    float m_time = 1;
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")&&collision.TryGetComponent<Damagable>(out var damagable)
            || collision.CompareTag("enemyAttack") && collision.transform.parent.TryGetComponent<Damagable>(out damagable))
        {
            if (!damagable.Freezed)
            {
                damagable.Freezed = true;
                damagable.Freeze(m_time);
            }
        }
    }
}
