using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class FillStrategy
{
    protected int m_maxChunkWidth = 60;
    protected int m_minChunkWidth = 12;
    //Standard height of the straight section
    protected int m_chunkHeight = 6;
    protected int m_finalChunkHeight = 15;
    protected int m_finalChunkWidth = 45;

    protected int m_minTransitionWidth = 2;
    protected int m_maxTransitionWidth = 30;
    protected int m_maxTransitionHeight = 15;

    protected int m_minElevationHeight = 2;
    protected int m_maxElevationHeight = 20;
    //Minimum width of a straight section
    protected readonly int m_minStraightSection = 6;
    //Minimum width of the area on which the enemy can walk
    protected readonly int m_minEnemyWidth = 6;
    protected readonly int m_maxSlopeHeight = 7;

    //Max number of enemies per chunk
    int m_enemiesPerChunk;
    //Max number of traps per chunk
    int m_trapsPerChunk;
    //How many cats it's needed to create at the moment to fully replenish the player's health
    int m_catsLeft;
    //How many cats are currently created in the game
    int m_catsSpawned;
    int m_trapsNum;
    bool m_jumper = false;
    //Offset for creating traps when there is a jumper or platform
    protected float m_rightOffset;
    //Offset to create traps for the player to land
    protected float m_leftOffset;

    protected readonly LevelTheme m_levelTheme;
    readonly SpawnManager m_spawnManager;

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

    //Probability of slope generation
    protected float m_slopeChance = 0.7f;
    //Probability of jumper generation
    protected float m_jumperChance = 0.4f;

    //Width of the player's jump at the same height
    protected int m_playerJumpWidth = 9;
    //Height of the player's jump at the same width
    protected int m_playerJumpHeight = 6;
    //Width od the player
    protected readonly float m_playerWidth = 1f;

    bool m_shopSpawned = false;
    bool m_increaseBossHealth = false;

    public FillStrategy(LevelTheme levelTheme)
    {
        m_levelTheme = levelTheme;
    }

    public FillStrategy(LevelTheme levelTheme, AnimationCurve enemiesCount, AnimationCurve trapsCount)
    {
        m_levelTheme = levelTheme;
        m_enemiesCount = enemiesCount;
        m_trapsCount = trapsCount;

        m_spawnManager = new SpawnManager(trapsCount, enemiesCount);
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
    /// <summary>
    /// Sets the triple jump
    /// </summary>
    public void SetTripleJump()
    {
        m_playerJumpHeight = 8;
        m_playerJumpWidth = 13;
    }
    public void IncreaseBossHealth()
    {
        m_increaseBossHealth = true;
    }
    /// <summary>
    /// Creates elevations and lowlands for the chunk, adds a landscape and draws tiles
    /// </summary>
    /// <param name="prevChunk">previous chunk</param>
    /// <param name="transitionStrategy">strategy for building a transition to the next chunk</param>
    /// <returns>filled chunk</returns>
    public virtual Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //draws transition tiles from the previous chunk to this one
        prevChunk.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevChunk.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevChunk.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minChunkWidth, m_maxChunkWidth), start.y);
        //width of the initial straight section
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        Chunk chunk = new Chunk(end, startWidth, prevChunk.GetNextTransition());

        m_enemiesPerChunk = (int)m_enemiesCount.Evaluate(m_lvlBuilder.LevelProgress());
        m_trapsPerChunk = (int)m_trapsCount.Evaluate(m_lvlBuilder.LevelProgress());
        //how many cats to create, depending on the player's lost health and existing cats
        m_catsLeft = m_UI.AllHerats - m_UI.CurrentHearts - m_catsSpawned;

        //height of the next section
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        m_leftOffset = m_playerWidth * 1.5f;
        CreateElevationsAndLowlands(chunk, start + startWidth * Vector3Int.right, startWidth, height, true);
        chunk.AddTransition(transitionStrategy.FillTransition(chunk));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, true));

        return chunk;
    }
    /// <summary>
    /// Creats the chunk transition
    /// </summary>
    /// <param name="chunk"></param>
    public virtual Chunk FillTransition(Chunk chunk)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(-m_maxTransitionHeight, Mathf.Min(width * m_playerJumpHeight / 3, m_maxTransitionHeight));
        Vector3Int end = new Vector3Int(chunk.GetEndPosition().x + width, chunk.GetEndPosition().y + height);
        Chunk transition = new Chunk(chunk.GetEndPosition(), end);

        //if the width and height of the transition are too large for the player to jump - creates protrusions
        if (width > m_playerJumpWidth || height > m_playerJumpHeight || Mathf.Abs(height) > GetJumpHeight(width))
        {
            int gapHeight, gapWidth;
            Vector3Int lastPoint = transition.GetStartPosition();
            while (lastPoint.x < end.x - 3)
            {
                //max width for the gap between the previous point and the end of the transition 
                int maxGapWidth = GetMaxWidthGapForJump(lastPoint, end);
                //the width of the gap
                gapWidth = Random.Range(m_minTransitionWidth, Mathf.Clamp(maxGapWidth, m_minTransitionWidth, Mathf.Min(m_playerJumpWidth, end.x - lastPoint.x - 2)));
                //height of the gap is between the height from the width of the gap and the height from the max width of the gap
                gapHeight = Random.Range(GetGapHeightInDiagonalWidth(lastPoint, end, gapWidth), GetGapHeightInDiagonalWidth(lastPoint, end, maxGapWidth));
                if (height < 0)
                {
                    gapHeight = -gapHeight;
                }
                lastPoint = new Vector3Int(lastPoint.x + gapWidth, lastPoint.y + gapHeight);
                transition.CreateLedge(lastPoint);
            }
        }
        //creates a bound for the player to fall
        transition.AddEnviromentObject(CreateHorizontalBounds(transition.GetStartPosition(), end, width, height));

        return transition;
    }
    /// <summary>
    /// Max width of the gap that the player can jump from the current point to the end point
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    protected int GetMaxWidthGapForJump(Vector3Int currentPos, Vector3Int end)
    {
        return (int)(m_playerJumpHeight * 1.0f / (Mathf.Abs(end.y - currentPos.y) * 1.0f / (end.x - currentPos.x) + m_playerJumpHeight * 1.0f / m_playerJumpWidth));
    }
    /// <summary>
    /// Height of the gap for a specific width on the diagonal between the current position and the end
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="end"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    protected int GetGapHeightInDiagonalWidth(Vector3Int currentPos, Vector3Int end, int width)
    {
        return (int)Mathf.Clamp(Mathf.Abs(end.y - currentPos.y) * 1.0f / (end.x - currentPos.x) * width, 0, Mathf.Abs(end.y - currentPos.y));
    }
    /// <summary>
    /// The width of the player's jump depends on the height of the jump
    /// </summary>
    /// <param name="height"></param>
    /// <returns></returns>
    protected int GetJumpWidth(int height)
    {
        return Mathf.CeilToInt(-(Mathf.Abs(height) - m_playerJumpHeight) * 1.0f / m_playerJumpHeight * m_playerJumpWidth);
    }
    /// <summary>
    /// The height of the player's jump depends on the width of the jump
    /// </summary>
    /// <param name="width"></param>
    /// <returns></returns>
    protected int GetJumpHeight(int width)
    {
        return Mathf.CeilToInt(-m_playerJumpHeight * 1.0f / m_playerJumpWidth * width + m_playerJumpHeight);
    }

    /// <summary>
    /// Creates a start chunk for the initial position
    /// </summary>
    /// <param name="start">start of the chunk</param>
    /// <param name="transitionStrategy">strategy for creating a transition between this and the next chunks</param>
    /// <returns>filled chunk</returns>
    public Chunk FillStartChunk(Vector3Int start, FillStrategy transitionStrategy)
    {
        m_container.Inject(m_spawnManager);
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minChunkWidth, m_maxChunkWidth), start.y);
        Chunk chunk = new Chunk(start, end, new Chunk(start, start));
        //width of the initial straight section
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        //creats a polygon with the width of the initial straight section
        chunk.MakePolygon(startWidth, start);
        //height of the initial straight section
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        CreateElevationsAndLowlands(chunk, start + startWidth * Vector3Int.right, startWidth, height, false);
        chunk.AddTransition(transitionStrategy.FillTransition(chunk));
        //border is on the left, so that it cannot be passed, because there is nothing there
        chunk.AddEnviromentObject(CreateVerticalBounds(start));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, true), isInitial: true);

        return chunk;
    }
    /// <summary>
    /// Creates a horizontal bound for the player to fall, which takes him to the start or end of the chunk
    /// </summary>
    /// <param name="start">start of the bound</param>
    /// <param name="end">end of the bound</param>
    /// <param name="width">width of the chunk or transition</param>
    /// <param name="height">height of the chunk or transition</param>
    /// <returns></returns>
    protected GameObject CreateHorizontalBounds(Vector3 start, Vector3 end, int width, int height)
    {
        BoxCollider2D bounds = new GameObject("HorizontalBound").AddComponent<BoxCollider2D>();
        bounds.gameObject.transform.position = new Vector3(start.x, (height > 0 ? start.y : end.y) - m_chunkHeight);
        bounds.isTrigger = true;
        bounds.gameObject.tag = "bounds";
        bounds.size = new Vector2(width + 1, 0.5f);
        bounds.offset = new Vector2((width + 1) / 2, -0.5f);
        return bounds.gameObject;
    }
    /// <summary>
    /// Creates a vertical border that cannot be passed through,
    /// usually at the beginning of the chunk
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public GameObject CreateVerticalBounds(Vector3 pos)
    {
        BoxCollider2D bounds = new GameObject("VerticalBound").AddComponent<BoxCollider2D>();
        bounds.gameObject.transform.position = pos;
        bounds.gameObject.layer = LayerMask.NameToLayer("Wall");
        bounds.size = new Vector2(0.5f, m_playerJumpHeight * 2);
        bounds.offset = new Vector2(-0.25f, m_playerJumpHeight);
        return bounds.gameObject;
    }
    /// <summary>
    /// Creates side bounds under the chunks for the player to fall, which takes him to the start or end of the chunk
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="isLeft"></param>
    protected void CreateSideBound(Chunk chunk, bool isLeft)
    {
        Vector3 pos = (isLeft ?
            chunk.GetStartPosition() - Vector3Int.up * chunk.GetTransitionLeftHeight() :
            chunk.GetEndPosition() - Vector3Int.up * chunk.GetTransitionRightHeight())
            + new Vector3Int((isLeft ? -1 : 1), 1 - m_chunkHeight);
        BoxCollider2D bounds = new GameObject("SideBounds").AddComponent<BoxCollider2D>();
        bounds.gameObject.transform.position = pos;
        bounds.isTrigger = true;
        bounds.gameObject.tag = "bounds";
        bounds.size = new Vector2(m_minStraightSection, bounds.gameObject.transform.position.y - (isLeft ? chunk.GetEndPosition().y : chunk.GetStartPosition().y) + m_chunkHeight);
        bounds.offset = new Vector2((isLeft ? -1 : 1) * bounds.size.x / 2, -bounds.size.y / 2);
        chunk.AddEnviromentObject(bounds.gameObject);
    }
    /// <summary>
    /// Creates the final chunk with the boss
    /// </summary>
    /// <param name="prevChunk"></param>
    /// <returns></returns>
    public Chunk FillFinalChunk(Chunk prevChunk)
    {
        prevChunk.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevChunk.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevChunk.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + m_minStraightSection * 2 + m_finalChunkWidth, start.y);
        Chunk chunk = new Chunk(end, m_minStraightSection, prevChunk.GetNextTransition());

        //start of the chunk
        chunk.CreateElevationOrLowland(-m_finalChunkHeight, m_finalChunkWidth, start + m_minStraightSection * Vector3Int.right);
        //lowland where the boss is walking
        chunk.CreateElevationOrLowland(m_finalChunkHeight, m_minStraightSection, start + new Vector3Int(m_minStraightSection + m_finalChunkWidth, -m_finalChunkHeight));      
        BossScript boss = m_container.InstantiatePrefab(m_levelTheme.m_boss, new Vector3(start.x + (m_minStraightSection + m_finalChunkWidth - m_levelTheme.m_boss.GetWidth()) / 2, start.y - m_finalChunkHeight), Quaternion.identity, null).GetComponent<BossScript>();
        if (m_increaseBossHealth)
        {
            boss.IncreaseHealth();
        }
        chunk.AddEnviromentObject(boss.gameObject);
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, true));
        return chunk;
    }
    /// <summary>
    /// Creates lowlands, uplands and slopes for chunks, adds enemies and traps
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="lastPoint">last point of the straight section</param>
    /// <param name="spawnEnemyOrTrap"></param>
    protected void CreateElevationsAndLowlands(Chunk chunk, Vector3Int lastPoint, int startWidth, int height, bool spawnEnemyOrTrap)
    {
        //width of the section
        int width = startWidth;
        //enemy in the last section
        WalkEnemy lastEnemy = null;
        while (chunk.GetEndPosition().x - lastPoint.x > m_minStraightSection)
        {
            //if slopeChance and the remaining distance are enough to generate a slope
            if (Random.value > m_slopeChance && m_minElevationHeight * 2 + m_minStraightSection + lastPoint.x <= chunk.GetEndPosition().x - m_minStraightSection)
            {
                //resets the right offset because there is no height change
                m_rightOffset = 0f;
                m_jumper = false;
                //creates enemies and traps in the previous section, if necessary
                if (spawnEnemyOrTrap)
                    SpawnObjectInTheSection(chunk, width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
                //resets the left offset because there is no height change and the previous section is filled in
                m_leftOffset = 0f;
                int slopeHeight = Random.Range(m_minElevationHeight, Mathf.Clamp((chunk.GetEndPosition().x - m_minStraightSection * 2 - lastPoint.x - 1) / 2, m_minElevationHeight, m_maxSlopeHeight));
                width = Random.Range(m_minStraightSection, chunk.GetEndPosition().x - m_minStraightSection - slopeHeight * 2 - lastPoint.x - 1);
                chunk.CreateSlope(slopeHeight, width, lastPoint);
                //creates enemies and traps on the slope, if necessary
                if (spawnEnemyOrTrap)
                    SpawnObjectInTheSection(chunk, width, int.MaxValue, new Vector3(lastPoint.x + slopeHeight + 1, lastPoint.y + slopeHeight), ref lastEnemy);
                //point after the slope
                lastPoint = new Vector3Int(lastPoint.x + slopeHeight * 2 + width + m_minStraightSection + 1, lastPoint.y);
                //width of the section after the slope
                width = m_minStraightSection;
            }
            else
            {
                //creates enemies and traps in the previous section, if necessary
                if (spawnEnemyOrTrap)
                    SpawnObjectInTheSection(chunk, width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
                //width of the new section
                width = Random.Range(m_minStraightSection, chunk.GetEndPosition().x - lastPoint.x);
                chunk.CreateElevationOrLowland(height, width, lastPoint);
                //creates a platform or jumper at the end of the previous section if the height of the new section is higher than the player's jump
                if (height > m_playerJumpHeight)
                {
                    if (!m_jumper)
                    {
                        MovingPlatform platform = Object.Instantiate(m_levelTheme.m_movingPlatform,
                            lastPoint + m_levelTheme.m_movingPlatform.GetOffset() + Vector3.up * (height - 2 + m_levelTheme.m_movingPlatform.GetHeight()),
                            Quaternion.identity).GetComponent<MovingPlatform>();
                        platform.AddCheckpoint(lastPoint + m_levelTheme.m_movingPlatform.GetOffset());
                        chunk.AddEnviromentObject(platform.gameObject);
                        if (spawnEnemyOrTrap && lastEnemy != null)
                        {
                            lastEnemy.ConnectPlatform(platform);
                        }
                    }
                    else
                    {
                        Jumper jumper = m_container.InstantiatePrefabForComponent<Jumper>(m_levelTheme.m_jumper, lastPoint, Quaternion.identity, null);
                        jumper.SetWallHeight(height);
                        chunk.AddEnviromentObject(jumper.gameObject);
                    }
                }
                //offset at the beginning for the player
                m_leftOffset = m_playerWidth * 1.5f;
                //updates the position to the beginning of a new section
                lastPoint = new Vector3Int(lastPoint.x + width, lastPoint.y + height);
            }
            //resrts the last enemy
            lastEnemy = null;
            //height of the next section
            height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
            //sets the offset at the end of the section depending on the height of the new one
            SetRightOffset(height);
        }
        //adds remaining tiles
        chunk.AddTiles(m_chunkHeight, chunk.GetEndPosition().x - lastPoint.x, lastPoint);
        m_rightOffset = -m_playerWidth;
        m_jumper = false;
        //adds enemies and traps to the remaining tiles, if necessary
        if (spawnEnemyOrTrap)
            SpawnObjectInTheSection(chunk, chunk.GetEndPosition().x - lastPoint.x + width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
    }
    /// <summary>
    /// Defines the right offset of the section depending on the height of the next section
    /// If the next section is an elevation, it decides whether there will be a platform or a jumper
    /// </summary>
    /// <param name="height">height of the next section</param>
    protected void SetRightOffset(int height)
    {
        if (height > m_playerJumpHeight)
        {
            if (Random.value > m_jumperChance)
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
    /// Adds a landscape on the ground
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="height">max height of the vegetation</param>
    /// <param name="addTrees"></param>
    protected void AddLandscape(Chunk chunk, HashSet<Vector3Int> groundTiles, int height, bool addTrees)
    {
        int width = 0;
        //the width of the grass section
        int grassWidth = 0;
        Vector3Int start = groundTiles.FirstOrDefault();
        Vector3Int grassStart = start;
        foreach (var ground in groundTiles)
        {
            //if a tile with grass has been added - do not add grass
            //walking on a straight section of the ground and counting the number of tiles
            if (!m_editor.AddGrass(ground) && ground.y == start.y && ground.x == start.x + width)
            {
                if (grassWidth == 0)
                {
                    grassStart = ground;
                }
                width++;
                grassWidth++;
            }
            //if a straight section of the ground or grass has ended - add vegetation
            else if (grassWidth > 0 || ground.y != start.y || ground.x != start.x + width)
            {
                //adds grass
                AddEnvObjects(chunk, grassWidth, height, grassStart, m_levelTheme.m_grass);
                //If a straight section has ended - adds trees, rocks, and bushes.
                if (ground.y != start.y || ground.x != start.x + width)
                {
                    AddEnvObjects(chunk, width, height, start, m_levelTheme.m_bushes, addTrees ? m_levelTheme.m_trees : null);

                    start = grassStart = ground;
                    width = grassWidth = 1;
                }
                //if a section of grass has ended - resets the grass and continues
                else
                {
                    width++;
                    grassWidth = 0;
                }
            }
        }
        //adds vegetation to the last straight section
        AddEnvObjects(chunk, grassWidth, height, grassStart, m_levelTheme.m_grass);
        AddEnvObjects(chunk, width, height, start, m_levelTheme.m_bushes, addTrees ? m_levelTheme.m_trees : null);
    }

    /// <summary>
    /// Adds bushes and optionally trees to the objects of the chunk
    /// </summary>
    private void AddEnvObjects(Chunk chunk, int width, int height, Vector3Int start,
        EnviromentObject[] bushes, EnviromentObject[] trees = null)
    {
        foreach (var obj in AddVegetation(width, height, start, bushes))
            chunk.AddEnviromentObject(obj.gameObject);

        if (trees != null)
            foreach (var obj in AddVegetation(width, height, start, trees))
                chunk.AddEnviromentObject(obj.gameObject);
    }
    /// <summary>
    /// Adds vegetation to a straight piece of the ground
    /// </summary>
    /// <param name="width">width of the straight section</param>
    /// <param name="height">max height of the vegetation</param>
    /// <param name="start">start of the straight section</param>
    /// <param name="vegs">array of vegetation objects</param>
    /// <returns></returns>
    protected List<EnviromentObject> AddVegetation(int width, int height, Vector3Int start, EnviromentObject[] vegs)
    {
        //list of creatd plants
        List<EnviromentObject> objs = new List<EnviromentObject>();
        if (width == 0)
            return objs;
        //tries of generation
        int tries = width + 2;
        //total width of all created plants on the section
        float length = 0;
        while (tries >= 0)
        {
            //creates a random plant from an array
            EnviromentObject obj = Object.Instantiate(vegs[Random.Range(0, vegs.Length)], start, Quaternion.identity).GetComponent<EnviromentObject>();
            //random position in the section
            Vector3 pos = new Vector3(Random.Range(start.x + obj.GetRightBorder(), start.x + width + obj.GetLeftBorder()), start.y);
            obj.transform.position = pos + obj.GetOffset();
            // if an obj intersects with another created object by more than 1/3 of its width
            bool collides = objs.Any(o => o.transform.position.x > obj.transform.position.x &&
                obj.transform.position.x + obj.GetRightBorder() - o.transform.position.x - o.GetLeftBorder() > obj.GetWidth() / 3 ||
                o.transform.position.x < obj.transform.position.x &&
                o.transform.position.x + o.GetRightBorder() - obj.transform.position.x - obj.GetLeftBorder() > obj.GetWidth() / 3);
            //if the plant:
            //- the plant is higher than the maximum height
            //- intersects with another created object by more than 1/3 of its width
            //- with a probability of 35%, the total width of all created plants on the section is more than half the width of the section
            //- the plant goes beyond the section
            //then it starts generation anew and deletes the created object, as it does not fit
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
    /// The number of a random enemy, depending on its chance of creation
    /// </summary>
    /// <returns></returns>
    int GetEnemyNum()
    {
        float[] chances = new float[m_levelTheme.m_enemies.Length];
        //adds the chance of spawnig each enemy to the list
        for (int i = 0; i < m_levelTheme.m_enemies.Length; i++)
        {
            WalkEnemy enemy = m_levelTheme.m_enemies[i].gameObject.GetComponent<WalkEnemy>();
            m_container.Inject(enemy);
            chances[i] = enemy.GetSpawnChance();
        }

        return m_lvlBuilder.GetWeightedIndex(chances);
    }
    /// <summary>
    /// Creats an object in the section
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="sectionWidth">width of the section</param>
    /// <param name="height">max height of traps</param>
    /// <param name="startPos">start of the section</param>
    /// <param name="lastEnemy">last enemy created on the section</param>
    protected void SpawnObjectInTheSection(Chunk chunk, int sectionWidth, int height, Vector3 startPos, ref WalkEnemy lastEnemy)
    {
        if (sectionWidth <= 0)
            return;

        switch (m_spawnManager.ChooseSpawnObject(m_jumper, m_catsLeft,
            !m_shopSpawned && m_UI.GetMoney() >= m_shop.GetLowestPrice() && sectionWidth + m_rightOffset > m_levelTheme.m_shop.GetWidth(),
            sectionWidth > m_minEnemyWidth && m_enemiesPerChunk > 0,
            m_trapsPerChunk > 0))
        {
            //spawn a cat
            case 0:
                chunk.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_cat, new Vector3(startPos.x + (sectionWidth - m_levelTheme.m_cat.GetWidth()) / 2, startPos.y), Quaternion.identity, null));
                m_catsLeft--;
                m_catsSpawned++;
                break;
            //spawn the shop
            case 1:
                chunk.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_shop, new Vector3(Random.Range(startPos.x, startPos.x + sectionWidth + m_rightOffset - m_levelTheme.m_shop.GetWidth() + 1), startPos.y), Quaternion.identity, null));
                m_shopSpawned = true;
                break;
            //spawn an enemy
            case 2:
                SpawnValues enemy = m_levelTheme.m_enemies[GetEnemyNum()];
                Vector3 pos = new Vector3(startPos.x + (sectionWidth - enemy.GetWidth()) / 2, startPos.y);
                lastEnemy = m_container.InstantiatePrefabForComponent<WalkEnemy>(enemy, pos, Quaternion.identity, null);
                chunk.AddEnviromentObject(lastEnemy.gameObject);
                m_enemiesPerChunk--;
                break;
            //spawn traps
            case 3:
                List<Trap> traps = new List<Trap>();
                while (traps.Count == 0)
                {
                    Trap trap = m_levelTheme.m_floorTraps[Random.Range(0, m_levelTheme.m_floorTraps.Length)];
                    m_container.Inject(trap);
                    trap.SetTrapNum();
                    //if the trap is too high or wide - tries again
                    if (trap.GetWidth() > sectionWidth || trap.GetHeight() > height)
                    {
                        continue;
                    }
                    //borders of the section to create traps
                    float rightBorder = startPos.x + sectionWidth + m_rightOffset;
                    float leftBorder = startPos.x + m_leftOffset;
                    //number of traps in the section
                    m_trapsNum = Random.Range(1, (int)((rightBorder - leftBorder) / (trap.GetWidth() + m_playerWidth)) + 1);


                    if (trap.GetAttackDirection() == Vector3.right || trap.GetAttackDirection() == Vector3.forward)
                    {
                        if (trap.GetWidth() > sectionWidth / 3)
                        {
                            m_trapsNum = 1;
                        }
                        // if the trap is shooting - crop the border
                        if (trap.GetAttackDirection() == Vector3.forward)
                        {
                            rightBorder -= sectionWidth / 2;

                        }
                        else
                        {
                            leftBorder += sectionWidth / 2;
                        }
                    }
                    //if the trap is serial, make a series of traps in a row
                    if (trap.IsSeries())
                    {
                        //recalculates the possible number of traps
                        m_trapsNum = Random.Range(1, (int)Mathf.Clamp(-(trap.GetHeight() - m_playerJumpHeight) * m_playerJumpWidth * 1.0f / m_playerJumpHeight, 1, (rightBorder - trap.GetRightBorder() - leftBorder + trap.GetLeftBorder()) / trap.GetWidth()));
                        pos = new Vector3(Random.Range(leftBorder - trap.GetLeftBorder(), rightBorder - trap.GetRightBorder() - m_trapsNum * trap.GetWidth()), startPos.y);
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
                    chunk.AddEnviromentObject(trap.gameObject);
                }
                m_trapsPerChunk--;
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// Creates a trap within the borders and triggers a creation to the right and left of itself
    /// </summary>
    /// <param name="leftBorder"></param>
    /// <param name="rightBorder"></param>
    /// <param name="posY"></param>
    /// <param name="trapType">trap type</param>
    /// <param name="traps">all traps</param>
    void SpawnTrap(float leftBorder, float rightBorder, float posY, Trap trapType, List<Trap> traps)
    {
        //if all traps are instantiated or the section is too small - exit
        if (m_trapsNum == 0 || trapType.GetWidth() >= rightBorder - leftBorder)
            return;
        //random position on the section
        float posX = Random.Range(leftBorder - trapType.GetLeftBorder(), rightBorder - trapType.GetRightBorder());
        m_trapsNum--;
        Trap trap = m_container.InstantiatePrefabForComponent<Trap>(trapType, new Vector3(posX, posY), Quaternion.identity, null);
        trap.SetTrap(trapType.TrapNumber);
        traps.Add(trap);
        //starts creation on the right and left
        SpawnTrap(leftBorder, posX + trap.GetLeftBorder() - m_playerWidth, posY, trapType, traps);
        SpawnTrap(posX + trap.GetRightBorder() + m_playerWidth, rightBorder, posY, trapType, traps);
    }

}
