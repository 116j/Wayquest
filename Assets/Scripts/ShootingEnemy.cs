using UnityEngine;

public class ShootingEnemy : WalkEnemy
{
    [SerializeField]
    AttackScript m_shootScript;
    [SerializeField]
    DetectZone m_shootZone;

    readonly int m_HashShootNum = Animator.StringToHash("ShootNum");

    readonly int m_shootNum = 2;
    protected override void FixedUpdate()
    {
        //если игрок вошел в зону стрельбы, но не в зону преследования - стрелять
        if (!m_dead && m_shootZone.TargetDetected && !m_detectZone.TargetDetected)
        {
            m_shootScript.EnableAttack = true;
            m_speed = 0f;
            m_waiting = false;
            m_rb.velocity = Vector2.zero;
            m_anim.SetInteger(m_HashShootNum, Random.Range(0, m_shootNum));
        }
        else
        {
            m_shootScript.EnableAttack = false;
            base.FixedUpdate();
        }
    }
}
