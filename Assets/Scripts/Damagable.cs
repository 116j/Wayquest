using UnityEngine;
using UnityEngine.Events;

public class Damagable : MonoBehaviour
{
    [SerializeField]
    int m_maxHealth = 4;
    [SerializeField]
    float m_recoverTime = 2f;
    [SerializeField]
    UnityEvent<int> m_receiver;

    bool m_dead = false;
    bool m_recovering = false;
    bool m_afterDeathRecovering = false;

    float m_recoverTimer = 0f;
    float m_freezeTime = 0f;
    float m_afterDeathRecoverTimer = 0f;

    int m_health = 4;
    public bool Invincible { get; set; } = false;
    public bool Freezed { get; private set; } = false;

    private void Start()
    {
        m_health = m_maxHealth;
    }

    private void Update()
    {
        if (m_recovering)
        {
            m_recoverTimer += Time.deltaTime;
            if (m_recoverTimer >= m_recoverTime)
            {
                m_recovering = false;
                m_recoverTimer = 0f;
            }
        }
        if (m_afterDeathRecovering)
        {
            m_afterDeathRecoverTimer+= Time.deltaTime;
            if (m_afterDeathRecoverTimer >= m_recoverTime)
            {
                m_afterDeathRecovering = false;
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
                m_afterDeathRecoverTimer = 0f;
            }
        }
        //freezes for a while
        else if (Freezed)
        {
            m_freezeTime -= Time.deltaTime;
            if (m_freezeTime <= 0)
            {
                Freezed = false;
            }
        }
    }
    /// <summary>
    /// Freezed stops when taking damage
    /// </summary>
    /// <param name="damage"></param>
    public void ApplyDamage(int damage)
    {
        if (m_dead || m_recovering) return;

        Freezed = false;
        m_recovering = true;
        m_health = Mathf.Min(m_health - damage, m_maxHealth);

        m_receiver.Invoke(-damage);
        if (m_health <= 0)
        {
            m_health = 0;
            m_dead = true;
            m_receiver.Invoke(0);
        }
    }

    public void Freeze(float time)
    {
        Freezed = true;
        m_freezeTime = time;
    }

    public void ApplyHealth(int healPoints)
    {
        if (m_health >= m_maxHealth)
            return;

        m_health += healPoints;
        m_receiver.Invoke(healPoints);
        if (m_health <= 0)
        {
            m_health = 0;
            m_dead = false;
            m_receiver.Invoke(0);
        }
    }

    public void IncreaseHealth()
    {
        m_maxHealth++;
        ApplyHealth(1);
    }

    public void IncreaseHealth(int increaseAmount)
    {
        m_maxHealth+= increaseAmount;
        m_health += increaseAmount;
    }

    public void SetHealth(int health)
    {
        m_maxHealth = health;
        m_health = health;
    }

    public float GetHealthPercentage() => Mathf.Max(0, m_health) / (1.0f * m_maxHealth);
    /// <summary>
    /// Restores full health and becomes invincible if necessary
    /// </summary>
    /// <param name="invincible">if invincible after reborn</param>
    public void Reborn(bool invincible = false)
    {
        m_recovering = invincible;
        Freezed = false;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), invincible);
        m_afterDeathRecovering = invincible;
        ApplyHealth(m_maxHealth - m_health);
        m_dead = false;
    }
}
