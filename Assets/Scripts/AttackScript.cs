using UnityEngine;

public class AttackScript : MonoBehaviour
{
    [SerializeField]
    //Attack number in the animator
    string m_animAttackParameter;
    [SerializeField]
    float m_attackCooldownTime = 1.5f;
    [SerializeField]
    DetectZone m_groundZone;
    //If an object can attack
    public bool EnableAttack { get; set; } = true;

    Animator m_anim;
    Rigidbody2D m_rb;
    //Attack recharge indicator
    bool m_canAttack = true;
    float m_attackCooldown;

    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (m_canAttack && EnableAttack)
        {
            m_anim.SetTrigger(m_animAttackParameter);
        }
        //attack recharge
        else if (!m_canAttack)
        {
            m_attackCooldown -= Time.deltaTime;
            if (m_attackCooldown <= 0)
            {
                m_canAttack = true;
                m_attackCooldown = m_attackCooldownTime;
            }
        }
    }
    /// <summary>
    /// Activates the object's forward movement
    /// </summary>
    public void AttackMoveForward()
    {
        if (m_rb != null && m_groundZone!=null && m_groundZone.TargetDetected)
            m_rb.velocity = transform.right * 5f;
    }
    /// <summary>
    /// Activates the object's backward movement
    /// </summary>
    public void AttackMoveBackwards()
    {
        if (m_rb != null)
            transform.position -= transform.right * 3;
    }
    /// <summary>
    /// Stops the object's movement
    /// </summary>
    public void AttackStop()
    {
        if (m_rb != null)
            m_rb.velocity = Vector2.zero;
    }
    /// <summary>
    /// Stops the attack
    /// </summary>
    public void ResetAttack()
    {
        m_canAttack = false;
        m_anim.ResetTrigger(m_animAttackParameter);
    }

    public void SetAttackCooldown(float time)
    {
        m_attackCooldown = time;
        m_canAttack = false;
    }
}
