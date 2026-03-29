using UnityEngine;
using Zenject;

public class WalkEnemy : MonoBehaviour
{
    [SerializeField]
    float m_turnOffset;
    [SerializeField]
    //Coefficient of the reward value depends on the difficulty of the enemy
    float m_costCoeff;
    [SerializeField]
    float m_walkSpeed = 1.5f;
    [SerializeField]
    bool m_canRun = true;
    [SerializeField]
    float m_runSpeed = 2f;
    [SerializeField]
    protected AttackScript m_attackScript;
    [SerializeField]
    protected DetectZone m_attackZone;
    [SerializeField]
    protected DetectZone m_detectZone;
    [SerializeField]
    protected DetectZone m_groundZone;
    [SerializeField]
    AnimationCurve m_spawnChance;
    [SerializeField]
    Coin m_coin;

    protected Animator m_anim;
    protected Rigidbody2D m_rb;
    protected TouchingCheck m_touchings;
    protected BoxCollider2D m_col;
    protected MovingPlatform m_platform;
    protected Damagable m_damageable;
    protected SpawnValues m_values;
    [Inject]
    protected UIController m_UI;
    [Inject]
    protected LevelBuilder m_lvlBuilder;
    [Inject]
    protected ShopLayout m_shop;
    [Inject]
    DiContainer m_container;

    //Animation hashes
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashCanMove = Animator.StringToHash("CanMove");
    readonly int m_HashParry = Animator.StringToHash("Parry");
    protected readonly int m_HashAttackNum = Animator.StringToHash("AttackNum");
    protected readonly int m_HashAttack = Animator.StringToHash("Attack");

    protected bool m_dead = false;
    protected bool m_waiting = false;
    protected bool m_canMove = true;
    bool m_coinsSpawned = false;

    readonly float m_waitTime = 3f;
    //Number of attack types
    int m_attackCount = 3;
    //Reward for killing an enemy
    int m_cost;

    protected Vector3 m_startPos;
    protected int m_currentDir = 1;
    protected float m_speed = 0f;
    float m_waitTimer;

    /// <summary>
    /// Sets the number of enemy attacks
    /// </summary>
    /// <param name="count"></param>
    public void SetAttacksCount(int count)
    {
        m_attackCount = count;
    }

    protected virtual void Start()
    {
        m_anim = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody2D>();
        m_touchings = GetComponent<TouchingCheck>();
        m_col = GetComponent<BoxCollider2D>();
        m_damageable = GetComponent<Damagable>();
        m_values = GetComponent<SpawnValues>();

        m_startPos = transform.position;
        //The reward for killing depends on the max number of chunks and the difficulty of the enemy
        m_cost = Mathf.CeilToInt(Mathf.Min(m_shop.AllPrices * m_costCoeff / (m_lvlBuilder.GetLevelChunksCount() * m_lvlBuilder.GetEnemySpawnChance()), 1000f));
    }

    protected virtual void Update()
    {
        m_anim.SetBool(m_HashDie, m_dead);
        m_anim.SetFloat(m_HashHorizontal, m_speed);
        m_anim.SetBool(m_HashParry, m_damageable.Freezed);
    }

    protected virtual void FixedUpdate()
    {
        if (!m_dead)
        {
            m_canMove = m_anim.GetBool(m_HashCanMove) && !m_damageable.Freezed;
            if (m_damageable.Freezed)
            {
                m_rb.velocity = Vector2.zero;
            }
            //if the player is in the attack zone - enables the attack and stops moving
            if (m_attackZone.TargetDetected)
            {
                if (!m_attackScript.EnableAttack)
                {
                    m_attackScript.EnableAttack = true;
                    m_speed = 0f;
                    m_rb.velocity = Vector2.zero;
                    m_waiting = false;
                }
                m_anim.SetInteger(m_HashAttackNum, Random.Range(0, m_attackCount));
                return;
            }
            //if the player is in the detection zone, the enemy can go further and can move - chase after the player
            else if (m_detectZone.TargetDetected && m_groundZone.TargetDetected && !m_touchings.IsWalls() && m_canMove)
            {
                Chase();
            }
            //if the player is out of the zone or the enemy cannot go further - patrol
            else if (m_canMove)
            {
                Potrol();
            }

            m_rb.velocity = (m_canMove ? 1 : 0) * m_currentDir * m_speed * Vector2.right;
        }
    }
    /// <summary>
    /// Patrolling the area back and forth
    /// </summary>
    protected void Potrol()
    {
        //disables an attack
        m_attackScript.EnableAttack = false;

        if (!m_waiting)
        {
            m_speed = m_walkSpeed;
            //if there is no land or wall further away - stops and waits
            if (!m_groundZone.TargetDetected || m_touchings.IsWalls())
            {
                m_speed = 0f;
                m_waitTimer = 0f;
                m_waiting = true;
            }
        }
        else
        {
            m_waitTimer += Time.deltaTime;
            //if the waiting time is over - turns around and moves on
            if (m_waitTimer >= m_waitTime)
            {
                TurnAround();
                m_waiting = false;
            }
        }
    }
    /// <summary>
    /// Chases the player
    /// </summary>
    protected void Chase()
    {
        //if the player is from behind - turns around
        if ((m_detectZone.TargetLocation.x - transform.position.x) * m_currentDir < 0f)
        {
            TurnAround();
        }
        //enables an attack
        m_attackScript.EnableAttack = false;
        m_waiting = false;
        //if the player is close - walks, far away - runs
        m_speed = m_canRun && GetDistance() > m_col.size.x ? Mathf.Lerp(m_speed, m_runSpeed, 0.5f) : Mathf.Lerp(m_speed, m_walkSpeed, 0.5f);
    }
    /// <summary>
    /// Distance between the player and the start of the attack zone
    /// </summary>
    /// <returns></returns>
    protected float GetDistance()
    {
        return m_currentDir == 1 ?
                (m_detectZone.TargetLocation.x - m_attackZone.RightBorder.x) :
                (m_attackZone.LeftBorder.x - m_detectZone.TargetLocation.x);
    }

    virtual protected void TurnAround()
    {
        m_currentDir *= -1;
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + m_currentDir * 180f, 0f);
        transform.position += Vector3.right * m_currentDir * m_turnOffset;
    }
    /// <summary>
    /// Takes damage
    /// </summary>
    /// <param name="damage"></param>
    public virtual void ReceiveDamage(int damage)
    {
        //stops
        m_rb.velocity = Vector2.zero;
        m_speed = 0f;
        //if the damage is 0 - the enemy is dead
        if (damage == 0)
        {
            m_dead = true;
            m_col.isTrigger = true;
            //if a platform was attached to the enemy - starts its movement
            if (m_platform != null)
            {
                m_platform.StartMovement();
            }
            //creates coins as a reward for killing
            if (!m_coinsSpawned && m_coin != null)
            {
                m_coinsSpawned = true;
                int coins = Random.Range(3, 7);
                for (int i = 0; i < coins; i++)
                {
                    m_container.InstantiatePrefabForComponent<Coin>(m_coin, transform.position, Quaternion.identity, null).SetCost(m_cost / coins);
                }
            }
        }
        else if (damage < 0)
        {
            if ((m_detectZone.TargetLocation.x - transform.position.x) * m_currentDir < 0f)
            {
                TurnAround();
            }
            m_anim.SetTrigger(m_HashHit);
        }
    }
    /// <summary>
    /// Attachs the platform to the enemy
    /// After the enemy's death, the platform will start moving
    /// </summary>
    /// <param name="platform"></param>
    public void ConnectPlatform(MovingPlatform platform)
    {
        m_platform = platform;
        platform.DisableAutoMovement();
    }
    /// <summary>
    /// Chance of a specific type of enemy appearing, depending on the number of chunks created
    /// </summary>
    /// <returns></returns>
    public float GetSpawnChance()
    {
        return m_spawnChance.Evaluate(m_lvlBuilder.LevelProgress());
    }
    /// <summary>
    /// Revives the enemy
    /// </summary>
    public virtual void Reset()
    {
        transform.SetPositionAndRotation(m_startPos + m_values.GetOffset(), Quaternion.identity);
        m_currentDir = 1;
        m_col.isTrigger = false;
        m_dead = false;
        m_damageable.Reborn();
        m_platform?.Restart(true);
    }
}
