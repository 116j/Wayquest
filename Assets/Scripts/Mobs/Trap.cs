using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class Trap : MonoBehaviour, IMetrics
{
    [SerializeField]
    RuntimeAnimatorController m_animationController;
    [SerializeField]
    AnimationClip[] m_attacks;
    [SerializeField]
    Vector3[] m_offsets;
    [SerializeField]
    Vector3[] m_metrics;
    [SerializeField]
    Vector3[] m_attackDirections;
    [SerializeField]
    AnimationCurve[] m_spawnChances;
    [SerializeField]
    //Series of traps or a single one
    bool m_series;
    [Header("Outlines")]
    [SerializeField]
    Color[] m_outlineColors;
    [SerializeField]
    float[] m_outlineThicknesses;

    protected Animator m_anim;
    Outline m_outline;
    [Inject]
    protected LevelBuilder m_lvlBuilder;
    //Trap number
    int m_trapNumber;
    //Is the trap number selected
    bool m_numberSet = false;

    protected virtual void Awake()
    {
        m_anim = GetComponent<Animator>();
        m_outline = GetComponent<Outline>();
    }

    private void Start()
    {
        if (!m_numberSet)
            SetTrapNum();
        SetAnimations(m_trapNumber);
        SetOffset();
        SetOutline();
    }

    void SetOutline()
    {
        if (m_outline != null)
        {
            if (m_outlineColors.Length > 0)
            {
                m_outline.SetOutline(m_outlineColors[m_trapNumber], m_outlineThicknesses[m_trapNumber]);
            }
        }
    }

    /// <summary>
    /// Sets animations for the overrided trap animation controller
    /// </summary>
    /// <param name="animNum">number of the trap</param>
    protected void SetAnimations(int animNum)
    {
        AnimatorOverrideController aoc = new(m_animationController);
        foreach (var anim in aoc.animationClips)
        {
            aoc[anim.name] = m_attacks[animNum];
        }
        m_anim.runtimeAnimatorController = aoc;
    }
    /// <summary>
    /// Sets a random trap number depending on their chance of occurrence
    /// </summary>
    public void SetTrapNum()
    {
        try
        {
            m_numberSet = true;
            List<float> chances = new List<float>();
            foreach (var spawnChance in m_spawnChances)
            {
                chances.Add(spawnChance.Evaluate(m_lvlBuilder.LevelProgress()));
            }

            float value = Random.Range(0, chances.Sum());
            float sum = 0;
            for (int i = 0; i < chances.Count; i++)
            {
                sum += chances[i];
                if (value < sum)
                {
                    m_trapNumber = i;
                    return;
                }
            }
            m_trapNumber = chances.Count - 1;
        }

        catch (System.NullReferenceException e)
        {
            Debug.Log("Null reference at obj " + gameObject + "\n" + e.Message);
        }

    }
    /// <summary>
    /// Sets a certain trap number
    /// </summary>
    /// <param name="num"></param>
    public void SetTrap(int num)
    {
        m_trapNumber = num;
        m_numberSet = true;
    }

    public int GetTrapNum() => m_trapNumber;

    public void DestroyTrap()
    {
        Destroy(gameObject);
    }

    public float GetHeight()
    {
        return m_metrics[m_trapNumber].y;
    }

    public float GetRightBorder()
    {
        return m_metrics[m_trapNumber].z;
    }

    public float GetLeftBorder()
    {
        return m_metrics[m_trapNumber].x;
    }

    public Vector3 GetOffset()
    {
        return m_offsets[m_trapNumber];
    }

    public float GetWidth()
    {
        return m_metrics[m_trapNumber].z - m_metrics[m_trapNumber].x;
    }

    public void SetOffset()
    {
        transform.position += m_offsets[m_trapNumber];
    }

    public Vector3 GetAttackDirection()
    {
        return m_attackDirections[m_trapNumber];
    }

    public bool IsSeries()
    {
        return m_series;
    }
}
