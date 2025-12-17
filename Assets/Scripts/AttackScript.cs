using UnityEngine;

public class AttackScript : MonoBehaviour
{
    [SerializeField]
    //Номер атаки в аниматоре
    string m_animAttackParameter;
    [SerializeField]
    //Время перезарядки атаки
    float m_attackCooldownTime = 1.5f;
    //Может ли объект атаковать сейчас
    public bool EnableAttack { get; set; } = true;

    Animator m_anim;
    Rigidbody2D m_rb;
    //Индикатор перезарядки атаки
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
        //перезарядка атаки
        else if (!m_canAttack)
        {
            m_attackCooldown += Time.deltaTime;
            if (m_attackCooldown >= m_attackCooldownTime)
            {
                m_attackCooldown = 0;
                m_canAttack = true;
            }
        }
    }
    /// <summary>
    /// Активирует движение вперед объекта
    /// </summary>
    public void AttackMoveForward()
    {
        if (m_rb != null)
            m_rb.velocity = transform.right * 5f;
    }
    /// <summary>
    /// Активирует движение назад объекта
    /// </summary>
    public void AttackMoveBackwards()
    {
        if (m_rb != null)
            transform.position -= transform.right * 3;
    }
    /// <summary>
    /// Останавливает движение объекта
    /// </summary>
    public void AttackStop()
    {
        if (m_rb != null)
            m_rb.velocity = Vector2.zero;
    }
    /// <summary>
    /// Останавливает атаку
    /// </summary>
    public void ResetAttack()
    {
        m_canAttack = false;
        m_anim.ResetTrigger(m_animAttackParameter);
    }
}
