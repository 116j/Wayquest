using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [SerializeField]
    //Variable for determining the distance of an object from the player
    float m_parallaxMultiplier;
    //Width of the background image
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
        //moves the object to the left relative to
        transform.position += m_deltaCamMove.x * m_parallaxMultiplier * Vector3.left;
        m_lastCameraPosition = m_cam.position;
        //if it is shifted more than the width of the image, it returns the object to the minimum distance from the camera
        if (Mathf.Abs(m_cam.position.x - transform.position.x) >= m_textureUnitSizeX)
        {
            float offsetX = (m_cam.position.x - transform.position.x) % m_textureUnitSizeX;
            transform.position = new Vector3(m_cam.position.x + offsetX, transform.position.y);
        }

    }
}
