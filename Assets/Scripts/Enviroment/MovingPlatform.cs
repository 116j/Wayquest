using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField]
    float m_speed = 4f;
    [SerializeField]
    float m_waitTime = 0.5f;

    List<Vector3> m_checkpoints = new List<Vector3>();
    //List of stop signals at checkpoints (true - stop)
    List<bool> m_stops = new List<bool>();
    int m_currentCheckpoint;
    float m_waitTimer;

    bool m_waiting = false;
    //If the movement has started
    bool m_start = true;
    //Starts the movement only when the player collides
    bool m_moveWnenStand = false;

    private void Start()
    {
        m_checkpoints.Add(transform.position);
        m_stops.Add(true);
    }

    void Update()
    {
        //waiting before moving on
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
            //moves to the next point in a circle 
            transform.position = Vector3.MoveTowards(transform.position, m_checkpoints[m_currentCheckpoint], m_speed * Time.deltaTime);

            if (Vector3.Distance(m_checkpoints[m_currentCheckpoint], transform.position) < 0.02f)
            {
                m_waiting = m_stops[m_currentCheckpoint];
                m_currentCheckpoint = (m_currentCheckpoint + 1) % m_checkpoints.Count;
            }
        }
    }
    //When it collides with the player, it starts moving (if m_moveWnenStand)
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
    /// The platform will start moving when the player steps on it
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
    /// Returns the platform to its initial position
    /// </summary>
    /// <param name="enemy">if an enemy should be killed first</param>
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
