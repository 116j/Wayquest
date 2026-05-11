using Cinemachine;
using System.Collections;
using UnityEngine;
using Zenject;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    CinemachineVirtualCamera m_playerCam;
    [SerializeField]
    CameraFollow m_follow;
    [SerializeField]
    CatDetectZone m_catZone;

    Rigidbody2D m_rb;
    PlayerInput m_input;
    BoxCollider2D m_col;
    Damagable m_damagable;
    Animator m_anim;
    TouchingCheck m_touchings;
    SpawnValues m_values;
    SoundController m_sound;
    CinemachineFramingTransposer m_transposer;

    [Inject]
    UIController m_UI;
    [Inject]
    FloatingCanvas m_enemyBar;
    [Inject]
    LevelBuilder m_lvlBuilder;

    //Animation hashes
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashJump = Animator.StringToHash("Jump");
    readonly int m_HashFalling = Animator.StringToHash("Falling");
    readonly int m_HashDoubleJump = Animator.StringToHash("DoubleJump");
    readonly int m_HashDash = Animator.StringToHash("Dash");
    readonly int m_HashDodge = Animator.StringToHash("Dodge");
    readonly int m_HashAnimationTime = Animator.StringToHash("AnimationTime");
    readonly int m_HashAttack = Animator.StringToHash("Attack");
    readonly int m_HashPet = Animator.StringToHash("Pet");
    readonly int m_HashHeavyAttack = Animator.StringToHash("HeavyAttack");
    readonly int m_HashCanMove = Animator.StringToHash("CanMove");
    readonly int m_HashBlocking = Animator.StringToHash("Blocking");

    readonly float m_runSpeed = 7f;
    readonly float m_dashPower = 10;
    readonly float m_jumpPower = 12f;
    readonly float m_fallMultiplier = 3f;
    readonly float m_junpMultiplier = 4f;
    readonly float m_jumpTime = 0.4f;
    readonly float m_blockCooldownTime = 0.4f;
    readonly float m_blockDuration = 0.15f;
    readonly float m_cameraSpeed = 4f;
    readonly float m_dashTime = 0.35f;


    bool m_dead = false;
    bool m_jump = false;
    bool m_attack = false;
    bool m_dash = false;
    bool m_pet = false;
    bool m_canMove = false;
    bool m_canTurn = true;

    bool m_jumping = false;
    bool m_falling = false;
    bool m_canDash = true;
    bool m_canBlock = true;
    bool m_isHit = false;
    bool m_canPet = false;
    bool m_onSlope = false;
    bool m_blocking = false;

    float m_dashCooldownTime = 1.5f;
    float m_dashTimer;
    int m_jumpsMaxCount = 2;
    int m_currentJumps = 0;
    int m_currentDir = 1;
    float m_jumpCounter = 0f;
    float m_dashCooldown = 0f;
    float m_blockCooldown = 0f;
    float m_baseTargetOffset = 2.5f;
    float m_cameraBoundsHeight;

    Vector2 m_gravity;
    float m_gravityScale;

    Vector3 m_fallCheckpoint;
    Vector3 m_rebornCheckpoint;

    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody2D>();
        m_col = GetComponent<BoxCollider2D>();
        m_damagable = GetComponent<Damagable>();
        m_touchings = GetComponent<TouchingCheck>();
        m_sound = GetComponent<SoundController>();
        m_values = GetComponent<SpawnValues>();

        m_gravityScale = m_rb.gravityScale;
        m_gravity = new Vector2(0f, -Physics2D.gravity.y);
        m_transposer = m_playerCam.GetCinemachineComponent<CinemachineFramingTransposer>();

        SetRebornCheckpoint(transform.position);
        SetChunkCheckpoint(transform.position, true);
    }

    void Update()
    {
        m_anim.SetBool(m_HashDie, m_dead);
        if (!m_dead)
        {
            m_isHit = m_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Hit";
            m_canMove = m_anim.GetBool(m_HashCanMove) && !m_damagable.Freezed;
            //If the dash animation is finished - returns gravity, starts falling after the dash in the air
            if (m_canMove && m_dash)
            {
                if (m_jumping)
                    m_falling = true;
                m_rb.gravityScale = m_gravityScale;
                m_dash = m_canDash = false;
            }
            else if (m_dash)
            {
                m_dashTimer += Time.deltaTime;
                if (m_dashTimer >= m_dashTime || m_touchings.IsWalls())
                {
                    m_dash = m_canDash = false;
                    m_canMove = true;
                    m_anim.SetBool(m_HashCanMove, true);
                    m_rb.gravityScale = m_gravityScale;
                }
            }
            //when the jump button is released - starts falling
            if (m_jump && !m_input.Jump)
            {
                m_falling = true;
                m_jump = false;
            }
            //block
            if (m_canBlock && m_canMove && !m_isHit && !m_falling && !m_jumping && m_input.Block)
            {
                m_blocking = true;
            }
            //when the attack animation is finished - returns gravity
            if (m_canMove && m_attack)
            {
                m_attack = false;
                m_rb.gravityScale = m_gravityScale;
                m_anim.ResetTrigger(m_HashAttack);
            }
            //dash
            if (m_input.Dash && m_canDash && m_canMove && !m_isHit)
            {
                m_dashTimer = 0f;
                m_canMove = false;
                m_anim.SetBool(m_HashCanMove, false);
                m_anim.SetTrigger(m_HashDash);
                m_UI.SetDashSprite(0f);
                //removes gravity
                m_rb.gravityScale = 0f;
                m_dash = true;
                //the player moves only horizontally
                m_rb.velocity = new Vector2(m_currentDir * m_runSpeed, 0f);
                m_rb.AddForce(Vector2.right * m_currentDir * m_dashPower, ForceMode2D.Impulse);
            }
            //dodge
            if (m_input.Dodge && m_canMove && !m_isHit)
            {
                m_canMove = false;
                m_anim.SetBool(m_HashCanMove, false);
                m_anim.SetTrigger(m_HashDodge);
                m_rb.velocity = new Vector2(-m_currentDir * m_dashPower, 0f);
            }
            //dash recharge
            if (!m_canDash)
            {
                m_dashCooldown += Time.deltaTime;

                if (m_dashCooldown >= m_dashCooldownTime)
                {
                    m_UI.SetDashSprite(1f);
                    m_canDash = true;
                    m_dashCooldown = 0f;
                }
                else
                {
                    m_UI.SetDashSprite(m_dashCooldown / m_dashCooldownTime);
                }
            }
            //block recharge
            if (!m_canBlock)
            {
                m_blockCooldown += Time.deltaTime;

                if (m_blockCooldown >= m_blockCooldownTime)
                {
                    m_canBlock = true;
                    m_blockCooldown = 0f;
                }
            }
            //light attack
            if (m_input.Attack && !m_isHit && !m_damagable.Freezed)
            {
                //if the player haven't attacked yet - removes gravity and movement
                if (m_canMove)
                {
                    m_canMove = false;
                    m_anim.SetBool(m_HashCanMove, false);
                    m_attack = true;
                    m_rb.velocity = Vector2.zero;
                    m_rb.gravityScale = 0;
                }
                m_anim.SetTrigger(m_HashAttack);
            }
            //heavy attack
            if (m_input.HeavyAttack && m_canMove && !m_isHit && !m_jumping && !m_falling)
            {
                m_canMove = false;
                m_anim.SetBool(m_HashCanMove, false);
                m_attack = true;
                m_rb.velocity = Vector2.zero;
            }
            //if the cat is in the cat's area - pet it
            if (m_canPet && m_input.Pet && !m_damagable.Freezed)
            {
                m_catZone.ApplyPet(true);
                m_pet = true;
                m_input.LockInput(true);
                m_rb.velocity = Vector2.zero;
            }

            if (m_pet && (!m_catZone.TargetDetected || m_onSlope))
            {
                m_pet = false;
                m_input.LockInput(false);
            }

            m_anim.SetBool(m_HashHeavyAttack, m_input.HeavyAttack);
            m_anim.SetFloat(m_HashAnimationTime, Mathf.Repeat(m_anim.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f));
            m_anim.SetFloat(m_HashHorizontal, Mathf.Abs(m_rb.velocity.x));
            m_anim.SetBool(m_HashJump, m_jumping);
            m_anim.SetBool(m_HashFalling, m_falling);
            m_anim.SetBool(m_HashBlocking, m_blocking);
        }
    }

    private void LateUpdate()
    {
        float targetY;
        //camera vertical offset 
        if (!Mathf.Approximately(m_input.MoveCamera, 0))
        {
            targetY = Mathf.Clamp(m_transposer.m_TrackedObjectOffset.y + m_input.MoveCamera * m_cameraSpeed * Time.fixedDeltaTime, -m_cameraBoundsHeight, m_cameraBoundsHeight);
        }
        else
        {
            targetY = Mathf.MoveTowards(m_transposer.m_TrackedObjectOffset.y, m_baseTargetOffset, m_cameraSpeed * Time.fixedDeltaTime);
        }
        m_transposer.m_TrackedObjectOffset = new Vector3(m_transposer.m_TrackedObjectOffset.x, targetY, m_transposer.m_TrackedObjectOffset.z);

    }

    private void FixedUpdate()
    {
        //if the player falls for a long time and does not meet the limits for the fall - returns to the fall checkpoint
        if (m_falling && (m_fallCheckpoint.y - transform.position.y > 70))
        {
            FallReset();
        }

        if (!m_dead)
        {
            //pet the cat
            if (m_pet && !m_catZone.CatMoving)
            {
                //aproaching the cat
                Vector2 dir = m_catZone.TargetLocation - transform.position;
                m_rb.velocity = new Vector2(dir.normalized.x * m_runSpeed, 0f);
                //If has reached it - turns on a petting animation.
                if (Mathf.Abs(dir.x) <= 0.05f)
                {
                    m_pet = false;
                    m_rb.velocity = Vector2.zero;
                    m_input.LockInput(false);
                    m_anim.SetTrigger(m_HashPet);
                }
                return;
            }

            //turn around
            if (m_currentDir * m_input.Move < 0 && m_canMove && m_canTurn)
            {
                m_currentDir *= -1;
                float targetY = m_currentDir == 1 ? 0f : 180f;
                transform.rotation = Quaternion.Euler(0f, targetY, 0f);
                m_follow.Turn(m_currentDir);
            }
            //if touches the walls during the jump or is freezed - stop moving horizontally
            if ((m_touchings.IsWalls() && (m_jumping || m_falling)) || m_damagable.Freezed)
            {
                m_rb.velocity = new Vector2(0f, m_rb.velocity.y);
            }
            //if it's not an attack or a dash - move
            else if (m_canMove && !m_isHit)
            {
                //if goes up a slope - prevents a fall by adding vertical speed up
                if (m_touchings.IsSlopeUp())
                {
                    m_falling = false;
                    m_onSlope = m_rb.isKinematic = true;
                    m_rb.velocity = new Vector2(m_input.Move * m_runSpeed, Mathf.Abs(m_input.Move) * m_runSpeed);
                }
                //if goes down a slope - prevents a fall by adding vertical speed down
                else if (m_touchings.IsSlopeDown())
                {
                    m_falling = false;
                    m_onSlope = m_rb.isKinematic = true;
                    m_rb.velocity = new Vector2(m_input.Move * m_runSpeed, -Mathf.Abs(m_input.Move) * m_runSpeed);
                }
                else
                {
                    if (m_onSlope)
                    {
                        m_onSlope = m_rb.isKinematic = false;
                    }
                    m_rb.velocity = new Vector2(m_input.Move * m_runSpeed, m_rb.velocity.y);
                }
            }
            //if it's not an attack or a dash - jump
            if (m_canMove && m_input.Jump && !m_jump && m_touchings.IsGrounded())
            {
                if (m_onSlope)
                {
                    m_onSlope = m_rb.isKinematic = false;
                }
                m_sound.PlaySound("Jump");
                m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpPower);
                m_jump = true;
                Jump();
                m_currentJumps++;
                return;
            }
            //adds a jump during the jump, if possible
            if (m_canMove && m_jumping && m_input.Jump && !m_jump && m_currentJumps < m_jumpsMaxCount)
            {
                m_sound.PlaySound("Jump");
                m_jump = true;
                m_currentJumps++;
                m_anim.SetTrigger(m_HashDoubleJump);
                m_jumpCounter = 0f;
                m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpPower + 5f);
            }
            //if jumping and moving up - adds vertical speed for m_jumpTime secs or until the jump button is released
            if (m_canMove && !m_onSlope && m_rb.velocity.y > 0f && m_jumping && !m_falling)
            {
                m_jumpCounter += Time.fixedDeltaTime;
                if (m_jumpCounter > m_jumpTime)
                {
                    m_falling = true;
                }
                m_rb.velocity += m_junpMultiplier * Time.fixedDeltaTime * m_gravity;
            }
            //if does not touch the ground and moves downwards - sets the drop and reduces the vertical speed
            if (!m_onSlope && m_rb.velocity.y < 0f && !m_touchings.IsGrounded())
            {
                m_falling = true;
                m_rb.velocity -= m_fallMultiplier * Time.fixedDeltaTime * m_gravity;
            }
            //if the player is in the air and reaches the ground without touching the walls (or touches, but does not move horizontally) - land
            if ((m_falling && !m_touchings.IsSlopeDown() && m_touchings.IsGrounded() && (!m_touchings.IsWalls() || Mathf.Approximately(m_rb.velocity.y, 0f))) ||
                (m_touchings.IsSlopeDown() || m_touchings.IsSlopeUp()) && m_jumping)
            {
                m_sound.PlaySound("Land");
                m_rb.velocity = new Vector2(m_rb.velocity.x, 0f);
                m_falling = m_jumping = false;
                m_currentJumps = 0;
            }

            if (m_rb.velocity != Vector2.zero)
            {
                //If the player is stuck in the ground or wall - move him to the surface
                Vector2 move = new Vector2(m_touchings.WallsStuck(m_rb.velocity.x * Time.fixedDeltaTime), m_touchings.GroundStuck(m_rb.velocity.y * Time.fixedDeltaTime));
                if (move != Vector2.zero)
                {
                    m_rb.MovePosition(m_rb.position + m_rb.velocity * Time.fixedDeltaTime + move);
                }
            }
        }
        //stop moving if is dead
        else if (m_touchings.IsGrounded())
        {
            m_rb.gravityScale = 0f;
            m_rb.velocity = Vector2.zero;
            m_col.enabled = false;
        }

        //If the player is stuck in the ground, lift up
        if (!m_onSlope && m_touchings.IsGroundStuck())
        {
            Debug.Log("Ground stuck");
            FallReset();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("bounds"))
        {
            FallReset();
        }
        else if (collision.gameObject.CompareTag("cat") && m_pet)
        {
            m_pet = false;
            m_rb.velocity = Vector2.zero;
            m_anim.SetTrigger(m_HashPet);
        }
    }
    /// <summary>
    /// Blocks an attack, becoming temporarily invincible
    /// </summary>
    /// <returns></returns>
    IEnumerator Block()
    {
        m_rb.velocity = Vector2.zero;
        m_canMove = false;
        m_anim.SetBool(m_HashCanMove, false);
        m_damagable.Invincible = true;
        yield return new WaitForSeconds(m_blockDuration);
        m_damagable.Invincible = false;
        m_blocking = false;
        m_canMove = true;
        m_anim.SetBool(m_HashCanMove, true);
        m_canBlock = false;
    }
    /// <summary>
    /// Takes damage 
    /// </summary>
    /// <param name="damage">amount of damage</param>
    public void ReceiveDamage(int damage)
    {
        //if 0 - the player is dead
        if (damage == 0)
        {
            m_rb.velocity = Vector2.zero;
            m_dead = !m_dead;
            m_UI.Die(true);
        }
        else if (damage < 0)
        {
            m_blocking = false;
            m_anim.SetTrigger(m_HashHit);
            m_anim.SetBool(m_HashCanMove, false);
            m_isHit = true;
            m_rb.velocity = Vector2.zero;
        }
    }
    /// <summary>
    /// Activates or deactivates the ability to pet a cat
    /// </summary>
    /// <param name="value">is a cat can be petted</param>
    public void EnablePet(bool value)
    {
        m_canPet = value;
    }

    public void Jump()
    {
        m_jumping = true;
        m_jumpCounter = 0f;
        m_falling = false;
    }
    /// <summary>
    /// Stop petting the cat
    /// </summary>
    public void StopPetting()
    {
        m_anim.ResetTrigger(m_HashPet);
        m_catZone.ApplyPet(false);
        m_input.LockInput(false);
    }
    /// <summary>
    /// Sets a new reborn point for the player after death
    /// </summary>
    /// <param name="checkpoint"></param>
    public void SetRebornCheckpoint(Vector3 checkpoint)
    {
        m_rebornCheckpoint = checkpoint + new Vector3(m_values.GetRightBorder(), m_values.GetOffset().y);
    }
    /// <summary>
    /// Sets a new recovery point after the fall
    /// </summary>
    /// <param name="checkpoint">param>
    /// <param name="start">start or the end of the chunk</param>
    public void SetChunkCheckpoint(Vector3 checkpoint, bool start)
    {
        m_fallCheckpoint = checkpoint + new Vector3(start ? m_values.GetRightBorder() : m_values.GetLeftBorder(), m_values.GetOffset().y + 2);
    }
    /// <summary>
    /// After the player falls - returns him to the fall checkpoint after the fall
    /// and restarts the bricks, if necessary
    /// </summary>
    public void FallReset()
    {
        m_lvlBuilder.RestartBricks();
        transform.SetPositionAndRotation(m_fallCheckpoint, Quaternion.identity);
        m_rb.velocity = Vector2.zero;
        m_rb.angularVelocity = 0;
        m_currentDir = 1;
        StartCoroutine(DisableFallInput());
    }

    IEnumerator DisableFallInput()
    {
        m_canTurn = false;
        m_input.LockInput(true);
        yield return new WaitForSeconds(0.3f);
        m_input.LockInput(false);
        m_canTurn = true;
    }
    /// <summary>
    /// Will the camera offset change more up or more down
    /// </summary>
    /// <param name="down"></param>
    public void ChangeCameraTargetOffset(float offset)
    {
        m_baseTargetOffset += offset;
    }

    public void SetCameraBoundsHeight(float height)
    {
        m_cameraBoundsHeight = (height * 3 - 3) / 2.0f;
    }
    /// <summary>
    /// Increases the player's max number of jumps
    /// </summary>
    public void AddJump()
    {
        m_jumpsMaxCount++;
    }
    /// <summary>
    /// Decreases the recovery time of the dash
    /// </summary>
    public void DecreaseDashCooldown()
    {
        m_dashCooldownTime -= 0.5f;
    }
    /// <summary>
    /// Restores the player's health and returns him to the reborn checkpoint, if necessary
    /// </summary>
    /// <param name="reborn">перерождение</param>
    public void Restart(bool reborn = true)
    {
        //if reborn - goes back
        if (reborn)
        {
            m_enemyBar.HideBar();
            transform.SetPositionAndRotation(m_rebornCheckpoint, Quaternion.identity);
            StartCoroutine(DisableFallInput());
            m_currentDir = 1;
        }
        m_damagable.Reborn(true);
        m_dead = !m_dead;
        m_col.enabled = true;
        m_anim.SetBool(m_HashCanMove, true);
        m_rb.gravityScale = m_gravityScale;
    }
}
