using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class FillStrategy
{
    protected int m_maxRoomWidth = 60;
    protected int m_minRoomWidth = 12;
    protected int m_roomHeight = 6;
    protected int m_finalRoomHeight = 15;
    protected int m_finalRoomWidth = 45;

    protected int m_minTransitionWidth = 2;
    protected int m_maxTransitionWidth = 30;
    protected int m_maxTransitionHeight = 15;

    protected int m_minElevationHeight = 2;
    protected int m_maxElevationHeight = 20;
    // min width of staright ground
    protected readonly int m_minStraightSection = 6;
    // min width for a section where wnwmy can walk
    protected readonly int m_minEnemyWidth = 6;
    protected readonly int m_maxSlopeHeight = 7;

    int m_enemiesPerLevel;
    int m_trapsPerLevel;
    int m_catsLeft;
    int m_catsSpawned;
    int m_trapsNum;
    bool m_jumper = false;
    // offset for traps spawn when there's a jumper or a platform
    protected float m_rightOffset;
    protected float m_leftOffset;

    protected readonly LevelTheme m_levelTheme;

    [Inject]
    UIController m_UI;
    [Inject]
    protected LevelBuilder m_lvlBuilder;
    [Inject]
    protected ShopLayout m_shop;
    [Inject]
    protected DiContainer m_container;
    [Inject]
    protected TileEditor m_editor;

    protected AnimationCurve m_enemiesCount;
    protected AnimationCurve m_trapsCount;

    protected float m_slopeChance = 0.7f;
    protected float m_jumperChance = 0.4f;

    protected int m_playerJumpWidth = 9;
    protected int m_playerJumpHeight = 6;
    protected readonly float m_playerWidth = 1f;

    bool m_shopSpawned = false;

    public FillStrategy(LevelTheme levelTheme)
    {
        m_levelTheme = levelTheme;
    }

    public FillStrategy(LevelTheme levelTheme, AnimationCurve enemiesCount, AnimationCurve trapsCount)
    {
        m_levelTheme = levelTheme;
        m_enemiesCount = enemiesCount;
        m_trapsCount = trapsCount;
    }

    public void ResetCats()
    {
        m_catsLeft = 0;
    }

    public void CatPetted(int cats)
    {
        m_catsSpawned -= cats;
    }

    public void ShopDestroyed()
    {
        m_shopSpawned = false;
    }

    public void SetTripleJump()
    {
        m_playerJumpHeight = 8;
        m_playerJumpWidth = 13;

    }
    /// <summary>
    /// Creates room with elevations and lowlands, adds landscape and draws tiles
    /// </summary>
    /// <param name="prevRoom">previous room</param>
    /// <param name="transitionStrategy">strategy to create the transition between this and next rooms</param>
    /// <returns>filled room</returns>
    public virtual Room FillRoom(Room prevRoom, FillStrategy transitionStrategy)
    {
        prevRoom.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevRoom.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevRoom.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minRoomWidth, m_maxRoomWidth), start.y);
        //width of start straight section
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        Room room = new Room(end, startWidth, prevRoom.GetNextTransition());

        m_enemiesPerLevel = (int)m_enemiesCount.Evaluate(m_lvlBuilder.LevelProgress());
        m_trapsPerLevel = (int)m_trapsCount.Evaluate(m_lvlBuilder.LevelProgress());
        m_catsLeft = m_UI.AllHerats - m_UI.CurrentHearts - m_catsSpawned;

        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        m_leftOffset = m_playerWidth * 1.5f;
        CreateElevations(room, start + startWidth * Vector3Int.right, startWidth, height, true);
        room.AddTransition(transitionStrategy.FillTransition(room));
        room.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(room, groundTiles, int.MaxValue, true));

        return room;
    }
    /// <summary>
    /// Creates transition from room
    /// </summary>
    /// <param name="room"></param>
    public virtual Room FillTransition(Room room)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(-m_maxTransitionHeight, Mathf.Min(width * m_playerJumpHeight / 3, m_maxTransitionHeight));
        Vector3Int end = new Vector3Int(room.GetEndPosition().x + width, room.GetEndPosition().y + height);
        Room transition = new Room(room.GetEndPosition(), end);

        // if transition width and height are too large for player's jump - create ledges
        if (width > m_playerJumpWidth || height > m_playerJumpHeight || Mathf.Abs(height) > GetJumpHeight(width))
        {
            int localHeight, localWidth;
            Vector3Int lastPoint = transition.GetStartPosition();
            while (lastPoint.x < end.x - 3)
            {
                // max width for ledge where its max height for jump and its located on the straight line between the transition's start and the end
                int maxWidth = GetMaxTileWidthGap(lastPoint, end);
                localWidth = Random.Range(m_minTransitionWidth, Mathf.Clamp(maxWidth, m_minTransitionWidth, Mathf.Min(m_playerJumpWidth, end.x - lastPoint.x - 2)));
                // height between the point of the local width and the max width on the straight line between the end of the transition and the current point 
                localHeight = Random.Range(GetTileHeightGapForWidth(lastPoint, end, localWidth), GetTileHeightGapForWidth(lastPoint, end, maxWidth));
                if (height < 0)
                {
                    localHeight = -localHeight;
                }
                lastPoint = new Vector3Int(lastPoint.x + localWidth, lastPoint.y + localHeight);
                transition.CreateLedge(lastPoint);
            }
        }
        // create bounds for player's fall
        transition.AddEnviromentObject(CreateHorizontalBounds(transition.GetStartPosition(), end, width, height));

        return transition;
    }
    /// <summary>
    /// gets the max width for tile where its max height for jump and it's located on the straight line between the current position and the end
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    protected int GetMaxTileWidthGap(Vector3Int currentPos, Vector3Int end)
    {
        return (int)(m_playerJumpHeight * 1.0f / (Mathf.Abs(end.y - currentPos.y) * 1.0f / (end.x - currentPos.x) + m_playerJumpHeight * 1.0f / m_playerJumpWidth));
    }
    /// <summary>
    /// gets the height for the width in the diagonal between the current position and the end
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="end"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    protected int GetTileHeightGapForWidth(Vector3Int currentPos, Vector3Int end, int width)
    {
        return (int)Mathf.Clamp(Mathf.Abs(end.y - currentPos.y) * 1.0f / (end.x - currentPos.x) * width, 0, Mathf.Abs(end.y - currentPos.y));
    }

    protected int GetJumpWidth(int height)
    {
        return Mathf.CeilToInt(-(Mathf.Abs(height) - m_playerJumpHeight) * 1.0f / m_playerJumpHeight * m_playerJumpWidth);
    }

    protected int GetJumpHeight(int width)
    {
        return Mathf.CeilToInt(-m_playerJumpHeight * 1.0f / m_playerJumpWidth * width + m_playerJumpHeight);
    }

    /// <summary>
    /// Creates start room from start position
    /// </summary>
    /// <param name="start">start position of the room</param>
    /// <param name="transitionStrategy">strategy to create the transition between this and next rooms</param>
    /// <returns>filled room</returns>
    public Room FillStratRoom(Vector3Int start, FillStrategy transitionStrategy)
    {
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minRoomWidth, m_maxRoomWidth), start.y);
        Room room = new Room(start, end, new Room(start, start));
        //width of start straight section
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        // creates polygon with start width
        room.MakePolygon(startWidth, start);
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        CreateElevations(room, start + startWidth * Vector3Int.right, startWidth, height, false);
        room.AddTransition(transitionStrategy.FillTransition(room));
        room.AddEnviromentObject(CreateVerticalBounds(start));
        room.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(room, groundTiles, int.MaxValue, true), isInitial: true);

        return room;
    }

    protected GameObject CreateHorizontalBounds(Vector3 start, Vector3 end, int width, int height)
    {
        BoxCollider2D bounds = new GameObject("HorizontalBound").AddComponent<BoxCollider2D>();
        bounds.gameObject.transform.position = new Vector3(start.x, (height > 0 ? start.y : end.y) - m_roomHeight);
        bounds.isTrigger = true;
        bounds.gameObject.tag = "bounds";
        bounds.size = new Vector2(width + 1, 0.5f);
        bounds.offset = new Vector2((width + 1) / 2, -0.5f);
        return bounds.gameObject;
    }

    public GameObject CreateVerticalBounds(Vector3 pos)
    {
        BoxCollider2D bounds = new GameObject("VerticalBound").AddComponent<BoxCollider2D>();
        bounds.gameObject.transform.position = pos;
        bounds.gameObject.layer = LayerMask.NameToLayer("Wall");
        bounds.size = new Vector2(0.5f, m_playerJumpHeight * 2);
        bounds.offset = new Vector2(-0.25f, m_playerJumpHeight);
        return bounds.gameObject;
    }

    protected void CreateSideBound(Room room, bool isLeft)
    {
        Vector3 pos = (isLeft ?
            room.GetStartPosition() - Vector3Int.up * room.GetTransitionLeftHeight() :
            room.GetEndPosition() - Vector3Int.up * room.GetTransitionRightHeight())
            + new Vector3Int((isLeft ? -1 : 1), 1 - m_roomHeight);
        BoxCollider2D bounds = new GameObject("SideBounds").AddComponent<BoxCollider2D>();
        bounds.gameObject.transform.position = pos;
        bounds.isTrigger = true;
        bounds.gameObject.tag = "bounds";
        bounds.size = new Vector2(m_minStraightSection, bounds.gameObject.transform.position.y - (isLeft ? room.GetEndPosition().y : room.GetStartPosition().y) + m_roomHeight);
        bounds.offset = new Vector2((isLeft ? -1 : 1) * bounds.size.x / 2, -bounds.size.y / 2);
        room.AddEnviromentObject(bounds.gameObject);
    }

    public Room FillFinalRoom(Room prevRoom)
    {
        prevRoom.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevRoom.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevRoom.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + m_minStraightSection * 2 + m_finalRoomWidth, start.y);
        //width of start straight section
        Room room = new Room(end, m_minStraightSection, prevRoom.GetNextTransition());

        room.CreateElevationOrLowland(-m_finalRoomHeight, m_finalRoomWidth, start + m_minStraightSection * Vector3Int.right);
        room.CreateElevationOrLowland(m_finalRoomHeight, m_minStraightSection, start + new Vector3Int(m_minStraightSection + m_finalRoomWidth, -m_finalRoomHeight));
        room.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_boss, new Vector3(start.x + (m_minStraightSection + m_finalRoomWidth - m_levelTheme.m_boss.GetWidth()) / 2, start.y - m_finalRoomHeight), Quaternion.identity, null));
        room.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(room, groundTiles, int.MaxValue, true));
        return room;
    }
    /// <summary>
    /// Creates elevations and lowlands for the room, adds enemies and traps
    /// </summary>
    /// <param name="room"></param>
    /// <param name="lastPoint">last point of the room's straight section</param>
    /// <param name="spawnEnemyOrTrap"></param>
    protected void CreateElevations(Room room, Vector3Int lastPoint, int startWidth, int height, bool spawnEnemyOrTrap)
    {
        int width = startWidth;
        WalkEnemy lastEnemy = null;
        while (room.GetEndPosition().x - lastPoint.x > m_minStraightSection)
        {
            //if slopeChance and the remaining distance is enought for a slope
            if (Random.value > m_slopeChance && m_minElevationHeight * 2 + m_minStraightSection + lastPoint.x <= room.GetEndPosition().x - m_minStraightSection)
            {
                m_rightOffset = 0f;
                m_jumper = false;
                //spawn enemies or traps on previous section
                if (spawnEnemyOrTrap)
                    SpawnEnemyOrTrap(room, width, int.MaxValue, lastPoint-Vector3Int.right*width, ref lastEnemy);
                m_leftOffset = 0f;
                int slopeHeight = Random.Range(m_minElevationHeight, Mathf.Clamp((room.GetEndPosition().x - m_minStraightSection * 2 - lastPoint.x - 1) / 2, m_minElevationHeight, m_maxSlopeHeight));
                width = Random.Range(m_minStraightSection, room.GetEndPosition().x - m_minStraightSection - slopeHeight * 2 - lastPoint.x-1);
                room.CreateSlope(slopeHeight, width, lastPoint);
                // spawn enemies or traps on the slope
                if (spawnEnemyOrTrap)
                    SpawnEnemyOrTrap(room, width, int.MaxValue, new Vector3(lastPoint.x + slopeHeight + 1, lastPoint.y + slopeHeight), ref lastEnemy);

                lastPoint = new Vector3Int(lastPoint.x + slopeHeight * 2 + width + m_minStraightSection + 1, lastPoint.y);
                width = m_minStraightSection;
            }
            else
            {
                //spawn enemies or traps on previous section
                if (spawnEnemyOrTrap)
                    SpawnEnemyOrTrap(room, width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);

                width = Random.Range(m_minStraightSection, room.GetEndPosition().x - lastPoint.x);
                room.CreateElevationOrLowland(height, width, lastPoint);
                // create a moving platform or a jumper if the height is higher than player's jump height
                if (height > m_playerJumpHeight)
                {
                    if (!m_jumper)
                    {
                        MovingPlatform platform = Object.Instantiate(m_levelTheme.m_movingPlatform, 
                            lastPoint + m_levelTheme.m_movingPlatform.GetOffset() + Vector3.up * (height - 2 + m_levelTheme.m_movingPlatform.GetHeight()), 
                            Quaternion.identity).GetComponent<MovingPlatform>();
                        platform.AddCheckpoint(lastPoint + m_levelTheme.m_movingPlatform.GetOffset());
                        room.AddEnviromentObject(platform.gameObject);
                        if (spawnEnemyOrTrap && lastEnemy!=null)
                        {
                            lastEnemy.ConnectPlatform(platform);
                        }
                    }
                    else
                    {
                        Jumper jumper = m_container.InstantiatePrefabForComponent<Jumper>(m_levelTheme.m_jumper, lastPoint, Quaternion.identity, null);
                        jumper.SetWallHeight(height);
                        room.AddEnviromentObject(jumper.gameObject);
                    }
                }

                m_leftOffset = m_playerWidth * 1.5f;
                lastPoint = new Vector3Int(lastPoint.x + width, lastPoint.y + height);
            }

            lastEnemy = null;
            height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
            SetRightOffset(height);
        }
        // add remining tiles for the room
        room.AddTiles(m_roomHeight, room.GetEndPosition().x - lastPoint.x, lastPoint);
        m_rightOffset = -m_playerWidth;
        m_jumper = false;
        if (spawnEnemyOrTrap)
            SpawnEnemyOrTrap(room, room.GetEndPosition().x - lastPoint.x + width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
    }

    protected void SetRightOffset(int height)
    {
        if (height > m_playerJumpHeight)
        {
            if(Random.value > m_jumperChance)
            {
                m_rightOffset = m_levelTheme.m_movingPlatform.GetOffset().x * 2;
                m_jumper = false;
            }
            else
            {
                m_rightOffset = m_levelTheme.m_jumper.GetOffset().x * 2;
                m_jumper = true;
            }
        }
        else
        {
            m_rightOffset = 0f;
            m_jumper = false;
        }
    }

    /// <summary>
    /// Adds landscape to the room's ground
    /// </summary>
    /// <param name="room"></param>
    /// <param name="height">max vegetation height</param>
    /// <param name="addTrees"></param>
    protected void AddLandscape(Room room, HashSet<Vector3Int> groundTiles, int height, bool addTrees)
    {
        int width = 0;
        int grassWidth = 0;
        Vector3Int start = groundTiles.FirstOrDefault();
        Vector3Int grassStart = start;
        foreach (var ground in groundTiles)
        {
            // if tile has grass - don't add one
            if (!m_editor.AddGrass(ground) && ground.y == start.y && ground.x == start.x + width)
            {
                if (grassWidth == 0)
                {
                    grassStart = ground;
                }
                width++;
                grassWidth++;
            }
            // if a ground straight section is over - add vegetation
            else if (grassWidth > 0 || ground.y != start.y || ground.x != start.x + width)
            {
                AddEnvObjects(room, grassWidth, height, grassStart, m_levelTheme.m_grass);

                if (ground.y != start.y || ground.x != start.x + width)
                {
                    AddEnvObjects(room, width, height, start, m_levelTheme.m_bushes, addTrees ? m_levelTheme.m_trees : null);

                    start = grassStart = ground;
                    width = grassWidth = 1;
                }
                else
                {
                    width++;
                    grassWidth = 0;
                }
            }
        }

        AddEnvObjects(room, grassWidth, height, grassStart, m_levelTheme.m_grass);
        AddEnvObjects(room, width, height, start, m_levelTheme.m_bushes, addTrees ? m_levelTheme.m_trees : null);
    }

    /// <summary>
    /// Вспомогательный метод для сброса растений: кусты + опционально деревья
    /// </summary>
    private void AddEnvObjects(Room room, int width, int height, Vector3Int start,
        EnviromentObject[] bushes, EnviromentObject[] trees = null)
    {
        foreach (var obj in AddVegetation(width, height, start, bushes))
            room.AddEnviromentObject(obj.gameObject);

        if (trees != null)
            foreach (var obj in AddVegetation(width, height, start, trees))
                room.AddEnviromentObject(obj.gameObject);
    }

    /// Adds vegetation on the straight section of the ground
    /// </summary>
    /// <param name="width">width of the straight section</param>
    /// <param name="height">max veg's height</param>
    /// <param name="start">start of the straight section</param>
    /// <param name="vegs">array of the vegetation</param>
    /// <returns></returns>
    protected List<EnviromentObject> AddVegetation(int width, int height, Vector3Int start, EnviromentObject[] vegs)
    {
        List<EnviromentObject> objs = new List<EnviromentObject>();
        if (width == 0)
            return objs;
        // tries for respawn
        int tries = width + 2;
        float length = 0;
        while (tries >= 0)
        {
            EnviromentObject obj = Object.Instantiate(vegs[Random.Range(0, vegs.Length)], start, Quaternion.identity).GetComponent<EnviromentObject>();
            Vector3 pos = new Vector3(Random.Range(start.x + obj.GetRightBorder(), start.x + width + obj.GetLeftBorder()), start.y);
            obj.transform.position = pos + obj.GetOffset();
            // if obj collides other objs for more than 1/3 of its width
            bool collides = objs.Any(o => o.transform.position.x > obj.transform.position.x &&
                obj.transform.position.x + obj.GetRightBorder() - o.transform.position.x - o.GetLeftBorder() > obj.GetWidth() / 3 ||
                o.transform.position.x < obj.transform.position.x &&
                o.transform.position.x + o.GetRightBorder() - obj.transform.position.x - obj.GetLeftBorder() > obj.GetWidth() / 3);

            if (obj.GetHeight() > height || collides || (Random.value > 0.65f && length > width * 1.0f / 2) || pos.x + obj.GetRightBorder() > start.x + width || pos.x + obj.GetLeftBorder() < start.x)
            {
                tries--;
                Object.Destroy(obj.gameObject);
                continue;
            }
            length += obj.GetWidth();
            objs.Add(obj);
        }
        return objs;
    }

    /// <summary>
    /// Get random enemy based on their spawn chances
    /// </summary>
    /// <returns></returns>
    int GetEnemyNum()
    {
        List<float> chances = new List<float>();
        foreach (var obj in m_levelTheme.m_enemies)
        {
            WalkEnemy enemy = obj.gameObject.GetComponent<WalkEnemy>();
            m_container.Inject(enemy);
            chances.Add(enemy.GetSpawnChance());
        }

        float value = Random.Range(0, chances.Sum());
        float sum = 0;
        for (int i = 0; i < chances.Count; i++)
        {
            sum += chances[i];
            if (value < sum)
            {
                return i;
            }
        }
        return chances.Count - 1;
    }
    /// <summary>
    /// Spawns enemy or traps
    /// </summary>
    /// <param name="room"></param>
    /// <param name="sectionWidth"></param>
    /// <param name="height">max trap height</param>
    /// <param name="startPos"></param>
    protected void SpawnEnemyOrTrap(Room room, int sectionWidth, int height, Vector3 startPos, ref WalkEnemy lastEnemy)
    {
        if (sectionWidth <= 0)
            return;

        if (m_catsLeft > 0 && (m_catsSpawned == 0 || Random.value < m_catsSpawned / m_catsLeft) && !m_jumper)
        {
            room.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_cat, new Vector3(startPos.x + (sectionWidth - m_levelTheme.m_cat.GetWidth()) / 2, startPos.y), Quaternion.identity, null));
            m_catsLeft--;
            m_catsSpawned++;
        }
        else if (!m_shopSpawned && m_UI.GetMoney() >= m_shop.GetLowestPrice() && sectionWidth + m_rightOffset > m_levelTheme.m_shop.GetWidth() && Random.value > 0.6f)
        {
            room.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_shop, new Vector3(Random.Range(startPos.x, startPos.x + sectionWidth + m_rightOffset - m_levelTheme.m_shop.GetWidth() + 1), startPos.y), Quaternion.identity, null));
            m_shopSpawned = true;
        }
        else if (!m_jumper && sectionWidth > m_minEnemyWidth && m_enemiesPerLevel > 0 && Random.value < (m_enemiesPerLevel / m_enemiesCount.Evaluate(m_lvlBuilder.LevelProgress())))
        {
            SpawnValues enemy = m_levelTheme.m_enemies[GetEnemyNum()];
            Vector3 pos = new Vector3(startPos.x + (sectionWidth - enemy.GetWidth()) / 2, startPos.y);
            lastEnemy = m_container.InstantiatePrefabForComponent<WalkEnemy>(enemy, pos, Quaternion.identity, null);
            room.AddEnviromentObject(lastEnemy.gameObject);
            m_enemiesPerLevel--;
        }
        else if (m_trapsPerLevel > 0 && Random.value < (m_trapsPerLevel / m_trapsCount.Evaluate(m_lvlBuilder.LevelProgress())))
        {
            List<Trap> traps = new List<Trap>();
            while (traps.Count == 0)
            {
                Trap trap = m_levelTheme.m_floorTraps[Random.Range(0, m_levelTheme.m_floorTraps.Length)];
                m_container.Inject(trap);
                trap.SetTrapNum();
                if (trap.GetWidth() > sectionWidth || trap.GetHeight() > height)
                {
                    continue;
                }
                float rightBorder = startPos.x + sectionWidth + m_rightOffset;
                float leftBorder = startPos.x + m_leftOffset;
                m_trapsNum = Random.Range(1, (int)((rightBorder - leftBorder) / (trap.GetWidth() + m_playerWidth)) + 1);


                if (trap.GetAttackDirection() == Vector3.right || trap.GetAttackDirection() == Vector3.forward)
                {
                    // if trap shoots - crop borders
                    if (trap.GetWidth() > sectionWidth / 3)
                    {
                        m_trapsNum = 1;
                    }

                    if (trap.GetAttackDirection() == Vector3.forward)
                    {
                        rightBorder -= sectionWidth / 2;

                    }
                    else
                    {
                        leftBorder += sectionWidth / 2;
                    }
                }
                // if trap is serial - set a series of traps
                if (trap.IsSeries())
                {
                    m_trapsNum = Random.Range(1, (int)Mathf.Clamp(-(trap.GetHeight() - m_playerJumpHeight) * m_playerJumpWidth * 1.0f / m_playerJumpHeight, 1, (rightBorder - trap.GetRightBorder() - leftBorder + trap.GetLeftBorder()) / trap.GetWidth()));
                    Vector3 pos = new Vector3(Random.Range(leftBorder - trap.GetLeftBorder(), rightBorder - trap.GetRightBorder() - m_trapsNum * trap.GetWidth()), startPos.y);
                    for (int i = 0; i < m_trapsNum; i++)
                    {
                        traps.Add(m_container.InstantiatePrefabForComponent<Trap>(trap, pos + i * trap.GetWidth() * Vector3.right, Quaternion.identity, null));
                    }
                }
                else
                {
                    SpawnTrap(leftBorder, rightBorder, startPos.y, trap, traps);
                }

            }

            foreach (var trap in traps)
            {
                room.AddEnviromentObject(trap.gameObject);
            }
            m_trapsPerLevel--;
        }
    }

    void SpawnTrap(float leftBorder, float rightBorder, float posY, Trap trap, List<Trap> traps)
    {
        if (m_trapsNum == 0 || trap.GetWidth() >= rightBorder - leftBorder)
            return;
        float posX = Random.Range(leftBorder - trap.GetLeftBorder(), rightBorder - trap.GetRightBorder());
        m_trapsNum--;
        traps.Add(m_container.InstantiatePrefabForComponent<Trap>(trap, new Vector3(posX, posY), Quaternion.identity, null));

        SpawnTrap(leftBorder, posX + trap.GetLeftBorder() - m_playerWidth, posY, trap, traps);
        SpawnTrap(posX + trap.GetRightBorder() + m_playerWidth, rightBorder, posY, trap, traps);
    }
}
