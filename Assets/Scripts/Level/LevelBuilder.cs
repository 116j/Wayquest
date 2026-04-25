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
    [Inject]
    CameraBounds m_cameraBounds;

    List<Chunk> m_chunks;
    //Used chunk strategies
    List<FillStrategy> m_usedChunksStrategies = new List<FillStrategy>();
    //Used transition strategies
    List<FillStrategy> m_usedTransitionStrategies = new List<FillStrategy>();
    FillStrategy[] m_strategies;
    Chunk m_currentChunk;
    //Should transition camera bounds be turn on
    bool m_transitionBounds = true;
    //Should chunk camera bounds be turn on
    bool m_chunkBounds = true;
    //Number of created chunks
    int m_chunksCount = 0;
    //Index of the current chunk in the chunk array
    int m_chunkIndex = 0;
    //Index of the chunk that needs to be created next
    int m_newChunkIndex = 1;
    bool m_isFinalChunkSpawned = false;
    bool m_newChunk = false;

    bool m_downTargetOffsetSet = false;
    bool m_upTargetOffsetSet = false;
    float m_currentTargetOffset;
    float m_upTargetOffset = 0.2f;
    float m_downTargetOffset = -2.5f;

    bool m_targetOffsetSet => m_downTargetOffsetSet || m_upTargetOffsetSet;

    AudioSource m_audio;

    void Start()
    {
        SetTheme();
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
        //spawns in the begining 3 chunks
        SpawnChunk();
        SpawnChunk();
        SpawnChunk();

        PlayBackgroundMusic();
    }

    void SetTheme()
    {
        if (m_currentTheme == null)
        {
            int m_currentThemeNum = Random.Range(0, m_themes.Length);
            m_currentTheme = m_themes[m_currentThemeNum];
        }
    }

    public void SetLeavesColor(GameObject leafPrefab)
    {
        SetTheme();
        leafPrefab.GetComponent<SpriteRenderer>().color = m_currentTheme.m_leafColor;
    }

    public void PlayBackgroundMusic()
    {
        m_audio = GetComponent<AudioSource>();
        m_audio.clip = m_backgroundMusic[Random.Range(0, m_backgroundMusic.Length)];
        m_audio.Play();
    }

    void Update()
    {
        //the player returned to the previous chunk
        if (m_currentChunk != null && m_player.transform.position.x < m_currentChunk.GetPreviousTransition().GetStartPosition().x)
        {
            if (m_chunkIndex > 0)
            {
                //if there is a chunk with space for falling - sets the camera bounds
                //and a checkpoint for falling to the end of the previous chunk
                if (IsChunkWithFallSpace(m_chunkIndex))
                {
                    m_chunkBounds = true;
                    m_transitionBounds = false;
                    m_player.SetChunkCheckpoint(m_chunks[m_chunkIndex - 1].GetEndPosition(), false);
                }
                //otherwise, sets a checkpoint for falling at the beginning of this chunk
                else
                {
                    m_player.SetChunkCheckpoint(m_currentChunk.GetStartPosition(), true);
                }

                m_currentChunk = m_chunks[--m_chunkIndex];

                //if the previous chunk has space for falling - sets camera bounds
                if (IsChunkWithFallSpace(m_chunkIndex))
                {
                    m_chunkBounds = true;
                    m_transitionBounds = false;
                }
            }
        }
        //if the player leaves the chunk ahead
        else if (m_currentChunk != null && m_player.transform.position.x > m_currentChunk.GetEndPosition().x)
        {
            //if there is a chunk with space for falling - sets the camera bounds
            // and a checkpoint for falling at the beginning of the next chunk
            if (IsChunkWithFallSpace(m_chunkIndex))
            {
                m_chunkBounds = true;
                m_transitionBounds = false;
                m_player.SetChunkCheckpoint(m_chunks[m_chunkIndex + 1].GetStartPosition(), true);
            }
            //otherwise puts a checkpoint for falling at the end of this chunk
            else
            {
                m_player.SetChunkCheckpoint(m_currentChunk.GetEndPosition(), false);
            }
            //if the player is on the last chunk he reached (did not go back) and the final chunk has not been created yet
            //creates a new chunk
            if (m_newChunkIndex == m_chunkIndex + 1 && !m_isFinalChunkSpawned)
            {
                SpawnChunk();
                m_newChunkIndex++;
            }
            //removes chunks at the beginning, if necessary
            ClearChunk();

            m_currentChunk = m_chunks[++m_chunkIndex];

            //if the next chunk has space to fall - sets the camera bounds
            if (IsChunkWithFallSpace(m_chunkIndex))
            {
                m_chunkBounds = true;
                m_transitionBounds = false;
            }
            else
            {
                m_newChunk = true;
            }
        }
        //if the player is on the transition and it's needed to set the transition camera bounds
        else if (m_currentChunk != null && m_player.transform.position.x < m_currentChunk.GetStartPosition().x && m_transitionBounds)
        {
            //changes camera offset for transition
             if (m_currentChunk.GetTransitionHeight() < 0 && !m_downTargetOffsetSet)
            {
                if (m_upTargetOffsetSet)
                {
                    m_upTargetOffsetSet = false;
                    m_player.ChangeCameraTargetOffset(-m_upTargetOffset);
                }
                m_downTargetOffsetSet = true;
                m_player.ChangeCameraTargetOffset(m_downTargetOffset);
                m_currentTargetOffset = m_downTargetOffset;
            }
            else if (m_currentChunk.GetTransitionHeight() > 8 && !m_upTargetOffsetSet)
            {
                if (m_downTargetOffsetSet)
                {
                    m_downTargetOffsetSet = false;
                    m_player.ChangeCameraTargetOffset(-m_downTargetOffset);
                }
                m_upTargetOffsetSet = true;
                m_player.ChangeCameraTargetOffset(m_upTargetOffset);
                m_currentTargetOffset = m_upTargetOffset;
            }
            else if (m_targetOffsetSet)
            {
                m_upTargetOffsetSet = m_downTargetOffsetSet = false;
                m_player.ChangeCameraTargetOffset(-m_currentTargetOffset);
            }

            if (!m_newChunk)
            {
                m_player.SetChunkCheckpoint(m_currentChunk.GetStartPosition(), true);
            }
            else
            {
                m_newChunk = false;
            }
            //signal that the transition bounds ares set
            m_transitionBounds = false;
            m_chunkBounds = true;
            //sets transition camera bounds
            m_currentChunk.SetTransitionCameraBounds(m_cameraBounds);
            m_player.SetCameraBoundsHeight(Mathf.Abs(m_currentChunk.GetTransitionHeight()));
        }
        //if the player is on a chunk and and it's needed to set the chunk camera bounds
        else if (m_currentChunk != null && m_player.transform.position.x >= m_currentChunk.GetStartPosition().x && m_chunkBounds)
        {
            //if there is a final chunk - puts a reborn point at the beginning of the chunk
            if (m_isFinalChunkSpawned && m_chunkIndex == m_chunks.Count - 1)
            {
                m_player.SetRebornCheckpoint(m_currentChunk.GetStartPosition());
            }
            //changes camera offset for specific chunks
            if(IsChunkWithFallSpace(m_chunkIndex))
            {
                if(m_currentChunk.GetEndPosition().y < m_currentChunk.GetStartPosition().y
                    && !m_downTargetOffsetSet)
                {
                    if (m_upTargetOffsetSet)
                    {
                        m_upTargetOffsetSet = false;
                        m_player.ChangeCameraTargetOffset(-m_upTargetOffset);
                    }

                    m_downTargetOffsetSet = true;
                    m_player.ChangeCameraTargetOffset(m_downTargetOffset);
                    m_currentTargetOffset = m_downTargetOffset;
                }
                else if (m_currentChunk.GetEndPosition().y > m_currentChunk.GetStartPosition().y
                    && !m_upTargetOffsetSet)
                {
                    if (m_downTargetOffsetSet)
                    {
                        m_downTargetOffsetSet = false;
                        m_player.ChangeCameraTargetOffset(-m_downTargetOffset);
                    }
                    m_upTargetOffsetSet = true;
                    m_player.ChangeCameraTargetOffset(m_upTargetOffset);
                    m_currentTargetOffset = m_upTargetOffset;
                }
                else if (m_targetOffsetSet)
                {
                    m_upTargetOffsetSet = m_downTargetOffsetSet = false;
                    m_player.ChangeCameraTargetOffset(-m_currentTargetOffset);
                }
            }
            else if (m_targetOffsetSet)
            {
                m_upTargetOffsetSet = m_downTargetOffsetSet = false;
                m_player.ChangeCameraTargetOffset(-m_currentTargetOffset);
            }
            //signal that the chunk bounds ares set 
            m_transitionBounds = true;
            m_chunkBounds = false;
            //sets chunk camera bounds
            m_currentChunk.SetCameraBounds(m_cameraBounds);
            m_player.SetCameraBoundsHeight(m_currentChunk.GetChunkCameraHeight());
        }
    }
    /// <summary>
    /// Checks if the chunk has space for falling
    /// </summary>
    /// <param name="index">index of the chunk</param>
    /// <returns>true - has space for falling</returns>
    bool IsChunkWithFallSpace(int index)
    {
        return m_usedChunksStrategies[index] is GridStrategy
                    || m_usedChunksStrategies[index] is MovingPlatformStrategy
                    || m_usedChunksStrategies[index] is DestroyableBrickStrategy;
    }
    //Number of chunks created is relative to the number of chunks per level
    public float LevelProgress() => m_chunksCount * 1f / m_values.m_chunksCount;
    //Number of created chuncks
    public int GetLevelChunksCount() => m_values.m_chunksCount;
    /// <summary>
    /// Notifies that cats have been petted or destroyed
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
    /// Sets the player's triple jump for all strategies
    /// </summary>
    public void SetTripleJump()
    {
        foreach (var s in m_strategies)
        {
            s.SetTripleJump();
        }
    }
    public void IncreaseBossHealth(int increaseAmount)
    {
        m_strategies[0].IncreaseBossHealth(increaseAmount);
    }
    /// <summary>
    /// Creates a chunk from a random strategy
    /// </summary>
    void SpawnChunk()
    {
        m_chunksCount++;
        //initial chunk
        if (m_chunksCount == 1)
        {
            //transition strategy from the initial chunk
            FillStrategy startTransitionStrategy = m_strategies[Random.Range(0, m_strategies.Length)];
            m_chunks = new List<Chunk>
            {
                m_strategies[0].FillStartChunk(m_startPosition,startTransitionStrategy)
            };
            m_usedChunksStrategies.Add(m_strategies[0]);
            m_usedTransitionStrategies.Add(startTransitionStrategy);
            m_currentChunk = m_chunks[0];
        }
        //if all the chunks of the level are created - creates the final chunk
        else if (m_chunksCount > m_values.m_chunksCount)
        {
            m_isFinalChunkSpawned = true;
            m_usedChunksStrategies.Add(m_strategies[0]);
            m_usedTransitionStrategies.Add(m_strategies[0]);
            m_chunks.Add(m_strategies[0].FillFinalChunk(m_chunks.Last()));
        }
        else
            while (true)
            {
                //chunk strategy
                FillStrategy сhunckStrategy = m_strategies[GetWeightedIndex(m_values.m_strategyWeights)];
                //if the last chunk had space for falling
                // and this chunk too, then it changes strategy, because they cannot go in a row
                if (IsChunkWithFallSpace(m_usedChunksStrategies.Count - 1) &&
                    (сhunckStrategy is GridStrategy
                    || сhunckStrategy is MovingPlatformStrategy
                    || сhunckStrategy is DestroyableBrickStrategy))
                    continue;
                //transition strategy
                FillStrategy transitionStrategy = m_strategies[Random.Range(0, m_strategies.Length)];
                Chunk chunck = сhunckStrategy.FillChunk(m_chunks.Last(), transitionStrategy);
                //if can't create a chunk - changes strategy.
                if (chunck == null)
                    continue;
                m_usedChunksStrategies.Add(сhunckStrategy);
                m_usedTransitionStrategies.Add(transitionStrategy);
                m_chunks.Add(chunck);
                break;
            }
    }
    /// <summary>
    /// Selects the index of from the array depending on the weights
    /// </summary>
    /// <returns></returns>
    public int GetWeightedIndex(float[] weights)
    {
        //рандомное число между 0 и суммой шансов всех стратегий
        float value = Random.Range(0, weights.Sum());
        float sum = 0;
        //идет по списку шансов, пока сумма шансов не будет больше, чем value
        for (int i = 0; i < weights.Length; i++)
        {
            sum += weights[i];
            if (value < sum)
            {
                return i;
            }
        }
        //if the sum is less than the value - returns the last one.
        return weights.Length - 1;
    }
    /// <summary>
    /// Percentage of the probability of generating chunks with enemies from generating other chunks
    /// </summary>
    /// <returns></returns>
    public float GetEnemySpawnChance()
    {
        float floorChunks = m_values.m_strategyWeights[0] + m_values.m_strategyWeights[1];
        float actualFloorChunks = Mathf.Max(0.5f, floorChunks / m_values.m_strategyWeights.Sum());
        float baseChunkRatio = m_values.m_strategyWeights[0] / floorChunks;
        return actualFloorChunks * baseChunkRatio;
    }

    /// <summary>
    /// Deletes the first chunk if there are more than 3 chunks left behind
    /// </summary>
    void ClearChunk()
    {
        if (m_chunkIndex >= 3)
        {
            m_chunks[0].Clear(m_editor, true);
            m_chunks.RemoveAt(0);
            m_usedChunksStrategies.RemoveAt(0);
            m_usedTransitionStrategies.RemoveAt(0);
            //if the first chunk has space for falling - takes the second as the initial one
            int i = IsChunkWithFallSpace(0) ? 1 : 0;
            m_chunkIndex--;
            m_newChunkIndex--;
            //creates a vertical border on the 2nd chunk and sets a reborn point there
            m_chunks[i].AddEnviromentObject(m_usedChunksStrategies[i].CreateVerticalBounds(m_chunks[i].GetStartPosition()));
            m_player.SetRebornCheckpoint(m_chunks[i].GetStartPosition());
        }
    }
    /// <summary>
    /// Restarts chunks (enemies, cats) when the player is revived,
    /// puts the current chunk first if it fits
    /// </summary>
    public void Restart()
    {
        m_player.Restart();
        m_strategies[0].ResetCats();
        for (int i = m_chunkIndex; i >= 0; i--)
        {
            m_chunks[i].Restart();
        }
        //if the first chunk has space for falling - takes the second as the initial one
        m_chunkIndex = IsChunkWithFallSpace(0) ? 1 : 0;
        m_currentChunk = m_chunks[m_chunkIndex];
        m_transitionBounds = false;
        m_chunkBounds = true;
    }
    /// <summary>
    /// Restarts bricks for chunk and transitions from the back and front, if necessary
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
