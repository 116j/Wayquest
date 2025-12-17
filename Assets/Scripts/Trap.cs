using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class Trap : MonoBehaviour, IMetrics
{
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
    //Серия ловушек или одиночная
    bool m_series;

    protected Animator m_anim;
    [Inject]
    protected LevelBuilder m_lvlBuilder;
    //Номер ловушки
    int m_trapNumber;
    //Выбран ли номер ловушки
    bool m_numberSet = false;

    protected virtual void Awake()
    {
        m_anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (!m_numberSet)
            SetTrapNum();
        SetAnimations(m_trapNumber);
        SetOffset();
    }

    /// <summary>
    /// Задает анимации для переопределнного контроллера анимации ловушек
    /// </summary>
    /// <param name="animNum">номер ловувшки</param>
    void SetAnimations(int animNum)
    {
        AnimatorOverrideController aoc = new(m_anim.runtimeAnimatorController);
        var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>(1)
        {
            new(aoc.animationClips[0], m_attacks[animNum])
        };
        aoc.ApplyOverrides(anims);
        m_anim.runtimeAnimatorController = aoc;
    }
    /// <summary>
    /// Задает рандомный номер ловушки в зависимости от их шанса появления
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
    /// Ставит определенной номер ловушки
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
