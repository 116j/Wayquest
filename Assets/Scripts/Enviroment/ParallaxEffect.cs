using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [SerializeField]
    //Переменная для определения отдаленности объекта от игрока
    float m_parallaxMultiplier;
    //Ширина картинки заднего фона
    readonly float m_textureUnitSizeX = 19.615f;

    Transform m_cam;

    Vector3 m_lastCameraPosition;
    Vector3 m_deltaCamMove;

    void Start()
    {
        m_cam = Camera.main.transform;  
        m_lastCameraPosition = m_cam.position;
    }

    void Update()
    {
        m_deltaCamMove = m_cam.position - m_lastCameraPosition;
        //сдвигает объект влево относительно
        transform.position += m_deltaCamMove.x * m_parallaxMultiplier * Vector3.left;
        m_lastCameraPosition = m_cam.position;
        //если сдвинуто больше, чем на ширину картинки - возвращает объект на минимальное расстояние от камеры
        if (Mathf.Abs(m_cam.position.x - transform.position.x) >= m_textureUnitSizeX)
        {
            float offsetX = (m_cam.position.x - transform.position.x) % m_textureUnitSizeX;
            transform.position = new Vector3(m_cam.position.x + offsetX, transform.position.y);
        }

    }
}
