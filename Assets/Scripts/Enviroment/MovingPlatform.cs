using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField]
    float m_speed = 4f;
    [SerializeField]
    float m_waitTime = 0.5f;

    List<Vector3> m_checkpoints = new List<Vector3>();
    //Список сигналов останововк на точках (true - остановиться)
    List<bool> m_stops = new List<bool>();
    int m_currentCheckpoint;
    float m_waitTimer;

    bool m_waiting = false;
    //Началось ли движение
    bool m_start = true;
    //Начать движение только при соприкосновении с игроком
    bool m_moveWnenStand = false;

    private void Start()
    {
        m_checkpoints.Add(transform.position);
        m_stops.Add(true);
    }

    void Update()
    {
        //ожидание перед дальнейшим движением
        if (m_waiting)
        {
            m_waitTimer += Time.deltaTime;
            if (m_waitTimer >= m_waitTime)
            {
                m_waitTimer = 0f;
                m_waiting = false;
            }
        }
        else if (m_start)
        {
            //движется к следующей точке по кругу 
            transform.position = Vector3.MoveTowards(transform.position, m_checkpoints[m_currentCheckpoint], m_speed * Time.deltaTime);

            if (Vector3.Distance(m_checkpoints[m_currentCheckpoint], transform.position) < 0.02f)
            {
                m_waiting = m_stops[m_currentCheckpoint];
                m_currentCheckpoint = (m_currentCheckpoint + 1) % m_checkpoints.Count;
            }
        }
    }
    //При соприкосновении с игрком начинает движение (если m_moveWnenStand)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (m_moveWnenStand)
            {
                m_start = true;
            }
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }

    public void SetWaitTime(float time)
    {
        m_waitTime = time;
    }

    public float GetWaitTime() => m_waitTime;

    public void SetSpeed(float speed)
    {
        m_speed = speed;
    }

    public float GetSpeed() => m_speed;
    /// <summary>
    /// Платформа начнет двигаться, когда на нее наступит игрок
    /// </summary>
    public void DisableAutoMovement()
    {
        m_moveWnenStand = true;
        m_start = false;
    }

    public void StartMovement()
    {
        m_moveWnenStand = false;
        m_start = true;
    }

    public void AddCheckpoint(Vector3 pos, bool stop = true)
    {
        m_checkpoints.Add(pos);
        m_stops.Add(stop);
    }
    /// <summary>
    /// Возвращает платформу на начальную позицию
    /// </summary>
    /// <param name="enemy">нужно ли ждать убийства врага</param>
    public void Restart(bool enemy = false)
    {
        if (m_moveWnenStand || enemy)
        {
            m_start = false;
            m_waiting = false;
            m_currentCheckpoint = 0;
            transform.position = m_checkpoints[m_checkpoints.Count - 1];
        }
    }
}
