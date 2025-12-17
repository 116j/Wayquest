using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public class Menu : MonoBehaviour
{
    [SerializeField]
    Button m_playButton;
    [SerializeField]
    Button m_resumeButton;
    [SerializeField]
    Button m_displayButton;
    [SerializeField]
    GameObject m_maimMenuTitle;

    [Inject]
    PlayerInput m_input;
    [Inject]
    UIController m_UI;

    Animator m_pauseLayoutAnim;
    //Затемнение экрана
    Image m_backgroundTint;
    //Выбранный UI компонент
    GameObject m_selected;

    void Awake()
    {
        m_pauseLayoutAnim = GetComponent<Animator>();
        m_backgroundTint = GetComponent<Image>();

        //начальные статы меню игры
        Time.timeScale = 1;
        m_UI.SetStats(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    /// <summary>
    /// Открывает или скрывает меню паузы
    /// </summary>
    /// <param name="show">true - открыть</param>
    public void Pause(bool show)
    {
        m_backgroundTint.enabled = show;
        if (show)
        {
            m_maimMenuTitle.SetActive(false);
            m_pauseLayoutAnim.SetBool("OpenOptions", true);
        }
        else
        {
            m_pauseLayoutAnim.SetBool("Close", true);
        }
    }

    public void Resume()
    {
        m_input.OnPause();
    }
    /// <summary>
    /// Перезагружает с цену на начальное меню
    /// </summary>
    public void MainMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Settings()
    {
        m_pauseLayoutAnim.ResetTrigger("CloseLayout");
        m_pauseLayoutAnim.SetBool("OpenSettings", true);
    }

    public void Back()
    {
        m_pauseLayoutAnim?.SetTrigger("CloseSettings");
    }

    public void SettingsLayout()
    {
        m_pauseLayoutAnim.ResetTrigger("CloseLayout");
        m_pauseLayoutAnim.SetTrigger("OpenSettingsLayout");
    }

    public void CloseLayout(InputAction.CallbackContext ctx)
    {
        if (m_pauseLayoutAnim)
            m_pauseLayoutAnim.SetTrigger("CloseLayout");
    }
    /// <summary>
    /// Закрывает меню и начинает игру
    /// </summary>
    public void Play()
    {
        m_input.StartGame();
        m_input.LockInput(false);
        m_pauseLayoutAnim.SetBool("Close", true);
        m_UI.SetStats(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
				Application.Quit();
#endif
    }

    public void SelectPlay()
    {
        m_playButton.Select();
    }

    public void SelectDisplay()
    {
        m_displayButton.Select();
    }

    public void SelectResume()
    {
        m_resumeButton.Select();
    }
    //Возвращает фокус выбранному ранее эоементу UI
    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            EventSystem.current.SetSelectedGameObject(m_selected);
        }
        else
        {
            m_selected = EventSystem.current.currentSelectedGameObject;
        }
    }
}
