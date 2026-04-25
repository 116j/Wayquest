using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class Cat : MonoBehaviour
{
    [SerializeField]
    DetectZone m_groundZone;

    public Transform PetPlayerLocation;
    public bool CanPet { get; private set; } = true;
    public bool IsMoving => m_walking;

    Animator m_anim;
    TouchingCheck m_touchings;
    Rigidbody2D m_rb;
    SoundController m_soundController;

    [Inject]
    LevelBuilder m_lvlBuilder;

    readonly UnityEvent<int> m_addHeart = new();

    readonly int m_HashWalk = Animator.StringToHash("Walk");
    readonly int m_HashSleep = Animator.StringToHash("Sleep");
    readonly int m_HashCanMove = Animator.StringToHash("CanMove");
    readonly int[] m_triggers = new int[]
    {
      Animator.StringToHash("Turn"),
      Animator.StringToHash("Jump"),
      Animator.StringToHash("CleanFace"),
      Animator.StringToHash("Lick")
    };

    readonly float m_walkRecoverTimeMax = 10f;
    readonly float m_walkRecoverTimeMin = 5f;
    readonly float m_triggerRecoverTime = 2f;
    readonly float m_walkSpeed = 2f;

    bool m_petting = false;
    bool m_walking = false;
    bool m_triggered = false;

    float m_walkTimer;
    float m_triggerTimer;
    int m_currentDir = 1;
    float m_speed;
    float m_walkTime;

    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_touchings = GetComponent<TouchingCheck>();
        m_soundController = GetComponent<SoundController>();
        m_rb = GetComponent<Rigidbody2D>();
        m_walkTime = Random.Range(m_walkRecoverTimeMin, m_walkRecoverTimeMax);
        m_addHeart.AddListener(GameObject.FindGameObjectWithTag("Player").GetComponent<Damagable>().ApplyHealth);
        m_addHeart.AddListener(m_lvlBuilder.CatPetted);
    }

    void Update()
    {
        //stops and turns around if there is no way ahead
        if (!m_groundZone.TargetDetected || m_touchings.IsWalls())
        {
            m_walkTimer = 0f;
            m_walking = false;
            m_speed = 0f;
            TurnAround();
        }
        //if is not petted - walks
        if (!m_petting && CanPet)
            Walk();
        m_anim.SetBool(m_HashWalk, m_walking && !m_petting);
        //if hasn't a trigger - sets
        if (!m_petting && !m_triggered)
        {
            m_anim.SetTrigger(m_triggers[Random.Range(0, m_triggers.Length)]);
            m_triggered = true;
        }
        //if has a trigger - recharge
        if (m_triggered)
        {
            m_triggerTimer += Time.deltaTime;
            if (m_triggerTimer >= m_triggerRecoverTime)
            {
                m_triggerTimer = 0f;
                m_triggered = false;
            }
        }
    }

    private void FixedUpdate()
    {
        m_rb.velocity = transform.right * (m_anim.GetBool(m_HashCanMove) ? m_speed : 0f);
    }

    /// <summary>
    /// Walks during the walk time, then stops
    /// </summary>
    void Walk()
    {
        m_walkTimer += Time.deltaTime;
        if (m_walkTimer >= m_walkTime)
        {
            m_walkTime = Random.Range(m_walkRecoverTimeMin, m_walkRecoverTimeMax);
            m_walking = !m_walking;
            m_walkTimer = 0;
            m_speed = m_walking ? m_walkSpeed : 0f;
        }
    }

    public void TurnAround()
    {
        m_currentDir *= -1;
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + m_currentDir * 180f, 0f);
    }

    public void Stop(bool stop)
    {
        m_walking = !stop;
        m_speed = m_walking ? m_walkSpeed : 0f;
        m_anim.SetBool(m_HashCanMove, !stop);
    }
    /// <summary>
    /// Signals the beginning and end of peting
    /// </summary>
    /// <param name="pet">is the petting ended</param>
    public void Pet(bool pet)
    {
        if (pet)
        {
            m_petting = true;
        }
        else
        {
            m_addHeart.Invoke(1);
            m_anim.SetBool(m_HashSleep, true);
            CanPet = false;
        }
    }

    public void Reset()
    {
        m_soundController.StopSound();
        m_anim.SetBool(m_HashSleep, false);
        CanPet = true;
        m_petting = false;
    }

    private void OnDestroy()
    {
        if (!m_anim.GetBool(m_HashSleep))
        {
            m_lvlBuilder.CatPetted(1);
        }
    }
}
