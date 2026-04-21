using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LeavesPool : MonoBehaviour
{
    [SerializeField]
    GameObject m_leafPrefab;
    [Range(10, 100)]
    [SerializeField]
    int m_leafCount = 40;
    [Range(0.5f, 3f)]
    [SerializeField]
    float m_fallSpeed = 1.2f;
    [Range(2f, 10f)]
    [SerializeField]
    float m_windPower = 6f;
    [Range(-2f, 2f)]
    [SerializeField]
    float m_windDirection = 0.5f;

    private List<GameObject> m_leaves;
    private Vector3 m_lastPlayerPos;
    private Camera m_cam;
    private float m_screenLeft, m_screenRight, m_screenBottom, m_screenTop;

    [Inject]
    PlayerController m_player;
    [Inject]
    LevelBuilder m_lvlBuilder;

    void Start()
    {
        m_cam = Camera.main;
        m_lastPlayerPos = m_player.transform.position;
        m_lvlBuilder.SetLeavesColor(m_leafPrefab);

        CreateLeaves();
    }

    void CreateLeaves()
    {
        if (m_leaves != null)
        {
            foreach (var leaf in m_leaves)
            {
                if (leaf != null)
                    Destroy(leaf);
            }
        }

        m_leaves = new List<GameObject>();

        for (int i = 0; i < m_leafCount; i++)
        {
            GameObject leaf = Instantiate(m_leafPrefab, transform);
            leaf.transform.position = GetRandomPosition();

            float scale = Random.Range(0.2f, 0.5f);
            leaf.transform.localScale = Vector3.one * scale;
            leaf.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

            m_leaves.Add(leaf);
        }
    }

    Vector3 GetRandomPosition()
    {
        UpdateScreenBounds();
        return new Vector3(
            Random.Range(m_screenLeft, m_screenRight),
            Random.Range(m_screenBottom, m_screenTop),
            0
        );
    }

    void UpdateScreenBounds()
    {
        if (m_cam == null)
            return;
        m_screenLeft = m_cam.transform.position.x - 12f;
        m_screenRight = m_cam.transform.position.x + 12f;
        m_screenBottom = m_cam.transform.position.y - 7f;
        m_screenTop = m_cam.transform.position.y + 7f;
    }

    void Update()
    {
        UpdateScreenBounds();

        //player's movement
        Vector3 delta = m_player.transform.position - m_lastPlayerPos;
        Vector3 wind = new Vector3(
            -delta.x * m_windPower * 1.2f,   //horizontal wind is stronger
            -delta.y * m_windPower,
            0
        );

        //constant horizontal wind
        Vector3 constantWind = new Vector3(m_windDirection, -0.2f, 0);

        foreach (GameObject leaf in m_leaves)
        {
            if (leaf == null)
                continue;

            //fall speed + player speed + horizontal speed 
            Vector3 move = new Vector3(
                wind.x + constantWind.x,
                -m_fallSpeed + wind.y + constantWind.y,
                0
            ) * Time.deltaTime;

            leaf.transform.position += move;

            //rotates when moves horizontaly
            leaf.transform.Rotate(0, 0, move.x * 100f * Time.deltaTime);

            //checks borders
            Vector3 pos = leaf.transform.position;

            if (pos.x < m_screenLeft || pos.x > m_screenRight
                || pos.y < m_screenBottom || pos.y > m_screenTop)
            {
                //respawns leaf above
                leaf.transform.position = GetRandomPosition();
                leaf.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            }
        }

        m_lastPlayerPos = m_player.transform.position;
    }
}