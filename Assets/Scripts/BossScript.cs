using UnityEngine;
using Zenject;

public class BossScript : WalkEnemy
{
    [SerializeField]
    DetectZone m_roarZone;
    [SerializeField]
    AnimationCurve m_bossHealth;

    readonly int m_HashRoar = Animator.StringToHash("Roar");

    BoxCollider2D m_attackZoneCol;

    readonly float m_roarRecoverTime = 5f;
    readonly float m_attackChance = 0.3f;
    readonly float m_closeAttackZoneOffsetX = 0.81308f;
    readonly float m_baseAttackZoneOffsetX = 2.784698f;
    readonly Vector2 m_baseColSize = new(4.28806639f, 3.34450054f);
    readonly Vector2 m_baseColOffset = new(1.00773644f, -2.37238407f);
    readonly Vector2 m_closeAttackColSize= new(1.55885553f, 2.93009996f);
    readonly Vector2 m_closeAttackColOffset = new(-0.356868982f, -2.57958436f);
    readonly float m_closeAttackCooldown = 5f;
    readonly float m_maxLevelCount = 150f;

    float m_roarTimer;
    bool m_roarRecovering = false;
    bool m_showHealth = true;
    bool m_closeAttack = false;
    float m_closeAttackCooldownTimer;
    bool m_canCloseAttack = true;

    [Inject]
    FloatingCanvas m_healthBar;

    protected override void Start()
    {
        base.Start();
        m_damageable.SetHealth((int)m_bossHealth.Evaluate(m_lvlBuilder.GetMaxRoomsCount() / m_maxLevelCount));
        m_attackZoneCol = m_attackZone.GetComponent<BoxCollider2D>();
    }

    private void LateUpdate()
    {
        if (m_closeAttack)
        {
             m_col.offset = m_closeAttackColOffset;
            m_col.size = m_closeAttackColSize;
        }
    }

    protected override void FixedUpdate()
    {
        if (!m_dead)
        {
            if (m_roarRecovering)
            {
                m_roarTimer += Time.fixedDeltaTime;
                if (m_roarTimer >= m_roarRecoverTime)
                {
                    m_roarTimer = 0;
                    m_roarRecovering = false;
                }
            }

            if (!m_canCloseAttack)
            {
                m_closeAttackCooldownTimer += Time.fixedDeltaTime;
                if (m_closeAttackCooldownTimer >= m_closeAttackCooldown)
                {
                    m_closeAttackCooldownTimer = 0f;
                    m_canCloseAttack = true;
                }
            }
            
            if (m_canCloseAttack && !m_closeAttack && Random.value <= m_attackChance)
            {
                m_closeAttack = true;
                m_col.offset = m_closeAttackColOffset;
                m_col.size = m_closeAttackColSize;
                m_attackZoneCol.offset = new Vector2(m_closeAttackZoneOffsetX, m_attackZoneCol.offset.y);
            }

            if (m_roarZone.TargetDetected && !m_roarRecovering)
            {
                m_attackScript.EnableAttack = false;
                m_anim.SetTrigger(m_HashRoar);
                m_speed = 0;
                m_rb.velocity = Vector2.zero;
                m_roarRecovering = true;
                m_roarTimer = 0;
            }
            else if (m_closeAttack)
            {
                if (m_attackZone.TargetDetected &&
                GetDistance() <= 0.1f)
                {
                    if (!m_attackScript.EnableAttack)
                    {
                        m_attackScript.EnableAttack = true;
                        m_speed = 0f;
                        m_rb.velocity = Vector2.zero;
                        m_waiting = false;
                    }
                    m_anim.SetInteger(m_HashAttackNum, 5);
                    return;
                }
                else if (m_attackZone.TargetDetected)
                {
                    Chase();
                    m_rb.velocity = (m_canMove ? 1 : 0) * m_currentDir * m_speed * Vector2.right;
                    return;
                }
                else if (!m_groundZone.TargetDetected)
                {
                    ResetColliders();
                }
            }

           base.FixedUpdate();
        }
    }

    public override void ReceiveDamage(int damage)
    {
        if (damage == 0)
        {
            m_UI.Win();
        }
        else if (damage < 0)
        {
            if (m_showHealth)
            {
                m_healthBar.ShowBar(transform);
                m_showHealth = false;
                m_UI.Boss();
            }

            m_healthBar.SetHealthSprite(m_damageable.GetHealthPercentage());
        }
        base.ReceiveDamage(damage);
    }

    void ResetColliders()
    {
        m_canCloseAttack = false;
        m_closeAttackCooldownTimer = 0f;
        m_closeAttack = false;
        m_col.offset = m_baseColOffset;
        m_col.size = m_baseColSize;
        m_attackZoneCol.offset = new Vector2(m_baseAttackZoneOffsetX, m_attackZoneCol.offset.y);
    }

    public override void Reset()
    {
        ResetColliders();
        m_healthBar.HideBar();
        m_showHealth = true;
        base.Reset();
    }
}
