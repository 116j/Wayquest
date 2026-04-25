using Cinemachine;
using UnityEngine;

public class CameraVerticalMovement : MonoBehaviour
{
    [Header("Up")]
    [SerializeField]
    float m_upDeadZone = 0.2f;
    [SerializeField]
    float m_upSoftZone = 0.4f;
    [SerializeField]
    float m_upBiasY = 0f;
    [SerializeField]
    float m_upDamping = 0.8f;

    [Header("Down")]
    [SerializeField]
    float m_downDeadZone = 0.5f;
    [SerializeField]
    float m_downSoftZone = 0.8f;
    [SerializeField]
    float m_downBiasY = -0.3f;
    [SerializeField]
    float m_downDamping = 1.8f;

    [Header("Stay")]
    [SerializeField]
    float m_stayDeadZone = 0.5f;
    [SerializeField]
    float m_staySoftZone = 0.8f;
    [SerializeField]
    float m_stayBiasY = -0.3f;
    [SerializeField]
    float m_stayDamping = 1.8f;

    [Header("")]
    [SerializeField]
    float m_transitionSpeed = 3f;

    CinemachineVirtualCamera vcam;
    CinemachineFramingTransposer m_transposer;
    Rigidbody2D m_playerRb;

    float m_targetDeadZone;
    float m_targetSoftZone;
    float m_targetBiasY;
    float m_targetDamping;
    float m_targetOffset;

    float m_currentDeadZone;
    float m_currentOffset;
    float m_currentSoftZone;
    float m_currentBiasY;
    float m_currentDamping;

    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        m_transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        m_playerRb = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();

        m_currentDeadZone = m_transposer.m_DeadZoneHeight;
        m_currentSoftZone = m_transposer.m_SoftZoneHeight;
        m_currentBiasY = m_transposer.m_BiasY;
        m_currentDamping = m_transposer.m_YDamping;

        m_targetDeadZone = m_currentDeadZone;
        m_targetSoftZone = m_currentSoftZone;
        m_targetBiasY = m_currentBiasY;
        m_targetOffset = m_currentOffset;
        m_targetDamping = m_currentDamping;
    }

    void Update()
    {
        //moves up
        if (m_playerRb.velocity.y > 0.5f)
        {
            m_targetDeadZone = m_upDeadZone;
            m_targetSoftZone = m_upSoftZone;
            m_targetBiasY = m_upBiasY;
            m_targetDamping = m_upDamping;
        }
        //moves down
        else if (m_playerRb.velocity.y < -0.5f)
        {
            m_targetDeadZone = m_downDeadZone;
            m_targetSoftZone = m_downSoftZone;
            m_targetBiasY = m_downBiasY;
            m_targetDamping = m_downDamping;
        }
        //stays horizontal
        else
        {
            m_targetDeadZone = m_stayDeadZone;
            m_targetSoftZone = m_staySoftZone;
            m_targetBiasY = m_stayBiasY;
            m_targetDamping = m_stayDamping;
        }

        //lerp values   
        m_currentDeadZone = Mathf.Lerp(m_currentDeadZone, m_targetDeadZone, Time.deltaTime * m_transitionSpeed);
        m_currentSoftZone = Mathf.Lerp(m_currentSoftZone, m_targetSoftZone, Time.deltaTime * m_transitionSpeed);
        m_currentBiasY = Mathf.Lerp(m_currentBiasY, m_targetBiasY, Time.deltaTime * m_transitionSpeed);
        m_currentDamping = Mathf.Lerp(m_currentDamping, m_targetDamping, Time.deltaTime * m_transitionSpeed);
        m_currentOffset = Mathf.Lerp(m_currentOffset, m_targetOffset, Time.deltaTime * m_transitionSpeed);

        //set values
        m_transposer.m_DeadZoneHeight = m_currentDeadZone;
        m_transposer.m_SoftZoneHeight = m_currentSoftZone;
        m_transposer.m_BiasY = m_currentBiasY;
        m_transposer.m_YDamping = m_currentDamping;
    }
}
