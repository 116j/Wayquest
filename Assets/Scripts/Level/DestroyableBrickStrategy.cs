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

    readonly float m_chunkRespawnTime = 5f;

    readonly DestroyableBrick m_brick;

    public DestroyableBrickStrategy(LevelTheme levelTheme, DestroyableBrick destroyableBrick) : base(levelTheme)
    {
        m_brick = destroyableBrick;
    }
    /// <summary>
    /// Creates a chunk with a collapsing floor
    /// </summary>
    /// <param name="prevChunk">previous chunk</param>
    /// <param name="transitionStrategy">strategy for building a transition to the next chunk</param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //clears the transition to this chunk
        prevChunk.GetNextTransition().Clear(m_editor);
        //empty transition for the previous chunk
        Chunk transition = new Chunk(prevChunk.GetEndPosition(), prevChunk.GetEndPosition());

        Vector3Int start = prevChunk.GetEndPosition();
        int width = Random.Range(m_minChunkSize, m_maxChunkSize);
        int height = Random.Range(-m_minChunkSize, m_minChunkSize);

        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk chunk = new Chunk(start, end, transition);
        //random type of collapsing floor
        int fillType = Random.Range(0, 4);

        switch (fillType)
        {
            case 0:
                CollapseStaircase(chunk);
                //if (height != 0)
                //{
                //    CreateSideBound(chunk, true);
                //    CreateSideBound(chunk, false);
                //}
                break;
            case 1:
                ResonanceCorridor(chunk, height);
                //CreateSideBound(chunk, height < 0);
                break;

            case 2:
                CollapseTunnel(chunk);
                break;

            case 3:
                WaveOfCollapse(chunk, height);
                //CreateSideBound(chunk, height < 0);
                break;
        }

        end = chunk.GetEndPosition();
        start = chunk.GetStartPosition();
        //creates a bound for the player to fall
        chunk.AddEnviromentObject(CreateHorizontalBounds(start, end, end.x - start.x, end.y - start.y));
        //empty transition
        chunk.AddTransition(new Chunk(end, end));
        //replaces the transition from the previous chunk to this one with an empty one
        prevChunk.AddTransition(transition);

        return chunk;
    }
    /// <summary>
    /// Creates a ladder of bricks consisting of groups of timer-collapsing bricks and regular bricks between the groups
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="chunkHeight">descending or ascending</param>
    void WaveOfCollapse(Chunk chunk, int chunkHeight)
    {
        Vector3Int currentPos = chunk.GetStartPosition() + Vector3Int.left;
        Vector3Int end = chunk.GetEndPosition();
        List<DestroyableBrick> group;

        while (currentPos.x < end.x)
        {
            int maxWidth = GetMaxWidthGapForJump(currentPos, end);
            //number of bricks in a group
            int tilesCount = Random.Range(1, maxWidth + 1);
            //height of the group section
            int height = Mathf.Min(Random.Range(GetGapHeightInDiagonalWidth(currentPos, end, tilesCount + 1), GetGapHeightInDiagonalWidth(currentPos, end, maxWidth)), tilesCount + 1);
            group = new List<DestroyableBrick>(tilesCount);
            Vector3Int offset = Vector3Int.zero;
            for (int i = 1; i <= tilesCount; i++)
            {
                offset += new Vector3Int(1, ((height - offset.y) * chunkHeight > 0 ? 1 : -1) / (tilesCount + 1 - i));
                CreateBrick(chunk, currentPos + offset, BrickBehaviour.Timer, group, true, m_chunkRespawnTime);
            }
            currentPos += new Vector3Int(tilesCount + 1, height * chunkHeight > 0 ? 1 : -1);
            if (currentPos.x < end.x)
            {
                CreateBrick(chunk, currentPos, BrickBehaviour.None);
            }
        }
        //updates the end of the chunk
        chunk.SetEndPosition(currentPos);
    }
    /// <summary>
    /// Creates two ladders consisting of bricks standing through one
    /// the upper one consists of bricks that collapse when leaving the brick,
    /// the lower one consists of bricks, which are destroyed by a timer after hitting the brick
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="chunkHeight">descending or ascending</param>
    void ResonanceCorridor(Chunk chunk, int chunkHeight)
    {
        Vector3Int currentPos = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        //the lower ladder offset
        int lineOffset = GetJumpHeight(2);

        while (currentPos.x < end.x)
        {
            //upper brick
            CreateBrick(chunk, currentPos, BrickBehaviour.OnExit, new List<DestroyableBrick>(1), true, m_chunkRespawnTime);
            //moves to the next cell
            currentPos += new Vector3Int(1, Mathf.Min(GetGapHeightInDiagonalWidth(currentPos, end, 1), 1) * (chunkHeight > 0 ? 1 : -1));
            //lower brick is shifted 1 to the right compared to the upper one
            CreateBrick(chunk, currentPos + Vector3Int.down * lineOffset, BrickBehaviour.Timer, new List<DestroyableBrick>(1), true, m_chunkRespawnTime);
            if (currentPos.x != end.x)
            {
                currentPos += new Vector3Int(1, Mathf.Min(GetGapHeightInDiagonalWidth(currentPos, end, 1), 1) * (chunkHeight > 0 ? 1 : -1));
            }
            else
            {
                currentPos += new Vector3Int(1, 0);
            }
        }
        //updates the end and the start of the chunk
        chunk.SetStartPosition(chunk.GetStartPosition() + Vector3Int.down * lineOffset);
        chunk.SetEndPosition(currentPos);
    }
    /// <summary>
    /// Creates platforms from groups of 3 types of bricks at 3 height levels to jump on
    /// </summary>
    /// <param name="chunk"></param>
    void CollapseStaircase(Chunk chunk)
    {
        Vector3Int currentPos = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        List<DestroyableBrick> group;

        int verticalGap = GetJumpHeight(3);
        //width of the platform
        int platformWidth = Random.Range(m_minStepWidth, m_maxStepWidth);
        int offsetX = Random.Range(m_minStarirsOffset, m_maxStarirsOffsetX);
        int offsetY = Random.Range(m_minStarirsOffset, GetJumpHeight(offsetX));
        //level of the platform
        int level = 0;

        while (currentPos.x < end.x)
        {
            switch (level)
            {
                case 0:
                    for (int i = 0; i < platformWidth; i++)
                    {
                        CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.OnExit, new List<DestroyableBrick>(1), true, m_chunkRespawnTime);
                    }
                    currentPos += new Vector3Int(platformWidth + offsetX, verticalGap + offsetY);
                    break;
                case 1:
                    group = new List<DestroyableBrick>(platformWidth);
                    for (int i = 0; i < platformWidth; i++)
                    {
                        CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.Timer, group, true, m_chunkRespawnTime);
                    }
                    currentPos += new Vector3Int(platformWidth + offsetX, -verticalGap);
                    break;
                case 2:
                    for (int i = 0; i < platformWidth; i++)
                    {
                        CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.OnEnter, new List<DestroyableBrick>(1), true, m_chunkRespawnTime);
                    }
                    currentPos += new Vector3Int(platformWidth + offsetX, -offsetY);
                    break;
            }

            level = (level + 1) % 3;
        }
        //updates the end of the chunk
        chunk.SetEndPosition(new Vector3Int(currentPos.x, chunk.GetStartPosition().y));
    }
    /// <summary>
    /// Creates a straight line of bricks alternating between groups of simple bricks and collapsing ones by timer
    /// </summary>
    /// <param name="chunk"></param>
    void CollapseTunnel(Chunk chunk)
    {
        Vector3Int currentPos = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        List<DestroyableBrick> group;

        while (currentPos.x < end.x)
        {
            //width of the timer brick group
            int timerZone = Random.Range(m_tunnelMinZone, m_tunnelTimerZone);
            group = new List<DestroyableBrick>(timerZone);
            for (int i = 0; i < timerZone; i++)
            {
                CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.Timer, group, true, m_chunkRespawnTime);
            }
            currentPos += Vector3Int.right * timerZone;
            if (currentPos.x < end.x)
            {
                //width of the simple brick group
                int safeZone = Random.Range(m_tunnelSafeZone, m_tunnelMinZone);
                for (int i = 0; i < safeZone; i++)
                {
                    CreateBrick(chunk, currentPos + Vector3Int.right * i, BrickBehaviour.None);
                }
                currentPos += Vector3Int.right * safeZone;
            }
        }
        //updates the end of the chunk
        chunk.SetEndPosition(currentPos);
    }

    void CreateBrick(Chunk chunk, Vector3Int pos, BrickBehaviour behaviour, List<DestroyableBrick> group = null, bool respawn = false, float respawnTime = 2.5f)
    {
        DestroyableBrick brick = Object.Instantiate(m_brick, pos, Quaternion.identity);
        brick.SetBrickBehaviour(behaviour, m_levelTheme.m_themeNum, group, respawn, respawnTime);
        chunk.CreatePlatform(pos, 1);
        chunk.AddEnviromentObject(brick.gameObject);
    }
    /// <summary>
    /// Creates a transition of timer-collapsing bricks that you need to jump up
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
        //does not render tiles
        transition.DontFillTiles();

        Vector3Int lastPoint = start + Vector3Int.right;
        int vertOffset = Random.Range(m_playerJumpHeight / 2 + 1, GetJumpHeight(1));
        bool posOffset = true;
        do
        {
            CreateBrick(transition, lastPoint, BrickBehaviour.Timer, new List<DestroyableBrick>(1), true);
            lastPoint += new Vector3Int((posOffset ? 1 : -1), vertOffset);
            posOffset = !posOffset;
        }
        while (lastPoint.y < end.y);
        //creates a bound for the player to fall
        transition.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));
        return transition;
    }
}
