using UnityEngine;

public interface IMetrics
{
    public float GetHeight();
    public float GetRightBorder();
    public float GetLeftBorder();
    public Vector3 GetOffset();
    public float GetWidth();
    public void SetOffset();
}


public class SpawnValues : MonoBehaviour, IMetrics
{
    [SerializeField]
    Vector3 m_spawnOffset;
    [SerializeField]
    Vector3 m_metric;
    [SerializeField]
    bool m_setOffset;

    private void Start()
    {
        if (m_setOffset)
            SetOffset();
    }

    public float GetHeight()
    {
        return m_metric.y;
    }

    public float GetRightBorder()
    {
        return m_metric.z;
    }

    public float GetLeftBorder()
    {
        return m_metric.x;
    }

    public Vector3 GetOffset()
    {
        return m_spawnOffset;
    }

    public float GetWidth()
    {
        return m_metric.z - m_metric.x;
    }

    public void SetOffset()
    {
        transform.position += m_spawnOffset;
    }
}
