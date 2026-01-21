using System.Collections.Generic;
using UnityEngine;

public class DestroyableBrickStrategy : FillStrategy
{
    protected int m_maxChunkSize = 50;
    protected int m_minChunkSize = 20;

    protected new int m_maxTransitionHeight = 30;
    protected int m_minTransitionHeight = 11;
    protected int m_transitionWidth = 4;

    readonly int m_tunnelTimerZone = 9;
    readonly int m_tunnelMinZone = 6;
    readonly int m_tunnelSafeZone = 4;

    readonly int m_minStepWidth = 2;
    readonly int m_maxStepWidth = 5;
    readonly int m_maxStarirsOffsetX = 4;
    readonly int m_minStarirsOffset = 1;

    readonly DestroyableBrick m_brick;

    public DestroyableBrickStrategy(LevelTheme levelTheme, DestroyableBrick destroyableBrick) : base(levelTheme)
    {
        m_brick = destroyableBrick;
    }
    /// <summary>
    /// Создает чанк с разрушающимся потолком и отрисовывет тайлы
    /// </summary>
    /// <param name="prevChunk">предыдущий чанк</param>
    /// <param name="transitionStrategy"></param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //очищает переход на этот чанк, он не нужен
        prevChunk.GetNextTransition().Clear(m_editor);
        //пустой перход для предыдущего чанка
        Chunk transition = new Chunk(prevChunk.GetEndPosition(), prevChunk.GetEndPosition());

        Vector3Int start = prevChunk.GetEndPosition();
        int width = Random.Range(m_minChunkSize, m_maxChunkSize);
        int height = Random.Range(-m_minChunkSize, m_minChunkSize);

        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk chunk = new Chunk(start, end, transition);
        //рандомный тип разрушающегося пола
        int fillType = Random.Range(0, 4);

        switch (fillType)
        {
            case 0:
                CollapseStaircase(chunk);
                if (height != 0)
                {
                    CreateSideBound(chunk, true);
                    CreateSideBound(chunk, false);
                }
                break;
            case 1:
                ResonanceCorridor(chunk, height);
                CreateSideBound(chunk, height < 0);
                break;

            case 2:
                CollapseTunnel(chunk);
                break;

            case 3:
                WaveOfCollapse(chunk, height);
                CreateSideBound(chunk, height < 0);
                break;
        }

        end = chunk.GetEndPosition();
        start = chunk.GetStartPosition();
        //создает границу для падения игрока
        chunk.AddEnviromentObject(CreateHorizontalBounds(start, end, end.x - start.x, end.y - start.y));
        //пустой перход
        chunk.AddTransition(new Chunk(end, end));
        //заменяет пререход от предыдущего чанка к этому пустым
        prevChunk.AddTransition(transition);

        return chunk;
    }
    /// <summary>
    /// Создает лесенку из кирпичей, состоящую из групп разрушающихся по таймеру кирпичей и обычных кирпичей между группами
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="chunkHeight">Вверх лестница или вниз</param>
    void WaveOfCollapse(Chunk chunk, int chunkHeight)
    {
        Vector3Int currentPos = chunk.GetStartPosition() + Vector3Int.left;
        Vector3Int end = chunk.GetEndPosition();
        List<DestroyableBrick> group;

        while (currentPos.x < end.x)
        {
            int maxWidth = GetMaxWidthGapForJump(currentPos, end);
            //количество кирпичей в группе
            int tilesCount = Random.Range(1, maxWidth + 1);
            //высота участка группы
            int height = Mathf.Min(Random.Range(GetGapHeightInDiagonalWidth(currentPos, end, tilesCount + 1), GetGapHeightInDiagonalWidth(currentPos, end, maxWidth)), tilesCount + 1);
            group = new List<DestroyableBrick>(tilesCount);
            Vector3Int offset = Vector3Int.zero;
            for (int i = 1; i <= tilesCount; i++)
            {
                offset += new Vector3Int(1, ((height - offset.y) * chunkHeight > 0 ? 1 : -1) / (tilesCount + 1 - i));
                CreateBrick(chunk, currentPos + offset, BrickBehaviour.Timer, group);
            }
            currentPos += new Vector3Int(tilesCount + 1, height * chunkHeight > 0 ? 1 : -1);
            if (currentPos.x < end.x)
            {
                CreateBrick(chunk, currentPos, BrickBehaviour.None);
            }
        }
        //обновляет конеци чанка
        chunk.SetEndPosition(currentPos);
    }
    /// <summary>
    /// Создает две лесенки, состоящие из кирпичей, стоящих через один
    /// верхняя состоит из кирпичей, которые разрушаются при уходе с кирпича,
    /// нижняя состоит из кирпичей, которые разрушаются по таймеру после насткпания на кирпич
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="chunkHeight">Вверх лестница или вниз</param>
    void ResonanceCorridor(Chunk chunk, int chunkHeight)
    {
        Vector3Int currentPos = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        //отступ нижней лестницы
        int lineOffset = GetJumpHeight(2);

        while (currentPos.x < end.x)
        {
            //верхний кирпич
            CreateBrick(chunk, currentPos, BrickBehaviour.OnExit, new List<DestroyableBrick>(1));
            //сдвигаем на следующую клетку
            currentPos += new Vector3Int(1, Mathf.Min(GetGapHeightInDiagonalWidth(currentPos, end, 1), 1) * (chunkHeight > 0 ? 1 : -1));
            //нижний кирпич, сдвинут на 1 вправо по сравнению с верхним
            CreateBrick(chunk, currentPos + Vector3Int.down * lineOffset, BrickBehaviour.Timer, new List<DestroyableBrick>(1));
            if (currentPos.x != end.x)
            {
                currentPos += new Vector3Int(1, Mathf.Min(GetGapHeightInDiagonalWidth(currentPos, end, 1), 1) * (chunkHeight > 0 ? 1 : -1));
            }
            else
            {
                currentPos += new Vector3Int(1, 0);
            }
        }
        //обновляет начало и конец чанка
        chunk.SetStartPosition(chunk.GetStartPosition() + Vector3Int.down * lineOffset);
        chunk.SetEndPosition(currentPos);
    }
    /// <summary>
    /// Создает платформы из групп кирпичей 3х типов на 3х уровнях высоты, по которым нужно прыгать
    /// </summary>
    /// <param name="chunk"></param>
    void CollapseStaircase(Chunk chunk)
    {
        Vector3Int currentPos = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        List<DestroyableBrick> group;

        int verticalGap = GetJumpHeight(3);
        //ширина платформы
        int platformWidth = Random.Range(m_minStepWidth, m_maxStepWidth);
        int offsetX = Random.Range(m_minStarirsOffset, m_maxStarirsOffsetX);
        int offsetY = Random.Range(m_minStarirsOffset, GetJumpHeight(offsetX));
        //уровень платформы
        int level = 0;

        while (currentPos.x < end.x)
        {
            switch (level)
            {
                case 0:
                    for (int i = 0; i < platformWidth; i++)
                    {
                        CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.OnExit, new List<DestroyableBrick>(1));
                    }
                    currentPos += new Vector3Int(platformWidth + offsetX, verticalGap + offsetY);
                    break;
                case 1:
                    group = new List<DestroyableBrick>(platformWidth);
                    for (int i = 0; i < platformWidth; i++)
                    {
                        CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.Timer, group);
                    }
                    currentPos += new Vector3Int(platformWidth + offsetX, -verticalGap);
                    break;
                case 2:
                    for (int i = 0; i < platformWidth; i++)
                    {
                        CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.OnEnter, new List<DestroyableBrick>(1));
                    }
                    currentPos += new Vector3Int(platformWidth + offsetX, -offsetY);
                    break;
            }

            level = (level + 1) % 3;
        }
        //обновляет конеци чанка
        chunk.SetEndPosition(currentPos);
    }
    /// <summary>
    /// Создает ровную прямую кирпичей, чередующую группы простых кирпичей и разрушающихся по таймеру
    /// </summary>
    /// <param name="chunk"></param>
    void CollapseTunnel(Chunk chunk)
    {
        Vector3Int currentPos = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        List<DestroyableBrick> group;

        while (currentPos.x < end.x)
        {
            //ширина группы таймер кирпичей
            int timerZone = Random.Range(m_tunnelMinZone, m_tunnelTimerZone);
            group = new List<DestroyableBrick>(timerZone);
            for (int i = 0; i < timerZone; i++)
            {
                CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.Timer, group);
            }
            currentPos += Vector3Int.right * timerZone;
            if (currentPos.x < end.x)
            {
                //ширина группы простых кирпичей
                int safeZone = Random.Range(m_tunnelSafeZone, m_tunnelMinZone);
                for (int i = 0; i < safeZone; i++)
                {
                    CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.None);
                }
                currentPos += Vector3Int.right * safeZone;
            }
        }
        //обновляет конеци чанка
        chunk.SetEndPosition(currentPos);
    }

    void CreateBrick(Chunk chunk, Vector3Int pos, BrickBehaviour behaviour, List<DestroyableBrick> group = null)
    {
        DestroyableBrick brick = Object.Instantiate(m_brick, pos, Quaternion.identity);
        brick.SetBrickBehaviour(behaviour, m_levelTheme.m_themeNum, group);
        chunk.CreatePlatform(pos, 1);
        chunk.AddEnviromentObject(brick.gameObject);
    }
    /// <summary>
    /// Создает переход из разрушающихся по таймеру кирпичей, по которым нужно прыгать наверх
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public override Chunk FillTransition(Chunk chunk)
    {
        int width = m_transitionWidth;
        int height = Random.Range(m_minTransitionHeight, m_maxTransitionHeight);
        Vector3Int start = chunk.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk transition = new Chunk(start, end);
        //не отрисовывает тайлы, т.к. создаем их тут
        transition.DontFillTiles();

        Vector3Int lastPoint = start + Vector3Int.right;
        int vertOffset = Random.Range(m_playerJumpHeight / 2 + 1, GetJumpHeight(1));
        bool posOffset = true;
        do
        {
            CreateBrick(transition, lastPoint, BrickBehaviour.Timer, new List<DestroyableBrick>(1));
            lastPoint += new Vector3Int((posOffset ? 1 : -1), vertOffset);
            posOffset = !posOffset;
        }
        while (lastPoint.y < end.y);
        //создает границу для падения игрока
        transition.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));
        return transition;
    }
}
