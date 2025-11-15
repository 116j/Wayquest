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
    AnimationCurve m_enemiesCountPerRoom;
    [SerializeField]
    AnimationCurve m_trapsCountPerRoom;
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

    List<Room> m_rooms;
    List<FillStrategy> m_usedRoomStrategies = new List<FillStrategy>();
    List<FillStrategy> m_usedTransitionStrategies = new List<FillStrategy>();
    FillStrategy[] m_strategies;
    Room m_currentRoom;
    bool m_changeTransposer = false;
    bool m_transitionBounds = true;
    bool m_roomBounds = true;
    //Count of spawned rooms
    int m_roomsCount = 1;
    int m_roomIndex = 0;
    int m_newRoomIndex = 1;

    AudioSource m_audio;

    // Start is called before the first frame update
    void Start()
    {
        int m_currentThemeNum = Random.Range(0, m_themes.Length);
        m_currentTheme = m_themes[m_currentThemeNum];
        m_editor.SetTheme(m_currentTheme.m_themeNum);
        Instantiate(m_currentTheme.m_backgrounds[Random.Range(0, m_currentTheme.m_backgrounds.Length)], Camera.main.transform);

        m_strategies = new FillStrategy[]
        {
            new FillStrategy(m_currentTheme,m_enemiesCountPerRoom,m_trapsCountPerRoom),
            new CeilStrategy(m_currentTheme),
            new GridStrategy(m_currentTheme),
            new MovingPlatformStrategy(m_currentTheme, m_movingPlatformSpeed),
            new DestroyableBrickStrategy(m_currentTheme,m_brick)
        };

        foreach (var strategy in m_strategies)
        {
            m_container.Inject(strategy);
        }
        FillStrategy startTransitionStrategy = m_strategies[Random.Range(0, m_strategies.Length)];
        m_rooms = new List<Room>
        {
            m_strategies[0].FillStratRoom(m_startPosition,startTransitionStrategy)
        };
        m_usedRoomStrategies.Add(m_strategies[0]);
        m_usedTransitionStrategies.Add(startTransitionStrategy);
        m_currentRoom = m_rooms[0];
        SpawnRoom();
        SpawnRoom();

        m_audio = GetComponent<AudioSource>();
        m_audio.clip = m_backgroundMusic[Random.Range(0, m_backgroundMusic.Length)];
        m_audio.Play();

    }

    // Update is called once per frame
    void Update()
    {
        if (m_currentRoom != null && m_player.transform.position.x < m_currentRoom.GetPreviousTransition().GetStartPosition().x)
        {
            if (m_roomIndex > 0)
            {
                if (m_usedRoomStrategies[m_roomIndex] is GridStrategy
                    || m_usedRoomStrategies[m_roomIndex] is MovingPlatformStrategy
                    || m_usedRoomStrategies[m_roomIndex] is DestroyableBrickStrategy)
                {
                    m_roomBounds = true;
                    m_transitionBounds = false;
                    m_player.SetLevelCheckpoint(m_rooms[m_roomIndex - 1].GetEndPosition(), false);
                }
                else
                {
                    m_player.SetLevelCheckpoint(m_currentRoom.GetStartPosition(), true);
                }

                m_currentRoom = m_rooms[--m_roomIndex];

                if (m_usedRoomStrategies[m_roomIndex] is GridStrategy
                    || m_usedRoomStrategies[m_roomIndex] is MovingPlatformStrategy
                    || m_usedRoomStrategies[m_roomIndex] is DestroyableBrickStrategy)
                {
                    m_roomBounds = true;
                    m_transitionBounds = false;
                }
            }
        }
        else if (m_currentRoom != null && m_player.transform.position.x > m_currentRoom.GetEndPosition().x)
        {
            if (m_usedRoomStrategies[m_roomIndex] is GridStrategy
                || m_usedRoomStrategies[m_roomIndex] is MovingPlatformStrategy
                || m_usedRoomStrategies[m_roomIndex] is DestroyableBrickStrategy)
            {
                m_roomBounds = true;
                m_transitionBounds = false;
                m_player.SetLevelCheckpoint(m_rooms[m_roomIndex + 1].GetStartPosition(), true);
            }
            else
            {
                m_player.SetLevelCheckpoint(m_currentRoom.GetEndPosition(), false);
            }

            if (m_newRoomIndex == m_roomIndex + 1 && m_roomsCount <= m_values.m_roomsCount)
            {
                SpawnRoom();
                m_newRoomIndex++;
            }
            ClearRoom();

            m_currentRoom = m_rooms[++m_roomIndex];

            if (m_usedRoomStrategies[m_roomIndex] is GridStrategy
                || m_usedRoomStrategies[m_roomIndex] is MovingPlatformStrategy
                || m_usedRoomStrategies[m_roomIndex] is DestroyableBrickStrategy)
            {
                m_roomBounds = true;
                m_transitionBounds = false;
            }
        }
        //resize camera bounds and camera offset
        else if (m_currentRoom != null && m_player.transform.position.x < m_currentRoom.GetStartPosition().x && m_transitionBounds)
        {
            if (m_currentRoom.GetTransitionHeight() < 0 && !m_changeTransposer)
            {
                m_changeTransposer = !m_changeTransposer;
                m_player.ChangeTransposerHeight(m_changeTransposer);
            }
            m_transitionBounds = false;
            m_roomBounds = true;
            m_currentRoom.SetTransitionCameraBounds();
            m_player.SetCameraBoundsHeight(Mathf.Abs(m_currentRoom.GetTransitionHeight()));
        }
        //resize camera bounds and camera offset
        else if (m_currentRoom != null && m_player.transform.position.x >= m_currentRoom.GetStartPosition().x && m_roomBounds)
        {
            if (m_roomsCount >= m_values.m_roomsCount && m_roomIndex == m_rooms.Count - 1)
            {
                m_player.SetRebornCheckpoint(m_currentRoom.GetStartPosition());
            }

            if (m_usedRoomStrategies[m_roomIndex] is not GridStrategy
                && m_usedRoomStrategies[m_roomIndex] is not MovingPlatformStrategy
                && m_usedRoomStrategies[m_roomIndex] is not DestroyableBrickStrategy)
            {
                m_player.SetLevelCheckpoint(m_rooms[m_roomIndex].GetStartPosition(), true);
            }

            if (((m_usedRoomStrategies[m_roomIndex] is GridStrategy
                    || m_usedRoomStrategies[m_roomIndex] is MovingPlatformStrategy
                    || m_usedRoomStrategies[m_roomIndex] is DestroyableBrickStrategy)
                && (m_currentRoom.GetEndPosition().y < m_currentRoom.GetStartPosition().y))
                && !m_changeTransposer
                || m_changeTransposer)
            {
                m_changeTransposer = !m_changeTransposer;
                m_player.ChangeTransposerHeight(m_changeTransposer);
            }

            m_transitionBounds = true;
            m_roomBounds = false;
            m_currentRoom.SetCameraBounds();
            m_player.SetCameraBoundsHeight(m_currentRoom.GetRoomCameraHeight());
        }
    }

    public float LevelProgress() => m_roomsCount / m_values.m_roomsCount;

    public int GetMaxRoomsCount() => m_values.m_roomsCount;

    public void CatPetted(int cats)
    {
        m_strategies[0].CatPetted(cats);
    }

    public void ShopDestroyed()
    {
        m_strategies[0].ShopDestroyed();
    }

    public void SetTripleJump()
    {
        foreach (var s in m_strategies)
        {
            s.SetTripleJump();
        }
    }
    /// <summary>
    /// Creates room from random strategy
    /// </summary>
    void SpawnRoom()
    {
        m_roomsCount++;
        if (m_roomsCount > m_values.m_roomsCount)
        {
            m_usedRoomStrategies.Add(m_strategies[0]);
            m_usedTransitionStrategies.Add(m_strategies[0]);
            m_rooms.Add(m_strategies[0].FillFinalRoom(m_rooms.Last()));
        }
        else
            while (true)
            {
                FillStrategy rs = m_strategies[GetStrategy()];
                if ((m_usedRoomStrategies.Last() is GridStrategy
                    || m_usedRoomStrategies.Last() is MovingPlatformStrategy
                    || m_usedRoomStrategies.Last() is DestroyableBrickStrategy) &&
                    (rs is GridStrategy
                    || rs is MovingPlatformStrategy
                    || rs is DestroyableBrickStrategy))
                    continue;
                FillStrategy ts = m_strategies[Random.Range(0, m_strategies.Length)];
                Room r = rs.FillRoom(m_rooms.Last(), ts);
                if (r == null)
                    continue;
                m_usedRoomStrategies.Add(rs);
                m_usedTransitionStrategies.Add(ts);
                m_rooms.Add(r);
                break;
            }
    }

    int GetStrategy()
    {
        float value = Random.Range(0, m_values.m_strategyWeights.Sum());
        float sum = 0;
        for (int i = 0; i < m_strategies.Length; i++)
        {
            sum += m_values.m_strategyWeights[i];
            if (value < sum)
            {
                return i;
            }
        }
        return 3;
    }

    public float GetEnemySpawnChance()
    {
        return m_values.m_strategyWeights[0] / m_values.m_strategyWeights.Sum();
    }
    /// <summary>
    /// Removes first room if rooms count is larger than 4
    /// </summary>
    void ClearRoom()
    {
        if (m_roomIndex >= 3)
        {
            m_rooms[0].Clear(m_editor, true);
            m_rooms.RemoveAt(0);
            m_usedRoomStrategies.RemoveAt(0);
            m_usedTransitionStrategies.RemoveAt(0);
            int i = m_usedRoomStrategies[0] is GridStrategy
                || m_usedRoomStrategies[0] is MovingPlatformStrategy
                || m_usedRoomStrategies[0] is DestroyableBrickStrategy ? 1 : 0;
            m_roomIndex--;
            m_newRoomIndex--;

            m_rooms[i].AddEnviromentObject(m_usedRoomStrategies[i].CreateVerticalBounds(m_rooms[i].GetStartPosition()));
            m_player.SetRebornCheckpoint(m_rooms[i].GetStartPosition());
        }
    }

    public void Restart()
    {
        m_player.Restart();
        m_strategies[0].ResetCats();
        for (int i = m_roomIndex; i >= 0; i--)
        {
            m_rooms[i].Restart();
        }
        m_roomIndex = m_usedRoomStrategies[0] is GridStrategy
                || m_usedRoomStrategies[0] is MovingPlatformStrategy
                || m_usedRoomStrategies[0] is DestroyableBrickStrategy ? 1 : 0;
        m_currentRoom = m_rooms[m_roomIndex];
        m_transitionBounds = false;
        m_roomBounds = true;
    }

    public void RestartBricks()
    {
        if (m_usedRoomStrategies[m_roomIndex] is DestroyableBrickStrategy)
        {
            m_rooms[m_roomIndex].Restart();
        }
        if (m_usedTransitionStrategies[m_roomIndex] is DestroyableBrickStrategy)
        {
            m_rooms[m_roomIndex].GetNextTransition().Restart();
        }
        if (m_roomIndex > 0 && m_usedTransitionStrategies[m_roomIndex - 1] is DestroyableBrickStrategy)
        {
            m_rooms[m_roomIndex].GetPreviousTransition().Restart();
        }
    }
}
