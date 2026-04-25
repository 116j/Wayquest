using System.Collections;
using UnityEngine;
using Zenject;

public class BossScript : WalkEnemy
{
    [SerializeField]
    //Roar's activation zone
    DetectZone m_roarZone;
    [SerializeField]
    DetectZone m_closeAttackZone;
    [SerializeField]
    BoxCollider2D[] m_bossCols;

    readonly int m_HashRoar = Animator.StringToHash("Roar");

    readonly float m_roarRecoverTime = 5f;
    readonly float m_closeAttackCooldown = 5f;

    float m_roarTimer;
    //Roar recharge indicator
    bool m_roarRecovering = false;
    bool m_showHealth = true;
    //Close attack indicator
    float m_closeAttackCooldownTimer;
    //Close attack recharge indicator
    bool m_canCloseAttack = true;
    bool m_onTop = false;
    bool m_roaring = false;
    int m_incteaseHealthAmount;


    [Inject]
    FloatingCanvas m_healthBar;
    SoundController m_audio;

    protected override void Start()
    {
        base.Start();
        m_damageable.IncreaseHealth(m_incteaseHealthAmount);
        m_audio = GetComponent<SoundController>();
    }

    protected override void FixedUpdate()
    {
        if (!m_dead)
        {
            if (m_roaring)
                return;

            DetectPlayerOnBack();
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
            //when player eneters roar zone - roars
            if (m_roarZone.TargetDetected && !m_roarRecovering)
            {
                m_roaring = true;
                m_rb.velocity = Vector2.zero;
                m_speed = 0;
                m_audio.PlaySound("Growl");
                StartCoroutine(Roar());
            }
            //when the player enters the attack zone - activates the close attack
            else if (m_canCloseAttack && m_closeAttackZone.TargetDetected)
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

            base.FixedUpdate();
        }
    }
    /// <summary>
    /// Waits while a growl sound ius over and starts roaring 
    /// </summary>
    /// <returns></returns>
    IEnumerator Roar()
    {
        yield return new WaitForSeconds(0.3f);
        m_roaring = false;
        m_attackScript.EnableAttack = false;
        m_anim.SetTrigger(m_HashRoar);
        m_roarRecovering = true;
        m_roarTimer = 0;
        m_attackScript.SetAttackCooldown(0);
    }

    void DetectPlayerOnBack()
    {
        foreach (var col in m_bossCols)
        {
            var hit = Physics2D.BoxCast(
            transform.position,
            col.bounds.size + Vector3.one * 0.02f, 0f,
            Vector3.up * 0.1f,
            0.02f,
            LayerMask.GetMask("Player")
            );
            if (hit.collider != null && !m_onTop)
            {
                m_onTop = true;
                return;
            }
        }

        if (m_onTop)
        {
            m_onTop = false;
        }
    }

    public override void ReceiveDamage(int damage)
    {
        StopAllCoroutines();
        m_audio.StopSound();
        if (m_onTop)
            return;
        m_roaring = false;
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
        m_attackScript.SetAttackCooldown(0.3f);
    }

    public void IncreaseHealth(int increaseAmount)
    {
        m_incteaseHealthAmount += increaseAmount;
    }
    /// <summary>
    /// Resets the close attack
    /// </summary>
    void ResetCloaseAttack()
    {
        m_canCloseAttack = false;
        m_closeAttackCooldownTimer = 0f;
    }

    public override void Reset()
    {
        ResetCloaseAttack();
        m_healthBar.HideBar();
        m_showHealth = true;
        base.Reset();
    }
}
