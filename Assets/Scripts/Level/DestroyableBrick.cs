using System.Collections.Generic;
using UnityEngine;

public enum BrickBehaviour
{
    None,
    Timer,
    OnExit,
    OnEnter
}

public class DestroyableBrick : MonoBehaviour
{
    [SerializeField]
    RuntimeAnimatorController[] m_anims;

    BrickBehaviour m_behaviour;

    Animator m_anim;
    List<DestroyableBrick> m_group;

    float m_timer;
    float m_destroyTime = 0.3f;
    Vector3 m_offset = new Vector3(0, -0.501f);
    bool m_destroyed = false;

    readonly int m_HashDestroyed = Animator.StringToHash("Destroyed");

    private void Awake()
    {
        m_anim = GetComponent<Animator>();
    }

    public void SetBrickBehaviour(BrickBehaviour b, int tileNum, List<DestroyableBrick> group)
    {
        m_behaviour = b;
        m_group = group;
        m_anim.runtimeAnimatorController = m_anims[tileNum];
        transform.position += m_offset;

        m_group?.Add(this);
    }

    private void Update()
    {
        if (m_behaviour == BrickBehaviour.Timer && m_timer > 0 && !m_destroyed)
        {
            m_timer -= Time.deltaTime;
            if (m_timer <= 0)
            {
                m_timer = 0;
                DestroyBrick();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (m_behaviour == BrickBehaviour.OnEnter && !m_destroyed)
            {
                DestroyBrick();
            }
            else if (m_behaviour == BrickBehaviour.Timer && !m_destroyed)
            {
                m_timer = m_destroyTime;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (m_behaviour == BrickBehaviour.OnExit && !m_destroyed)
            {
                DestroyBrick();
            }
        }
    }

    void DestroyBrick()
    {
        foreach (var brick in m_group)
        {
            if (!brick.m_destroyed)
            {
                brick.m_destroyed = true;
                brick.m_anim.SetBool(m_HashDestroyed, m_destroyed);
            }
        }
    }

    public void Restart()
    {
        m_timer = 0;
        m_destroyed = false;
        m_anim.SetBool(m_HashDestroyed, m_destroyed);
    }
}
