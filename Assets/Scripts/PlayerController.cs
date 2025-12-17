using Cinemachine;
using System.Collections;
using UnityEngine;
using Zenject;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    CinemachineVirtualCamera m_playerCam;
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

    //Хеши анимаций

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
    readonly float m_cameraSpeed = 6f;
    readonly float m_dashTime = 0.35f;


    bool m_dead = false;
    bool m_jump = false;
    bool m_attack = false;
    bool m_dash = false;
    bool m_pet = false;
    bool m_canMove = false;

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
    float m_baseTransposer = 2.5f;
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
            //если анимация рывка закончена - вернуть гравитацию, начать падать после рывка в воздухе
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
                if (m_dashTimer >= m_dashTime)
                {
                    m_dash = m_canDash = false;
                    m_canMove = true;
                    m_anim.SetBool(m_HashCanMove, true);
                    m_rb.gravityScale = m_gravityScale;
                }
            }
            //когда отпустили кнопку прыжка - начать падать
            if (m_jump && !m_input.Jump)
            {
                m_falling = true;
                m_jump = false;
            }
            //блок
            if (m_canBlock && m_canMove && !m_isHit && !m_falling && !m_jumping && m_input.Block)
            {
                m_blocking = true;
            }
            //когда анимация атаки закончена - вернуть гравитацию
            if (m_canMove && m_attack)
            {
                m_attack = false;
                m_rb.gravityScale = m_gravityScale;
                m_anim.ResetTrigger(m_HashAttack);
            }
            //рывок
            if (m_input.Dash && m_canDash && m_canMove && !m_isHit)
            {
                m_dashTimer = 0f;
                m_canMove = false;
                m_anim.SetBool(m_HashCanMove, false);
                m_anim.SetTrigger(m_HashDash);
                m_UI.SetDashSprite(0f);
                //убрать гравитацию
                m_rb.gravityScale = 0f;
                m_dash = true;
                //игрок двигается горизонтально
                m_rb.velocity = new Vector2(m_currentDir * m_runSpeed, 0f);
                m_rb.AddForce(Vector2.right * m_currentDir * m_dashPower, ForceMode2D.Impulse);
            }
            //уклонение
            if (m_input.Dodge && m_canMove && !m_isHit)
            {
                m_canMove = false;
                m_anim.SetBool(m_HashCanMove, false);
                m_anim.SetTrigger(m_HashDodge);
                m_rb.velocity = new Vector2(-m_currentDir * m_dashPower, 0f);
            }
            //перезарядка рывка
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
            //перезарядка блока
            if (!m_canBlock)
            {
                m_blockCooldown += Time.deltaTime;

                if (m_blockCooldown >= m_blockCooldownTime)
                {
                    m_canBlock = true;
                    m_blockCooldown = 0f;
                }
            }
            //легкая атака
            if (m_input.Attack && !m_isHit && !m_damagable.Freezed)
            {
                //если еще не атаковал - убирать гравитацию и движение
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
            //тяжелая атака
            if (m_input.HeavyAttack && m_canMove && !m_isHit && !m_jumping && !m_falling)
            {
                m_canMove = false;
                m_anim.SetBool(m_HashCanMove, false);
                m_attack = true;
                m_rb.velocity = Vector2.zero;
            }
            //если кот в зоне кота - погладить
            if (m_canPet && m_input.Pet && !m_damagable.Freezed)
            {
                m_catZone.ApplyPet(true);
                m_pet = true;
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
        //вертикальный отступ камеры 
        if (!Mathf.Approximately(m_input.MoveCamera, 0))
        {
            targetY = Mathf.Clamp(m_transposer.m_TrackedObjectOffset.y + m_input.MoveCamera * m_cameraSpeed * Time.fixedDeltaTime, -m_cameraBoundsHeight, m_cameraBoundsHeight);
        }
        else
        {
            targetY = Mathf.MoveTowards(m_transposer.m_TrackedObjectOffset.y, m_baseTransposer, m_cameraSpeed * Time.fixedDeltaTime);
        }
        m_transposer.m_TrackedObjectOffset = new Vector3(m_transposer.m_TrackedObjectOffset.x, targetY, m_transposer.m_TrackedObjectOffset.z);

    }

    private void FixedUpdate()
    {
        //если игрок падает долго и не встретил границ для падения - вернуть
        if (m_falling && (m_fallCheckpoint.y - transform.position.y > 100))
        {
            FallReset();
        }
        //если игрок застрял в земле - поднять вверх
        if (m_touchings.IsGroundStuck())
        {
            Debug.Log("Ground stuck");
            transform.position += Vector3.up * 0.6f;
        }

        if (!m_dead)
        {
            //погладить кота
            if (m_pet)
            {
                //приближается к коту
                Vector2 dir = m_catZone.TargetLocation - transform.position;
                m_rb.velocity = new Vector2(dir.normalized.x * m_runSpeed, 0f);
                //если дошел - анимация поглаживания
                if (Mathf.Abs(dir.x) <= 0.01f)
                {
                    m_pet = false;
                    m_rb.velocity = Vector2.zero;
                    m_anim.SetTrigger(m_HashPet);
                }
                return;
            }

            //повернуться
            if (m_currentDir * m_input.Move < 0 && m_canMove)
            {
                m_currentDir *= -1;
                transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + m_currentDir * 180f, 0f);
            }
            //если касается стен во время прыжка - не двигаться горизонтально
            if (m_touchings.IsWalls() && (m_jumping || m_falling))
            {
                m_rb.velocity = new Vector2(0f, m_rb.velocity.y);
            }
            //если не атака или рывок - двигаться
            else if (m_canMove && !m_isHit)
            {
                //если поднимается по холму - предотвратить падение, добавить вертикальную скорость вверх
                if (m_touchings.IsSlopeUp())
                {
                    m_falling = false;
                    m_onSlope = m_rb.isKinematic = true;
                    m_rb.velocity = new Vector2(m_input.Move * m_runSpeed, Mathf.Abs(m_input.Move) * m_runSpeed);
                }
                //если спускается с холма - предотвратить падение, добавить вертикальную скорость вниз
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
            //если не атака или рывок - прыжок
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
            //добавить прыжок во время прыдка, если возможно
            if (m_canMove && m_jumping && m_input.Jump && !m_jump && m_currentJumps < m_jumpsMaxCount)
            {
                m_sound.PlaySound("Jump");
                m_jump = true;
                m_currentJumps++;
                m_anim.SetTrigger(m_HashDoubleJump);
                m_jumpCounter = 0f;
                m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpPower + 5f);
            }
            //если в прыжке и двигается вверх - добавить вертикальную скорость на m_jumpTime сек или пока не отпустит кнопку прыжка
            if (m_canMove && !m_onSlope && m_rb.velocity.y > 0f && m_jumping && !m_falling)
            {
                m_jumpCounter += Time.fixedDeltaTime;
                if (m_jumpCounter > m_jumpTime)
                {
                    m_falling = true;
                }
                m_rb.velocity += m_junpMultiplier * Time.fixedDeltaTime * m_gravity;
            }
            //если не касается земли и движется вниз - установить падение и убавить вертикальную скорость
            if (!m_onSlope && m_rb.velocity.y < 0f && !m_touchings.IsGrounded())
            {
                m_falling = true;
                m_rb.velocity -= m_fallMultiplier * Time.fixedDeltaTime * m_gravity;
            }
            //если игрок в воздухе и достигает земли, не касаясь стен (или касается, но не двигается горизонтально) - приземлиться
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
                //если игрок застрял в земле или стене - пододвинуть его к поверхности
                Vector2 move = new Vector2(m_touchings.WallsStuck(m_rb.velocity.x * Time.fixedDeltaTime), m_touchings.GroundStuck(m_rb.velocity.y * Time.fixedDeltaTime));
                if (move != Vector2.zero)
                {
                    m_rb.MovePosition(m_rb.position + m_rb.velocity * Time.fixedDeltaTime + move);
                }
            }
        }
        //перестать двигаться, если умер
        else if (m_touchings.IsGrounded())
        {
            m_rb.gravityScale = 0f;
            m_rb.velocity = Vector2.zero;
            m_col.enabled = false;
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
    /// Блокирует удар, становлясь на время неуязвимым
    /// </summary>
    /// <returns></returns>
    IEnumerator Block()
    {
        m_rb.velocity = Vector2.zero;
        m_canMove = false;
        m_anim.SetBool(m_HashCanMove, false);
        m_damagable.Invinsible = true;
        yield return new WaitForSeconds(m_blockDuration);
        m_damagable.Invinsible = false;
        m_blocking = false;
        m_canMove = true;
        m_anim.SetBool(m_HashCanMove, true);
        m_canBlock = false;
    }
    /// <summary>
    /// Получает урон 
    /// </summary>
    /// <param name="damage">количество урона</param>
    public void ReceiveDamage(int damage)
    {
        //если 0 - игрок умер
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
    /// Активирует или дективирует возможность погладить кота
    /// </summary>
    /// <param name="value">можно ли гладить</param>
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
    /// Прекратить поглаживание кота
    /// </summary>
    public void StopPetting()
    {
        m_anim.ResetTrigger(m_HashPet);
        m_catZone.ApplyPet(false);
    }
    /// <summary>
    /// Ставит новую точку возрождения игрока после смерти
    /// </summary>
    /// <param name="checkpoint">новая точка возрождения</param>
    public void SetRebornCheckpoint(Vector3 checkpoint)
    {
        m_rebornCheckpoint = checkpoint + new Vector3(m_values.GetRightBorder(), m_values.GetOffset().y);
    }
    /// <summary>
    /// Ставит новую точку восстановления после падения
    /// </summary>
    /// <param name="checkpoint">новая точка</param>
    /// <param name="start">начало или конец чанка</param>
    public void SetChunkCheckpoint(Vector3 checkpoint, bool start)
    {
        m_fallCheckpoint = checkpoint + new Vector3(start ? m_values.GetRightBorder() : m_values.GetLeftBorder(), m_values.GetOffset().y + 2);
    }
    /// <summary>
    /// После падения игрока возвращает его в точку восстановления после падения
    /// и восстанавливает кирпичи, если нужно
    /// </summary>
    public void FallReset()
    {
        m_lvlBuilder.RestartBricks();
        transform.SetPositionAndRotation(m_fallCheckpoint, Quaternion.identity);
        m_rb.velocity = Vector2.zero;
        m_currentDir = 1;
    }
    /// <summary>
    /// Менят отступ камеры больше вверх или больше вниз
    /// </summary>
    /// <param name="down">вниз</param>
    public void ChangeTransposerHeight(bool down)
    {
        m_baseTransposer += 3f * (down ? -1 : 1);
    }

    public void SetCameraBoundsHeight(float height)
    {
        m_cameraBoundsHeight = (height * 3 - 3) / 2.0f;
    }
    /// <summary>
    /// Увеличивает макс количество прыжков игрока
    /// </summary>
    public void AddJump()
    {
        m_jumpsMaxCount++;
    }
    /// <summary>
    /// Уменьмает время восстановления рывка
    /// </summary>
    public void DecreaseDashCooldown()
    {
        m_dashCooldownTime -= 0.5f;
    }
    /// <summary>
    /// Восстанавливает здоровье игрока и возвращает егов точку перерождения, если нужно
    /// </summary>
    /// <param name="reborn">перерождение</param>
    public void Restart(bool reborn = true)
    {
        //если переродился - вернуться назад
        if (reborn)
        {
            m_enemyBar.HideBar();
            transform.SetPositionAndRotation(m_rebornCheckpoint, Quaternion.identity);
            m_currentDir = 1;
        }
        m_damagable.Reborn(true);
        m_dead = !m_dead;
        m_col.enabled = true;
        m_rb.gravityScale = m_gravityScale;
    }
}
