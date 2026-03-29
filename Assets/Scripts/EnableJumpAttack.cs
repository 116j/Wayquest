using UnityEngine;

public class EnableJumpAttack : MonoBehaviour
{
    [SerializeField]
    DetectZone m_wallsDetect;

    WalkEnemy m_enemyController;

    readonly int m_attacksCount = 3;
    bool m_attackDisabled = false;

    private void Start()
    {
        m_enemyController = GetComponent<WalkEnemy>();
    }

    private void Update()
    {
        //If there is a wall in front - a jump attack can't be performed
        if (!m_attackDisabled && m_wallsDetect.TargetDetected)
        {
            m_enemyController.SetAttacksCount(m_attacksCount - 1);
            m_attackDisabled = true;
        }
        else if (m_attackDisabled && !m_wallsDetect.TargetDetected)
        {
            m_attackDisabled = false;
            m_enemyController.SetAttacksCount(m_attacksCount);
        }
    }
}
