using UnityEngine;

[CreateAssetMenu]
public class LevelTheme : ScriptableObject
{
    public GameObject[] m_backgrounds;
    public Color m_leafColor;

    public EnviromentObject[] m_grass;
    public EnviromentObject[] m_bushes;
    public EnviromentObject[] m_trees;

    public SpawnValues[] m_enemies;
    public Trap[] m_ceilTraps;
    public Trap[] m_floorTraps;

    public SpawnValues m_cat;
    public SpawnValues m_shop;
    public SpawnValues m_boss;
    public Trap m_jumper;
    public SpawnValues m_movingPlatform;
    public SpawnValues m_coin;

    public int m_themeNum;
}
