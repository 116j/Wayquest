using UnityEngine;

public class Clouds : MonoBehaviour
{
    [SerializeField]
    float m_speed = 0.1f;

    void Update()
    {
        //the clouds themselves move slowly
        transform.position -= m_speed * Time.deltaTime * Vector3.right;
    }
}
