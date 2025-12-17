using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class LevelBuilder : MonoBehaviour
{
    [SerializeField]
    LevelTheme[] m_themes;
    [SerializeField]
    AudioClip[] m_backgroundMusic;

    [Header("Spawn Objects")]
    [SerializeField]
    DestroyableBrick m_brick;

    [Header("Spawn chances")]
    [SerializeField]
    AnimationCurve m_enemiesCountPerChunk;
    [SerializeField]
    AnimationCurve m_trapsCountPerChunk;
    [SerializeField]
    AnimationCurve m_movingPlatformSpeed;

    [SerializeField]
    Vector3Int m_startPosition;
    [SerializeField]
    LevelValues m_values;

    LevelTheme m_currentTheme;
    [Inject]
    DiContainer m_container;
    [Inject]
    TileEditor m_editor;
    [Inject]
    PlayerController m_player;

    List<Chunk> m_chunks;
    //Стратегии, использованные для чанков
    List<FillStrategy> m_usedChunksStrategies = new List<FillStrategy>();
    //Стратегии, использованные для переходов
    List<FillStrategy> m_usedTransitionStrategies = new List<FillStrategy>();
    FillStrategy[] m_strategies;
    Chunk m_currentChunk;
    bool m_changeTransposer = false;
    //Нужно ли включить ли границы перехода
    bool m_transitionBounds = true;
    //Нужно ли включить границы чанка
    bool m_chunkBounds = true;
    //Количество созданных чанков
    int m_chunksCount = 1;
    //Индекс текущего чанка в массиве чанков
    int m_chunkIndex = 0;
    //Индекс следующего чанка в массиве чанков
    int m_newChunkIndex = 1;
    //Создан ли финальный чанк
    bool m_isFinalChunkSpawned = false;

    AudioSource m_audio;

    void Start()
    {
        int m_currentThemeNum = Random.Range(0, m_themes.Length);
        m_currentTheme = m_themes[m_currentThemeNum];
        m_editor.SetTheme(m_currentTheme.m_themeNum);
        Instantiate(m_currentTheme.m_backgrounds[Random.Range(0, m_currentTheme.m_backgrounds.Length)], Camera.main.transform);

        m_strategies = new FillStrategy[]
        {
            new FillStrategy(m_currentTheme,m_enemiesCountPerChunk,m_trapsCountPerChunk),
            new CeilStrategy(m_currentTheme),
            new GridStrategy(m_currentTheme),
            new MovingPlatformStrategy(m_currentTheme, m_movingPlatformSpeed),
            new DestroyableBrickStrategy(m_currentTheme,m_brick)
        };

        foreach (var strategy in m_strategies)
        {
            m_container.Inject(strategy);
        }
        //стратегия перехода от начального чанка
        FillStrategy startTransitionStrategy = m_strategies[Random.Range(0, m_strategies.Length)];
        m_chunks = new List<Chunk>
        {
            m_strategies[0].FillStratChunk(m_startPosition,startTransitionStrategy)
        };
        m_usedChunksStrategies.Add(m_strategies[0]);
        m_usedTransitionStrategies.Add(startTransitionStrategy);
        m_currentChunk = m_chunks[0];
        //создает в начале еще 2 чанка после начального
        SpawnChunk();
        SpawnChunk();

        m_audio = GetComponent<AudioSource>();
        m_audio.clip = m_backgroundMusic[Random.Range(0, m_backgroundMusic.Length)];
        m_audio.Play();
    }

    void Update()
    {
        //игрок вернулся на предыдущий чанк
        if (m_currentChunk != null && m_player.transform.position.x < m_currentChunk.GetPreviousTransition().GetStartPosition().x)
        {
            if (m_chunkIndex > 0)
            {
                //если чанк с пространством для падения - ставит границы камеры
                //и чекпоинт для падения на конец предыдущего чанка
                if (IsChunkWithFallSpace(m_chunkIndex))
                {
                    m_chunkBounds = true;
                    m_transitionBounds = false;
                    m_player.SetChunkCheckpoint(m_chunks[m_chunkIndex - 1].GetEndPosition(), false);
                }
                //иначе ставит чекпоинт для падения на начало этого чанка
                else
                {
                    m_player.SetChunkCheckpoint(m_currentChunk.GetStartPosition(), true);
                }

                m_currentChunk = m_chunks[--m_chunkIndex];

                //если предыдущий чанк с пространством для падения - ставит границы камеры
                if (IsChunkWithFallSpace(m_chunkIndex))
                {
                    m_chunkBounds = true;
                    m_transitionBounds = false;
                }
            }
        }
        //если игрок уходит с чанка вперерд
        else if (m_currentChunk != null && m_player.transform.position.x > m_currentChunk.GetEndPosition().x)
        {
            //если чанк с пространством для падения - ставит границы камеры
            //и чекпоинт для падения на начало следующего чанка
            if (IsChunkWithFallSpace(m_chunkIndex))
            {
                m_chunkBounds = true;
                m_transitionBounds = false;
                m_player.SetChunkCheckpoint(m_chunks[m_chunkIndex + 1].GetStartPosition(), true);
            }
            //иначе ставит чекпоинт для падения на конец этого чанка
            else
            {
                m_player.SetChunkCheckpoint(m_currentChunk.GetEndPosition(), false);
            }
            //если игрок на последнем чанке, до которого он добрался (не уходил назад) и не был создан еще финальный чанк
            //создает новый чанк
            if (m_newChunkIndex == m_chunkIndex + 1 && !m_isFinalChunkSpawned)
            {
                SpawnChunk();
                m_newChunkIndex++;
            }
            //убирает первый чанк в списке, если количество созданных больше 4
            ClearChunk();

            m_currentChunk = m_chunks[++m_chunkIndex];

            //если следующий чанк с пространством для падения - ставит границы камеры
            if (IsChunkWithFallSpace(m_chunkIndex))
            {
                m_chunkBounds = true;
                m_transitionBounds = false;
            }
        }
        //если игрок уходит с чанка назад на переход и нужно включить границы камеры пререхода
        else if (m_currentChunk != null && m_player.transform.position.x < m_currentChunk.GetStartPosition().x && m_transitionBounds)
        {
            //меняет сдвиг в камере выше, если перход низходящий и сдвиг не был изменен
            if (m_currentChunk.GetTransitionHeight() < 0 && !m_changeTransposer)
            {
                m_changeTransposer = !m_changeTransposer;
                m_player.ChangeTransposerHeight(m_changeTransposer);
            }
            //сигнал, что границы перехода включены 
            m_transitionBounds = false;
            m_chunkBounds = true;
            //ставит границы камеры перехода
            m_currentChunk.SetTransitionCameraBounds();
            m_player.SetCameraBoundsHeight(Mathf.Abs(m_currentChunk.GetTransitionHeight()));
        }
        //если игрок и нужно включить границы камеры чанка
        else if (m_currentChunk != null && m_player.transform.position.x >= m_currentChunk.GetStartPosition().x && m_chunkBounds)
        {
            //если финальный чанк - ставит точку перерождения в начале чанка
            if (m_chunksCount >= m_values.m_chunksCount && m_chunkIndex == m_chunks.Count - 1)
            {
                m_player.SetRebornCheckpoint(m_currentChunk.GetStartPosition());
            }
            //если чанк с без пространства для падения - ставит чекроинт для падения на начало предыдущиего чанка
            if (!IsChunkWithFallSpace(m_chunkIndex))
            {
                m_player.SetChunkCheckpoint(m_chunks[m_chunkIndex].GetStartPosition(), true);
            }
            //если чанк с пространством для падения, низходящий и не было сдвига, или ничего из этого и уже был сдвиг - меняет сдвиг камеры
            if (IsChunkWithFallSpace(m_chunkIndex)
                && (m_currentChunk.GetEndPosition().y < m_currentChunk.GetStartPosition().y)
                && !m_changeTransposer
                || m_changeTransposer)
            {
                m_changeTransposer = !m_changeTransposer;
                m_player.ChangeTransposerHeight(m_changeTransposer);
            }
            //сигнал, что границы чанка включены 
            m_transitionBounds = true;
            m_chunkBounds = false;
            //ставит границы камеры чанка
            m_currentChunk.SetCameraBounds();
            m_player.SetCameraBoundsHeight(m_currentChunk.GetChunkCameraHeight());
        }
    }
    /// <summary>
    /// Проверяет, есть ли у чанка пространство для падения
    /// </summary>
    /// <param name="index">индекс существующего чанка</param>
    /// <returns>true - есть пространство для падения</returns>
    bool IsChunkWithFallSpace(int index)
    {
        return m_usedChunksStrategies[index] is GridStrategy
                    || m_usedChunksStrategies[index] is MovingPlatformStrategy
                    || m_usedChunksStrategies[index] is DestroyableBrickStrategy;
    }
    //Количество созданных чанков относительно количества чанков на уровне
    public float LevelProgress() => m_chunksCount / m_values.m_chunksCount;
    //Количество чанков на уровне всего
    public int GetLevelChunksCount() => m_values.m_chunksCount;
    /// <summary>
    /// Сообщает, что кошек погладили или они разрушены
    /// </summary>
    /// <param name="cats"></param>
    public void CatPetted(int cats)
    {
        m_strategies[0].CatPetted(cats);
    }

    public void ShopDestroyed()
    {
        m_strategies[0].ShopDestroyed();
    }
    /// <summary>
    /// Устанавливает тройной прыжок игрока для всех стратегий
    /// </summary>
    public void SetTripleJump()
    {
        foreach (var s in m_strategies)
        {
            s.SetTripleJump();
        }
    }
    /// <summary>
    /// Создает чанк из рандомной стратегии
    /// </summary>
    void SpawnChunk()
    {
        m_chunksCount++;
        //если все чанки уровня созданы - создает финальный чанк
        if (m_chunksCount > m_values.m_chunksCount)
        {
            m_isFinalChunkSpawned = true;
            m_usedChunksStrategies.Add(m_strategies[0]);
            m_usedTransitionStrategies.Add(m_strategies[0]);
            m_chunks.Add(m_strategies[0].FillFinalChunk(m_chunks.Last()));
        }
        else
            while (true)
            {
                //стртегия для создания чанка
                FillStrategy rs = m_strategies[GetStrategy()];
                //если последний чанк был с пространством для падения, 
                //и этот чанк тоже, то меняет стратегию, т.к. они не могут идти подряд
                if (IsChunkWithFallSpace(m_usedChunksStrategies.Count - 1) &&
                    (rs is GridStrategy
                    || rs is MovingPlatformStrategy
                    || rs is DestroyableBrickStrategy))
                    continue;
                //стратегия для перехода
                FillStrategy ts = m_strategies[Random.Range(0, m_strategies.Length)];
                Chunk r = rs.FillChunk(m_chunks.Last(), ts);
                //если не получилось создать чанк - меняет стратегию
                if (r == null)
                    continue;
                m_usedChunksStrategies.Add(rs);
                m_usedTransitionStrategies.Add(ts);
                m_chunks.Add(r);
                break;
            }
    }
    /// <summary>
    /// Выбирает номер стратегии для создания
    /// </summary>
    /// <returns></returns>
    int GetStrategy()
    {
        //рандомное число между 0 и суммой шансов всех стратегий
        float value = Random.Range(0, m_values.m_strategyWeights.Sum());
        float sum = 0;
        //идет по списку шансов, пока сумма шансов не будет больше, чем value
        for (int i = 0; i < m_strategies.Length; i++)
        {
            sum += m_values.m_strategyWeights[i];
            if (value < sum)
            {
                return i;
            }
        }
        //если сумма меньше чисел, то возвращает последнее
        return m_strategies.Length - 1;
    }
    //Процент вероятности генерации чанков с врагами от генерацыии других чанков
    public float GetEnemySpawnChance()
    {
        return m_values.m_strategyWeights[0] / m_values.m_strategyWeights.Sum();
    }
    /// <summary>
    /// Удаляет первый чанк, если количество существующих чанков больше 4
    /// </summary>
    void ClearChunk()
    {
        if (m_chunkIndex >= 3)
        {
            m_chunks[0].Clear(m_editor, true);
            m_chunks.RemoveAt(0);
            m_usedChunksStrategies.RemoveAt(0);
            m_usedTransitionStrategies.RemoveAt(0);
            //если первый чанк с пространством для падения - берет второй как начальный
            int i = IsChunkWithFallSpace(0) ? 1 : 0;
            m_chunkIndex--;
            m_newChunkIndex--;
            //создает вертикальную границу на 2 чанке и устанавливает там точку возрождения
            m_chunks[i].AddEnviromentObject(m_usedChunksStrategies[i].CreateVerticalBounds(m_chunks[i].GetStartPosition()));
            m_player.SetRebornCheckpoint(m_chunks[i].GetStartPosition());
        }
    }
    /// <summary>
    /// Перезапускает чанки (врагов, котов) при возрождении игрока, 
    /// ставит текущим чанком первый, если подходит
    /// </summary>
    public void Restart()
    {
        m_player.Restart();
        m_strategies[0].ResetCats();
        for (int i = m_chunkIndex; i >= 0; i--)
        {
            m_chunks[i].Restart();
        }
        //если первый чанк с пространством для падения - берет второй как начальный
        m_chunkIndex = IsChunkWithFallSpace(0) ? 1 : 0;
        m_currentChunk = m_chunks[m_chunkIndex];
        m_transitionBounds = false;
        m_chunkBounds = true;
    }
    /// <summary>
    /// Перезапускает кирпичи для чанка и переходов сдади и спереди, если нужно
    /// </summary>
    public void RestartBricks()
    {
        if (m_usedChunksStrategies[m_chunkIndex] is DestroyableBrickStrategy)
        {
            m_chunks[m_chunkIndex].Restart();
        }
        if (m_usedTransitionStrategies[m_chunkIndex] is DestroyableBrickStrategy)
        {
            m_chunks[m_chunkIndex].GetNextTransition().Restart();
        }
        if (m_chunkIndex > 0 && m_usedTransitionStrategies[m_chunkIndex - 1] is DestroyableBrickStrategy)
        {
            m_chunks[m_chunkIndex].GetPreviousTransition().Restart();
        }
    }
}
