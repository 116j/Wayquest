using UnityEngine;
using Zenject;

public class BossScript : WalkEnemy
{
    [SerializeField]
    //Roar's activation zone
    DetectZone m_roarZone;
    [SerializeField]
    AnimationCurve m_bossHealth;

    readonly int m_HashRoar = Animator.StringToHash("Roar");

    BoxCollider2D m_attackZoneCol;
    readonly float m_roarRecoverTime = 5f;
    //Close attack's chance
    readonly float m_attackChance = 0.3f;
    //Offset's X coorfinate of the close attack zone
    readonly float m_closeAttackZoneOffsetX = 0.81308f;
    //Offset's X coorfinate of a regular attack zone
    readonly float m_baseAttackZoneOffsetX = 2.784698f;
    //Size of the collider during a regular attack
    readonly Vector2 m_baseColSize = new(4.28806639f, 3.34450054f);
    //Offset of the collider during a regular attack
    readonly Vector2 m_baseColOffset = new(1.00773644f, -2.37238407f);
    //Size of the collider during the close attack
    readonly Vector2 m_closeAttackColSize = new(1.55885553f, 2.93009996f);
    //Offset of the collider during the close attack
    readonly Vector2 m_closeAttackColOffset = new(-0.356868982f, -2.57958436f);
    readonly float m_closeAttackCooldown = 5f;
    readonly float m_maxLevelCount = 150f;

    float m_roarTimer;
    //Roar recharge indicator
    bool m_roarRecovering = false;
    bool m_showHealth = true;
    //Close attack indicator
    bool m_closeAttack = false;
    float m_closeAttackCooldownTimer;
    //Close attack recharge indicator
    bool m_canCloseAttack = true;

    [Inject]
    FloatingCanvas m_healthBar;

    bool m_increaseHealth = false;

    protected override void Start()
    {
        base.Start();
        //sets the health depending on the number of chunks 
        m_damageable.SetHealth(Mathf.CeilToInt((m_increaseHealth ? 1.2f : 1) * m_bossHealth.Evaluate(m_lvlBuilder.GetLevelChunksCount() / m_maxLevelCount)));
        m_attackZoneCol = m_attackZone.GetComponent<BoxCollider2D>();
    }

    private void LateUpdate()
    {
        //sets the close attack colliders diring the close attack
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
            //recharging the roar
            if (m_roarRecovering)
            {
                m_roarTimer += Time.fixedDeltaTime;
                if (m_roarTimer >= m_roarRecoverTime)
                {
                    m_roarTimer = 0;
                    m_roarRecovering = false;
                }
            }
            //recharging the close atack
            if (!m_canCloseAttack)
            {
                m_closeAttackCooldownTimer += Time.fixedDeltaTime;
                if (m_closeAttackCooldownTimer >= m_closeAttackCooldown)
                {
                    m_closeAttackCooldownTimer = 0f;
                    m_canCloseAttack = true;
                }
            }
            //turns on the close attack - changes the colliders of an object and of the attack zone 
            if (m_canCloseAttack && !m_closeAttack && Random.value <= m_attackChance)
            {
                m_closeAttack = true;
                m_col.offset = m_closeAttackColOffset;
                m_col.size = m_closeAttackColSize;
                m_attackZoneCol.offset = new Vector2(m_closeAttackZoneOffsetX, m_attackZoneCol.offset.y);
            }
            //when player eneters roar zone - roars
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
                //when the player enters the attack zone - activates the close attack
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
                //if the playes is not in the attack zone - chases him
                else if (m_attackZone.TargetDetected)
                {
                    Chase();
                    m_rb.velocity = (m_canMove ? 1 : 0) * m_currentDir * m_speed * Vector2.right;
                    return;
                }
                //if hits the wall - reset the close attack
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
            //updates health on the health indicator
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

    internal void IncreaseHealth()
    {
        m_increaseHealth = true;
    }
    /// <summary>
    /// Resets the close attack
    /// </summary>
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
