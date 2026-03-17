using UnityEngine;
using Zenject;

public class SpawnManager
{
    AnimationCurve m_trapsCount;
    AnimationCurve m_enemiesCount;

    [Inject]
    LevelBuilder m_lvlBuilder;
    [Inject]
    UIController m_UI;

    readonly float m_shopChance = 0.8f;
    bool m_catSpawned;

    public SpawnManager(AnimationCurve trapsCount, AnimationCurve enemiesCount)
    {
        m_trapsCount = trapsCount;
        m_enemiesCount = enemiesCount;
    }

    public int ChooseSpawnObject(bool jumper, int catsLeft, bool shop, bool enemy, bool trap)
    {
        float[] chances = new float[4];

        if (!m_catSpawned && !jumper && catsLeft > 0)
        {
            chances[0] = 0.1f + 0.3f * Mathf.Log10(1 + 9 * (catsLeft / (float)m_UI.AllHerats));
        }

        if (shop)
        {
            chances[1] = m_shopChance;
        }

        if (!jumper && enemy)
        {
            chances[2] = 0.35f * m_enemiesCount.Evaluate(m_lvlBuilder.LevelProgress());
        }

        if (trap)
        {
            chances[3] = 0.12f * m_trapsCount.Evaluate(m_lvlBuilder.LevelProgress());
        }

        int ind = m_lvlBuilder.GetWeightedIndex(chances);
        m_catSpawned = ind == 0;

        if (ind == 3 && Mathf.Approximately(chances[3], 0f))
            return -1;
        else
            return ind;
    }
}
