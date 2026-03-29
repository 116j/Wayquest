using System.Collections.Generic;
using UnityEngine;

//Types of brick destruction
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
    //Brick's group
    List<DestroyableBrick> m_group;

    float m_timer;
    //Brick destroy time
    readonly float m_destroyTime = 0.3f;
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
            //ENTER Destruction Type - destroys a group of bricks when the player steps on one of them
            if (m_behaviour == BrickBehaviour.OnEnter && !m_destroyed)
            {
                DestroyBrick();
            }
            //Destruction Type TIMER - starts the timer when the player steps on a brick, and after it expires, we destroy a group of bricks
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
            //EXIT Destruction Type - destroys a group of bricks when the player leaves one of them
            if (m_behaviour == BrickBehaviour.OnExit && !m_destroyed)
            {
                DestroyBrick();
            }
        }
    }
    /// <summary>
    /// Destroys all the bricks in the group
    /// </summary>
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
    /// <summary>
    /// Resets the timer and returns the brick
    /// </summary>
    public void Restart()
    {
        m_timer = 0;
        m_destroyed = false;
        m_anim.SetBool(m_HashDestroyed, m_destroyed);
    }
}
