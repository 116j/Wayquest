using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class FillStrategy
{
    protected int m_maxChunkWidth = 60;
    protected int m_minChunkWidth = 12;
    //Стандартная высота прямого участка
    protected int m_chunkHeight = 6;
    protected int m_finalChunkHeight = 15;
    protected int m_finalChunkWidth = 45;

    protected int m_minTransitionWidth = 2;
    protected int m_maxTransitionWidth = 30;
    protected int m_maxTransitionHeight = 15;

    protected int m_minElevationHeight = 2;
    protected int m_maxElevationHeight = 20;
    //Минимальная ширина прямого участка
    protected readonly int m_minStraightSection = 6;
    //Минимальная ширина участка, по которому может ходить враг
    protected readonly int m_minEnemyWidth = 6;
    protected readonly int m_maxSlopeHeight = 7;

    //Макс количество врагов на чанке
    int m_enemiesPerChunk;
    //Макс количество ловушек на чанке
    int m_trapsPerChunk;
    //Сколько кошек нужно создать в данный момент, чтобы полностью восполнить здоровье игрока
    int m_catsLeft;
    //Сколько кошек сейчас создано в игре
    int m_catsSpawned;
    int m_trapsNum;
    bool m_jumper = false;
    //Отступ для создания ловушек, когда есть батут или платформа
    protected float m_rightOffset;
    //Отступ для создания ловушек для приземления игрока
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

    //Вероятность генерации холма
    protected float m_slopeChance = 0.7f;
    //Вероятность генерации батута
    protected float m_jumperChance = 0.4f;
    //Вероятность создания магазина
    protected float m_shopChance = 0.6f;

    //Длина прыжка игрока на одной высоте
    protected int m_playerJumpWidth = 9;
    //Высота прыжка игрока
    protected int m_playerJumpHeight = 6;
    //Ширина модели игрока
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
    /// <summary>
    /// Устанавливает для игрока тройной прыжок
    /// </summary>
    public void SetTripleJump()
    {
        m_playerJumpHeight = 8;
        m_playerJumpWidth = 13;

    }
    /// <summary>
    /// Создает возвышенности и низменности для чанка, добавляет ландшавт и отрисовывает тайлы
    /// </summary>
    /// <param name="prevChunk">предыдущий чанк</param>
    /// <param name="transitionStrategy">стратегия построения перехода на следующий чанк</param>
    /// <returns>filled chunk</returns>
    public virtual Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //рисует тайлы перехода от предыдущего чанка к этому
        prevChunk.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevChunk.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevChunk.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minChunkWidth, m_maxChunkWidth), start.y);
        //ширина начпльного прямого участка
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        Chunk chunk = new Chunk(end, startWidth, prevChunk.GetNextTransition());

        m_enemiesPerChunk = (int)m_enemiesCount.Evaluate(m_lvlBuilder.LevelProgress());
        m_trapsPerChunk = (int)m_trapsCount.Evaluate(m_lvlBuilder.LevelProgress());
        //сколько создать кошек в зависимости от потерянного здоровья игрока и существующих кошек
        m_catsLeft = m_UI.AllHerats - m_UI.CurrentHearts - m_catsSpawned;

        //высота следующего участка
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        m_leftOffset = m_playerWidth * 1.5f;
        CreateElevationsAndLowlands(chunk, start + startWidth * Vector3Int.right, startWidth, height, true);
        chunk.AddTransition(transitionStrategy.FillTransition(chunk));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, true));

        return chunk;
    }
    /// <summary>
    /// Создает переход для чанка
    /// </summary>
    /// <param name="chunk"></param>
    public virtual Chunk FillTransition(Chunk chunk)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(-m_maxTransitionHeight, Mathf.Min(width * m_playerJumpHeight / 3, m_maxTransitionHeight));
        Vector3Int end = new Vector3Int(chunk.GetEndPosition().x + width, chunk.GetEndPosition().y + height);
        Chunk transition = new Chunk(chunk.GetEndPosition(), end);

        //если ширина и высота перехода слишком большие для прыжка игрока - создает выступы
        if (width > m_playerJumpWidth || height > m_playerJumpHeight || Mathf.Abs(height) > GetJumpHeight(width))
        {
            int gapHeight, gapWidth;
            Vector3Int lastPoint = transition.GetStartPosition();
            while (lastPoint.x < end.x - 3)
            {
                //макс ширина для промежутка между предыдущей точкой и концом перехода 
                int maxGapWidth = GetMaxWidthGapForJump(lastPoint, end);
                //ширина промежутка
                gapWidth = Random.Range(m_minTransitionWidth, Mathf.Clamp(maxGapWidth, m_minTransitionWidth, Mathf.Min(m_playerJumpWidth, end.x - lastPoint.x - 2)));
                //высота промежутка между высотой от ширины промежутка до высотой от макс ширины промежутка
                // height between the point of the local width and the max width on the straight line between the end of the transition and the current point 
                gapHeight = Random.Range(GetGapHeightInDiagonalWidth(lastPoint, end, gapWidth), GetGapHeightInDiagonalWidth(lastPoint, end, maxGapWidth));
                if (height < 0)
                {
                    gapHeight = -gapHeight;
                }
                lastPoint = new Vector3Int(lastPoint.x + gapWidth, lastPoint.y + gapHeight);
                transition.CreateLedge(lastPoint);
            }
        }
        //создает границу для падения игрока
        transition.AddEnviromentObject(CreateHorizontalBounds(transition.GetStartPosition(), end, width, height));

        return transition;
    }
    /// <summary>
    /// Макс ширина промежутка, на который может прыгнуть игрок от текущей точки до конечной
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    protected int GetMaxWidthGapForJump(Vector3Int currentPos, Vector3Int end)
    {
        return (int)(m_playerJumpHeight * 1.0f / (Mathf.Abs(end.y - currentPos.y) * 1.0f / (end.x - currentPos.x) + m_playerJumpHeight * 1.0f / m_playerJumpWidth));
    }
    /// <summary>
    /// Высота промежутка для конкретной ширины на диагонали между текущей позицией и концом
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
    /// Ширина прыжка игрока в зависимости от высоты прыжка
    /// </summary>
    /// <param name="height"></param>
    /// <returns></returns>
    protected int GetJumpWidth(int height)
    {
        return Mathf.CeilToInt(-(Mathf.Abs(height) - m_playerJumpHeight) * 1.0f / m_playerJumpHeight * m_playerJumpWidth);
    }
    /// <summary>
    /// Высота прыжка игрока в зависимости от ширины прыжка
    /// </summary>
    /// <param name="width"></param>
    /// <returns></returns>
    protected int GetJumpHeight(int width)
    {
        return Mathf.CeilToInt(-m_playerJumpHeight * 1.0f / m_playerJumpWidth * width + m_playerJumpHeight);
    }

    /// <summary>
    /// Создает стратовый чанк мз начальной позиции
    /// </summary>
    /// <param name="start">начало чанка</param>
    /// <param name="transitionStrategy">стратегия для создания перехода между этим и след чанками</param>
    /// <returns>filled chunk</returns>
    public Chunk FillStratChunk(Vector3Int start, FillStrategy transitionStrategy)
    {
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minChunkWidth, m_maxChunkWidth), start.y);
        Chunk chunk = new Chunk(start, end, new Chunk(start, start));
        //ширина начального прямого участка
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        //создаем полигон с шириной начального прямого участка
        chunk.MakePolygon(startWidth, start);
        //высота следующего участка
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        CreateElevationsAndLowlands(chunk, start + startWidth * Vector3Int.right, startWidth, height, false);
        chunk.AddTransition(transitionStrategy.FillTransition(chunk));
        //граница слева, чтобы нельзя было пройти, т.к. там ничего нет
        chunk.AddEnviromentObject(CreateVerticalBounds(start));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, true), isInitial: true);

        return chunk;
    }
    /// <summary>
    /// Создает горизантальную границу для падения игрока, которая переносит его в начало или конец чанка
    /// </summary>
    /// <param name="start">начало границы</param>
    /// <param name="end">конец границы</param>
    /// <param name="width">ширина чанка или перехода</param>
    /// <param name="height">высота чанка или переходв</param>
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
    /// Создает вертикальную границу, через которую нельзя пройти, 
    /// обычно в начале чанка
    /// </summary>
    /// <param name="pos">расположение границцы</param>
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
    /// Создает боковые границы под чанками для падения игрока, которая переносит его в начало или конец чанка
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="isLeft">если граница слева</param>
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
    /// Создает последний чанк с боссом
    /// </summary>
    /// <param name="prevChunk"></param>
    /// <returns></returns>
    public Chunk FillFinalChunk(Chunk prevChunk)
    {
        prevChunk.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevChunk.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevChunk.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + m_minStraightSection * 2 + m_finalChunkWidth, start.y);
        Chunk chunk = new Chunk(end, m_minStraightSection, prevChunk.GetNextTransition());

        //начало чанка
        chunk.CreateElevationOrLowland(-m_finalChunkHeight, m_finalChunkWidth, start + m_minStraightSection * Vector3Int.right);
        //низменность, где ходит босс
        chunk.CreateElevationOrLowland(m_finalChunkHeight, m_minStraightSection, start + new Vector3Int(m_minStraightSection + m_finalChunkWidth, -m_finalChunkHeight));
        //срздает босса
        chunk.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_boss, new Vector3(start.x + (m_minStraightSection + m_finalChunkWidth - m_levelTheme.m_boss.GetWidth()) / 2, start.y - m_finalChunkHeight), Quaternion.identity, null));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, true));
        return chunk;
    }
    /// <summary>
    /// Создает низменности, возвышенности и холмы для чанк, добавляет врагов и ловушки
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="lastPoint">последняя точка прямого участка</param>
    /// <param name="spawnEnemyOrTrap">создавать ли ловушки и врагов</param>
    protected void CreateElevationsAndLowlands(Chunk chunk, Vector3Int lastPoint, int startWidth, int height, bool spawnEnemyOrTrap)
    {
        //ширина участка
        int width = startWidth;
        //враг на участке
        WalkEnemy lastEnemy = null;
        while (chunk.GetEndPosition().x - lastPoint.x > m_minStraightSection)
        {
            //если slopeChance и оставшаяся дистанция достаточна для генерации холма
            if (Random.value > m_slopeChance && m_minElevationHeight * 2 + m_minStraightSection + lastPoint.x <= chunk.GetEndPosition().x - m_minStraightSection)
            {
                //сбрасывает правый отступ, т.к. нет изменения высоты
                m_rightOffset = 0f;
                m_jumper = false;
                //создает врагов и ловушки на предыдущем участке, если надо
                if (spawnEnemyOrTrap)
                    SpawnEnemyOrTrap(chunk, width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
                //сбрасывает левый отступ, т.к. нет изменения высоты и предыдущий участок заполнен
                m_leftOffset = 0f;
                int slopeHeight = Random.Range(m_minElevationHeight, Mathf.Clamp((chunk.GetEndPosition().x - m_minStraightSection * 2 - lastPoint.x - 1) / 2, m_minElevationHeight, m_maxSlopeHeight));
                width = Random.Range(m_minStraightSection, chunk.GetEndPosition().x - m_minStraightSection - slopeHeight * 2 - lastPoint.x - 1);
                chunk.CreateSlope(slopeHeight, width, lastPoint);
                // создает ловушки и врагов нв холме если надо
                if (spawnEnemyOrTrap)
                    SpawnEnemyOrTrap(chunk, width, int.MaxValue, new Vector3(lastPoint.x + slopeHeight + 1, lastPoint.y + slopeHeight), ref lastEnemy);
                //точка после холма
                lastPoint = new Vector3Int(lastPoint.x + slopeHeight * 2 + width + m_minStraightSection + 1, lastPoint.y);
                //ширина участка после холма
                width = m_minStraightSection;
            }
            else
            {
                //создает врагов и ловушки на предыдущем участке, если надо
                if (spawnEnemyOrTrap)
                    SpawnEnemyOrTrap(chunk, width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
                //ширина нового участка
                width = Random.Range(m_minStraightSection, chunk.GetEndPosition().x - lastPoint.x);
                chunk.CreateElevationOrLowland(height, width, lastPoint);
                //создает платформу или батут в конце предыдущего участка, если высота нового участка выше прыжка игрока
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
                //отступ в начале для игрока
                m_leftOffset = m_playerWidth * 1.5f;
                //обновляем позицию на начало нового участка
                lastPoint = new Vector3Int(lastPoint.x + width, lastPoint.y + height);
            }
            //сбрасывает врага на участке
            lastEnemy = null;
            //высота следующего нового участка
            height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
            //устанавливает отступ в конце участка в зависимости от высоты нового
            SetRightOffset(height);
        }
        //добавляет оставшиеся тапйлы
        chunk.AddTiles(m_chunkHeight, chunk.GetEndPosition().x - lastPoint.x, lastPoint);
        m_rightOffset = -m_playerWidth;
        m_jumper = false;
        //добавляет врагов и ловушки на оставшиеся тайлы, если надо
        if (spawnEnemyOrTrap)
            SpawnEnemyOrTrap(chunk, chunk.GetEndPosition().x - lastPoint.x + width, int.MaxValue, lastPoint - Vector3Int.right * width, ref lastEnemy);
    }
    /// <summary>
    /// Определяет отступ справа на участке в зависимости от высототы следующего участка
    /// Если следующий участок - возвышенность, то решает, будет ли платформа или батут
    /// </summary>
    /// <param name="height">высота следующего участка</param>
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
    /// Добавляет ландшафт на землю чанка
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="height">макс высота растительности</param>
    /// <param name="addTrees">добавлять ли деревья</param>
    protected void AddLandscape(Chunk chunk, HashSet<Vector3Int> groundTiles, int height, bool addTrees)
    {
        int width = 0;
        //
        int grassWidth = 0;
        Vector3Int start = groundTiles.FirstOrDefault();
        Vector3Int grassStart = start;
        foreach (var ground in groundTiles)
        {
            //если добавлен тайл с травой - не добавлять траву
            //идет по прямому участку земли и считаем количество тайлов
            if (!m_editor.AddGrass(ground) && ground.y == start.y && ground.x == start.x + width)
            {
                if (grassWidth == 0)
                {
                    grassStart = ground;
                }
                width++;
                grassWidth++;
            }
            //если прямой участок земли закончился - добавить растительность
            else if (grassWidth > 0 || ground.y != start.y || ground.x != start.x + width)
            {
                //добавляем траву
                AddEnvObjects(chunk, grassWidth, height, grassStart, m_levelTheme.m_grass);
                //если закончился прямой участок - добавляет деревья, камни и кусты
                if (ground.y != start.y || ground.x != start.x + width)
                {
                    AddEnvObjects(chunk, width, height, start, m_levelTheme.m_bushes, addTrees ? m_levelTheme.m_trees : null);

                    start = grassStart = ground;
                    width = grassWidth = 1;
                }
                //если оборвался участок с травой - обнуляет траву и продолжаем
                else
                {
                    width++;
                    grassWidth = 0;
                }
            }
        }
        //добавляет растительность на последний прямой участок
        AddEnvObjects(chunk, grassWidth, height, grassStart, m_levelTheme.m_grass);
        AddEnvObjects(chunk, width, height, start, m_levelTheme.m_bushes, addTrees ? m_levelTheme.m_trees : null);
    }

    /// <summary>
    /// Добавляет кусты и опционально деревья в объектыы чанка
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
    /// Добавляет растительность на прямой участок земли
    /// </summary>
    /// <param name="width">ширина прямого участка</param>
    /// <param name="height">макс высота растительности</param>
    /// <param name="start">начало прямого участка</param>
    /// <param name="vegs">массив объектов растительности</param>
    /// <returns></returns>
    protected List<EnviromentObject> AddVegetation(int width, int height, Vector3Int start, EnviromentObject[] vegs)
    {
        //лист созданных растений
        List<EnviromentObject> objs = new List<EnviromentObject>();
        if (width == 0)
            return objs;
        // попытки генерации
        int tries = width + 2;
        //общая ширина всех созданных растений на участке
        float length = 0;
        while (tries >= 0)
        {
            //создает рандомное растение из массива
            EnviromentObject obj = Object.Instantiate(vegs[Random.Range(0, vegs.Length)], start, Quaternion.identity).GetComponent<EnviromentObject>();
            //рандомная позиция на участке
            Vector3 pos = new Vector3(Random.Range(start.x + obj.GetRightBorder(), start.x + width + obj.GetLeftBorder()), start.y);
            obj.transform.position = pos + obj.GetOffset();
            // если obj пересекается с другим созданным объектом больше, чем на 1/3 своей ширины
            bool collides = objs.Any(o => o.transform.position.x > obj.transform.position.x &&
                obj.transform.position.x + obj.GetRightBorder() - o.transform.position.x - o.GetLeftBorder() > obj.GetWidth() / 3 ||
                o.transform.position.x < obj.transform.position.x &&
                o.transform.position.x + o.GetRightBorder() - obj.transform.position.x - obj.GetLeftBorder() > obj.GetWidth() / 3);
            //если растение:
            //- растение выше макс высоты 
            //- пересекается с другим созданным объектом больше, чем на 1/3 своей ширины
            //- с вероятностью 35% общая ширина всех созданных растений на участке больше половины ширины растения
            //- растение выходит за рамки участка
            //тогда запускает генераци. заново и удаляемсозданный объект, так как он не подходит
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
    /// Номер рандомного врага в зависимости от его шанса создания
    /// </summary>
    /// <returns>номер врага в списке возможных врагов</returns>
    int GetEnemyNum()
    {
        List<float> chances = new List<float>();
        //добавляет шанс создания каждого врага в список
        foreach (var obj in m_levelTheme.m_enemies)
        {
            WalkEnemy enemy = obj.gameObject.GetComponent<WalkEnemy>();
            m_container.Inject(enemy);
            chances.Add(enemy.GetSpawnChance());
        }
        //рандомное число между 0 и суммой шансов всех врагов
        float value = Random.Range(0, chances.Sum());
        float sum = 0;
        //идет по списку шансов, пока сумма шансов не будет больше, чем value
        for (int i = 0; i < chances.Count; i++)
        {
            sum += chances[i];
            if (value < sum)
            {
                return i;
            }
        }
        //если сумма меньше чисел, то возвращает последнее
        return chances.Count - 1;
    }
    /// <summary>
    /// Создает врага или ловушку
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="sectionWidth">ширина прямого участка</param>
    /// <param name="height">макс высота ловушки</param>
    /// <param name="startPos">начало участка</param>
    /// <param name="lastEnemy">последний созданный враг на участке</param>
    protected void SpawnEnemyOrTrap(Chunk chunk, int sectionWidth, int height, Vector3 startPos, ref WalkEnemy lastEnemy)
    {
        if (sectionWidth <= 0)
            return;
        //если нужно создать кошку, нет в конце батута, нет созданных кошек или мало созданных кошек
        if (m_catsLeft > 0 && Random.value < m_catsLeft / (float)m_UI.AllHerats && !m_jumper)
        {
            chunk.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_cat, new Vector3(startPos.x + (sectionWidth - m_levelTheme.m_cat.GetWidth()) / 2, startPos.y), Quaternion.identity, null));
            m_catsLeft--;
            m_catsSpawned++;
        }
        //если не создан магазин, денег у игрока достаточно для самой маленькой покупки, достаточно место и попалает в вероятность создания магазина
        else if (!m_shopSpawned && m_UI.GetMoney() >= m_shop.GetLowestPrice() && sectionWidth + m_rightOffset > m_levelTheme.m_shop.GetWidth() && Random.value > m_shopChance)
        {
            chunk.AddEnviromentObject(m_container.InstantiatePrefab(m_levelTheme.m_shop, new Vector3(Random.Range(startPos.x, startPos.x + sectionWidth + m_rightOffset - m_levelTheme.m_shop.GetWidth() + 1), startPos.y), Quaternion.identity, null));
            m_shopSpawned = true;
        }
        else if (!m_jumper && sectionWidth > m_minEnemyWidth && m_enemiesPerChunk > 0 && Random.value < (m_enemiesPerChunk / m_enemiesCount.Evaluate(m_lvlBuilder.LevelProgress())))
        {
            SpawnValues enemy = m_levelTheme.m_enemies[GetEnemyNum()];
            Vector3 pos = new Vector3(startPos.x + (sectionWidth - enemy.GetWidth()) / 2, startPos.y);
            lastEnemy = m_container.InstantiatePrefabForComponent<WalkEnemy>(enemy, pos, Quaternion.identity, null);
            chunk.AddEnviromentObject(lastEnemy.gameObject);
            m_enemiesPerChunk--;
        }
        //если еще можно поставить ловушки на чанке и относительно мало ловушек
        else if (m_trapsPerChunk > 0 && Random.value < (m_trapsPerChunk / m_trapsCount.Evaluate(m_lvlBuilder.LevelProgress())))
        {
            List<Trap> traps = new List<Trap>();
            while (traps.Count == 0)
            {
                Trap trap = m_levelTheme.m_floorTraps[Random.Range(0, m_levelTheme.m_floorTraps.Length)];
                m_container.Inject(trap);
                trap.SetTrapNum();
                //если ловушка слишком высокая или широкая - пробует заново
                if (trap.GetWidth() > sectionWidth || trap.GetHeight() > height)
                {
                    continue;
                }
                //границы участка для создания ловушки
                float rightBorder = startPos.x + sectionWidth + m_rightOffset;
                float leftBorder = startPos.x + m_leftOffset;
                //количество ловушек на участке
                m_trapsNum = Random.Range(1, (int)((rightBorder - leftBorder) / (trap.GetWidth() + m_playerWidth)) + 1);


                if (trap.GetAttackDirection() == Vector3.right || trap.GetAttackDirection() == Vector3.forward)
                {
                    // если ловушка стреляет - обрезать границу
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
                //если ловушка серийная - сделать серию подряд ловушек
                if (trap.IsSeries())
                {
                    //пересчитывает возможное количество ловушек
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
                chunk.AddEnviromentObject(trap.gameObject);
            }
            m_trapsPerChunk--;
        }
    }
    /// <summary>
    /// Создает ловушку в пределах границ и запускает создание справа и слева от себя
    /// </summary>
    /// <param name="leftBorder">левая граница</param>
    /// <param name="rightBorder">правая граница</param>
    /// <param name="posY"></param>
    /// <param name="trap">титп ловушки</param>
    /// <param name="traps">лист всех ловушек</param>
    void SpawnTrap(float leftBorder, float rightBorder, float posY, Trap trap, List<Trap> traps)
    {
        //если все ловушки созданы или учвасток слишком мал - выход
        if (m_trapsNum == 0 || trap.GetWidth() >= rightBorder - leftBorder)
            return;
        //рандомная позиция на участке
        float posX = Random.Range(leftBorder - trap.GetLeftBorder(), rightBorder - trap.GetRightBorder());
        m_trapsNum--;
        traps.Add(m_container.InstantiatePrefabForComponent<Trap>(trap, new Vector3(posX, posY), Quaternion.identity, null));
        //запускает создание справа и слева
        SpawnTrap(leftBorder, posX + trap.GetLeftBorder() - m_playerWidth, posY, trap, traps);
        SpawnTrap(posX + trap.GetRightBorder() + m_playerWidth, rightBorder, posY, trap, traps);
    }
}
