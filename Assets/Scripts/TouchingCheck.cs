using UnityEngine;

public class TouchingCheck : MonoBehaviour
{
    [SerializeField]
    ContactFilter2D m_groundCastFilter;
    [SerializeField]
    ContactFilter2D m_wallCastFilter;
    [SerializeField]
    ContactFilter2D m_slopeCastFilter;
    [SerializeField]
    ContactFilter2D m_stuckCastFilter;

    Collider2D m_col;

    //Дистанция для определения стены
    readonly float m_wallHitDist = 0.1f;
    //Дистанция для определения земли
    readonly float m_groundHitDist = 0.05f;
    //Дистанция для определения холма
    readonly float m_slopeHitDist = 0.2f;
    //Ширина расширения границ для определения застревания
    readonly float m_skinWidth = 0.02f;

    RaycastHit2D[] m_rayHits = new RaycastHit2D[5];

    void Start()
    {
        m_col = GetComponent<Collider2D>();
    }
    /// <summary>
    /// Определяет небольшое застревание вправо или влево
    /// </summary>
    /// <param name="dist"></param>
    /// <returns></returns>
    public float WallsStuck(float dist)
    {
        //cоздает вокруг объекта прямоугольник для определения застревания 
        var hit = Physics2D.BoxCast(
            transform.position,
            m_col.bounds.size + Vector3.one * m_skinWidth, 0f,
            Vector3.right * Mathf.Sign(dist),
            Mathf.Abs(dist) + m_skinWidth,
            m_stuckCastFilter.layerMask
        );
        return hit.collider != null ? hit.distance : 0f;
    }
    /// <summary>
    /// Определяет небольшое застревание вниз
    /// </summary>
    /// <param name="dist"></param>
    /// <returns></returns>
    public float GroundStuck(float dist)
    {
        //cоздает вокруг объекта прямоугольник для определения застревания 
        var hit = Physics2D.BoxCast(
            transform.position,
            m_col.bounds.size + Vector3.one * m_skinWidth, 0f,
            Vector3.up * Mathf.Sign(dist),
            Mathf.Abs(dist) + m_skinWidth,
            m_stuckCastFilter.layerMask
        );
        return hit.collider != null ? hit.distance : 0f;
    }
    /// <summary>
    /// Если застрял в земле со всех сторон, кроме верха
    /// </summary>
    /// <returns></returns>
    public bool IsGroundStuck()
    {
        return m_col.Cast(-transform.up, m_stuckCastFilter, m_rayHits, m_slopeHitDist) > 0 &&
            m_col.Cast(transform.right, m_stuckCastFilter, m_rayHits, m_slopeHitDist) > 0 &&
            m_col.Cast(-transform.right, m_stuckCastFilter, m_rayHits, m_slopeHitDist) > 0 &&
            m_col.Cast(transform.up, m_stuckCastFilter, m_rayHits, m_slopeHitDist) == 0;
    }
    /// <summary>
    /// Если касается земли
    /// </summary>
    /// <returns></returns>
    public bool IsGrounded()
    {
        return m_col.Cast(-transform.up, m_groundCastFilter, m_rayHits, m_groundHitDist) > 0;
    }
    /// <summary>
    /// Если касается стены
    /// </summary>
    /// <returns></returns>
    public bool IsWalls()
    {
        return m_col.Cast(transform.right, m_wallCastFilter, m_rayHits, m_wallHitDist) > 0;
    }
    /// <summary>
    /// Если поднимается по холму
    /// </summary>
    /// <returns></returns>
    public bool IsSlopeUp()
    {
        return m_col.Cast(transform.right, m_slopeCastFilter, m_rayHits, m_slopeHitDist) > 0 &&
        m_col.Cast(-transform.up, m_groundCastFilter, m_rayHits, m_groundHitDist) > 0;
    }
    /// <summary>
    /// Если спускается по холму
    /// </summary>
    /// <returns></returns>
    public bool IsSlopeDown()
    {
        return m_col.Cast(-transform.right, m_slopeCastFilter, m_rayHits, m_slopeHitDist) > 0 &&
        !Physics2D.Raycast(new Vector2(m_col.bounds.max.x, m_col.bounds.min.y), -transform.up, m_groundHitDist, m_groundCastFilter.layerMask) &&
        m_col.Cast(-transform.up, m_groundCastFilter, m_rayHits, m_groundHitDist) > 0;
    }
}
