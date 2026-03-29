using Cinemachine;
using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    [SerializeField]
    CinemachineConfiner2D m_confiner;

    public static CameraBounds Instance { get; private set; }
    float m_cameraPreviousX;
    BoxCollider2D m_collider;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        m_collider = GetComponent<BoxCollider2D>();

        transform.position = Camera.main.transform.position;
        m_cameraPreviousX = Camera.main.transform.position.x;
    }

    void LateUpdate()
    {
        transform.position += (Camera.main.transform.position.x - m_cameraPreviousX) * Vector3.right;
        m_cameraPreviousX = Camera.main.transform.position.x;
    }
    /// <summary>
    /// Sets the camera bounds depending on the chunk's height
    /// </summary>
    /// <param name="pos">chunk's start</param>
    /// <param name="height">chunk's height</param>
    /// <param name="enable">enable bounds</param>
    public void SetHeight(Vector3 pos, int height, bool enable = true)
    {
        m_confiner.enabled = enable;
        transform.position = new Vector3(transform.position.x, pos.y + 1);
        m_collider.offset = new Vector2(0, (height - 1) / 2);
        m_collider.size = new Vector2(m_collider.size.x, height * 3 - 3);
        m_confiner.InvalidateCache();
    }
}
