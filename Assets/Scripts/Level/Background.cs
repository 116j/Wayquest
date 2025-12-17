using UnityEngine;

public class Background : MonoBehaviour
{
    [SerializeField]
    int[] m_darkThemeBG;
    [SerializeField]
    int[] m_calmThemeBG;

    /// <summary>
    /// Рандомно выбирает задний фон в зависимости от темы уровня
    /// </summary>
    /// <param name="thrmrNum">номер темы уровня</param>
    public void SetTheme(int thrmrNum)
    {
        switch (thrmrNum)
        {
            case 0:
                if (m_darkThemeBG.Length > 0)
                {
                    int rnd = Random.Range(0, m_darkThemeBG.Length / 2) * 2;
                    for (int i = m_darkThemeBG[rnd]; i <= m_darkThemeBG[rnd + 1]; i++)
                    {
                        transform.GetChild(i).gameObject.SetActive(true);
                    }
                }
                break;
            case 1:
                if(m_calmThemeBG.Length > 0)
                {
                    int rnd = Random.Range(0, m_calmThemeBG.Length / 2) * 2;
                    for (int i = m_calmThemeBG[rnd]; i <= m_calmThemeBG[rnd + 1]; i++)
                    {
                        transform.GetChild(i).gameObject.SetActive(true);
                    }
                }
                break;
        }
    }
}
