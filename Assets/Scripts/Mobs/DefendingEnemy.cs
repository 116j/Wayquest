using UnityEngine;

public class DefendingEnemy : WalkEnemy
{
    [SerializeField]
    DetectZone m_playerAttackZone;

    Damagable m_damagable;

    readonly int m_HashProtect = Animator.StringToHash("Protect");

    bool m_protecting = false;
    //Protecting time
    readonly float m_protectCooldownTime = 3f;
    float m_protectCooldown;
    bool m_inDelayCooldown;
    //Protect recharge time
    readonly float m_protectDelay = 2f;
    float m_protectDelayCooldown;

    protected override void Start()
    {
        base.Start();
        m_damagable = GetComponent<Damagable>();
    }

    protected override void Update()
    {
        if (m_protecting)
        {
            m_protectCooldown += Time.deltaTime;
            //cancels protection if time has run out, the player has entered the attack zone, or the enemy has left the player's attack zone
            if (m_protectCooldown >= m_protectCooldownTime || !m_playerAttackZone.TargetDetected || m_attackZone.TargetDetected)
            {
                DisableProtection();
            }
        }

        base.Update();
    }

    void DisableProtection()
    {
        m_anim.SetBool(m_HashProtect, false);
        m_protectCooldown = 0;
        m_protecting = false;
        m_damagable.Invincible = false;
        m_protectDelayCooldown = m_protectDelay;
        m_inDelayCooldown = true;
    }

    void EnableProtection()
    {
        m_attackScript.EnableAttack = false;
        m_rb.velocity = Vector2.zero;
        m_speed = 0;
        m_protecting = true;
        m_anim.SetBool(m_HashProtect, true);
        m_damagable.Invincible = true;
        m_waiting = m_inDelayCooldown = false;
    }

    protected override void FixedUpdate()
    {
        if (!m_protecting&&!m_dead)
        {
            if (m_inDelayCooldown)
            {
                m_protectDelayCooldown -= Time.fixedDeltaTime;
                if(m_protectDelayCooldown <= 0)
                {
                    m_inDelayCooldown = false;
                }
            }
            // if the player is out of the attack zone and the enemy is in the player's attack zone, the protection is recharged and activated,
            // otherwise - reset 
            if (m_playerAttackZone.TargetDetected && !m_attackZone.TargetDetected)
            {
                if (!m_inDelayCooldown)
                {
                    EnableProtection();
                }
                else
                {
                    base.FixedUpdate();
                }
            }
            else
            {
                base.FixedUpdate();
            }
        }
    }
    /// <summary>
    /// Activates protection when receiving damage
    /// </summary>
    /// <param name="damage"></param>
    public override void ReceiveDamage(int damage)
    {
        base.ReceiveDamage(damage);
        if (damage < 0)
        {
            EnableProtection();
        }
    }
}
