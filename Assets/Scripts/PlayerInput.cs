using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PlayerInput : MonoBehaviour
{
    float m_move;
    float m_moveCamera;
    bool m_attack;
    bool m_heavyAttack;
    bool m_jump;
    bool m_dash;
    bool m_dodge;
    bool m_block;
    bool m_pet;
    bool m_shop;
    bool m_pause;

    public float Move => m_inputLocked ? 0f : m_move;
    public float MoveCamera => m_inputLocked ? 0f : m_moveCamera;
    public bool Jump => !m_inputLocked && m_jump;
    public bool Dash
    {
        get
        {
            if (m_dash)
            {
                m_dash = false;
                return !m_inputLocked && !m_dash;
            }
            else
                return false;
        }
    }
    public bool Dodge
    {
        get
        {
            if (m_dodge)
            {
                m_dodge = false;
                return !m_inputLocked && !m_dodge;
            }
            else
                return false;
        }
    }
    public bool Block
    {
        get
        {
            if (m_block)
            {
                m_block = false;
                return !m_inputLocked && !m_block;
            }
            else
                return false;
        }
    }
    public bool Attack
    {
        get
        {
            if (m_attack)
            {
                m_attack = false;
                return !m_inputLocked && !m_attack;
            }
            else
                return false;
        }
    }
    public bool Pet
    {
        get
        {
            if (m_pet)
            {
                m_pet = false;
                return !m_inputLocked && !m_pet;
            }
            else
                return false;
        }
    }

    public bool HeavyAttack => !m_inputLocked && m_heavyAttack;

    bool m_inputLocked = true;
    bool m_gameStarted = false;
    bool m_shopOpened = false;

    UnityEngine.InputSystem.PlayerInput m_input;

    [Inject]
    UIController m_UI;
    [Inject]
    Menu m_menu;

    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        m_input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        m_input.uiInputModule.cancel.action.performed += m_menu.CloseLayout;
    }
    /// <summary>
    /// Блокирует и разблокитрует ввод
    /// </summary>
    /// <param name="toLock">блокировать</param>
    public void LockInput(bool toLock)
    {
        m_inputLocked = toLock;
    }
    /// <summary>
    /// Тип устройства ввода - клавиатура или джойстик
    /// </summary>
    /// <returns>название типа</returns>
    public string GetCurrentDeviceType() => m_input.currentControlScheme;
    /// <summary>
    /// Включает и выключает возможность взаимодействия с магазином
    /// </summary>
    public void EnableShop(bool enable)
    {
        m_shop = enable;
    }

    public void StartGame()
    {
        m_gameStarted = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    /// <summary>
    /// Ставит и снимает с паузы игру
    /// </summary>
    public void OnPause()
    {
        if (m_gameStarted && !m_shopOpened)
        {
            Cursor.lockState = m_pause ? CursorLockMode.Locked : CursorLockMode.None;
            m_pause = !m_pause;
            Cursor.visible = m_pause;
            Time.timeScale = m_pause ? 0 : 1;
            LockInput(m_pause);
            m_menu.Pause(m_pause);
        }
    }

    void OnMove(InputValue value)
    {
        m_move = value.Get<Vector2>().x;
    }

    void OnMoveCamera(InputValue value)
    {
        m_moveCamera = value.Get<Vector2>().y;
    }

    void OnAttack(InputValue value)
    {
        m_attack = value.isPressed;
    }

    void OnHeavyAttack(InputValue value)
    {
        m_heavyAttack = value.isPressed;
    }

    void OnJump(InputValue value)
    {
        m_jump = value.isPressed;
    }

    void OnDash(InputValue value)
    {
        m_dash = value.isPressed;
    }

    void OnDodge(InputValue value)
    {
        m_dodge = value.isPressed;
    }

    void OnBlock(InputValue value)
    {
        m_block = value.isPressed;
    }

    void OnPet(InputValue value)
    {
        m_pet = value.isPressed;
    }
    /// <summary>
    /// Открывает меню магазина
    /// </summary>
    void OnShop()
    {
        if (m_shop)
        {
            m_shopOpened = !m_shopOpened;
            LockInput(m_shopOpened);
            Cursor.visible = m_shopOpened;
            Cursor.lockState = m_shopOpened? CursorLockMode.None:CursorLockMode.Locked;
            m_UI.OpenShop();
        }
    }
}
