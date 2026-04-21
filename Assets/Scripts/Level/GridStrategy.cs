using System.Collections.Generic;
using UnityEngine;

public class GridStrategy : FillStrategy
{
    protected int m_maxChunkSize = 50;
    protected int m_minChunkSize = 20;

    protected new int m_minTransitionWidth = 4;
    protected new int m_maxTransitionWidth = 10;
    protected int m_minTransitionHeight = 11;
    protected new int m_maxTransitionHeight = 25;

    //Min platform width
    readonly int m_minWidth = 1;
    //Max platform width
    readonly int m_maxWidth = 6;
    //Min distance between platforms
    readonly int m_minDist = 3;

    //Max number of attempts to generate a chunk
    readonly int m_maxAttempts = 3;
    //Max number of attempts to generate a platform
    readonly int m_platformMaxAttempts = 100;

    readonly float m_negativeChunkHeightChance = 0.7f;

    public GridStrategy(LevelTheme levelTheme) : base(levelTheme)
    {
    }
    /// <summary>
    /// Creates a chunk consisting of pltforms leading up or down which the player needs to jump,
    /// adds a landscape and draws tiles
    /// </summary>
    /// <param name="prevChunk">previous chunk</param>
    /// <param name="transitionStrategy">strategy for building a transition to the next chunk</param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //draws transition tiles from the previous chunk to this one
        Chunk transition = new Chunk(prevChunk.GetEndPosition(), prevChunk.GetEndPosition());

        int width = Random.Range(m_minChunkSize, m_maxChunkSize);
        int height = Random.Range(m_minChunkSize, m_maxChunkSize);
        if (Random.value > m_negativeChunkHeightChance)
        {
            height = -height;
        }
        Vector3Int start = prevChunk.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk chunk = new Chunk(start, end, transition);
        int attempts = 0;
        //generates the m_maxAttempts level once, if it doesn't work, it tries a different strategy
        while (!MakeGrid(chunk))
        {
            attempts++;
            chunk.ClearGrid();
            if (attempts >= m_maxAttempts)
            {
                Debug.Log("Grid failed");
                return null;
            }
        }
        prevChunk.GetNextTransition().Clear(m_editor);

        //creates bound for the player to fall
        chunk.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));

        // CreateSideBound(chunk, height < 0);

        prevChunk.AddTransition(transition);
        chunk.AddTransition(new Chunk(end, end));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, false));
        return chunk;
    }
    /// <summary>
    ///     Creates a transition to the top from small platforms
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public override Chunk FillTransition(Chunk chunk)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(m_minTransitionHeight, m_maxTransitionHeight);
        Vector3Int start = chunk.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk transition = new Chunk(start, end);

        Vector3Int lastPoint = start + Vector3Int.right;
        int platformWidth = (width - 2) / 2;
        int horOffset = width - 2 - platformWidth * 2;
        int vertOffset = Random.Range(m_minDist, GetJumpHeight(horOffset));
        bool posOffset = true;
        do
        {
            transition.CreatePlatform(lastPoint, platformWidth);
            lastPoint += new Vector3Int((posOffset ? 1 : -1) * (platformWidth + horOffset), vertOffset);
            posOffset = !posOffset;
        }
        while (lastPoint.y < end.y);
        //creates bound for the player to fall
        transition.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));

        return transition;
    }
    /// <summary>
    /// Creates a grid of platforms
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns>was it possible to create a grid of platforms</returns>
    bool MakeGrid(Chunk chunk)
    {
        Vector3Int start = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        Vector3Int lastPoint = start;
        //position of the last second platform
        Vector3Int lastOffset = start;
        int chunkHeight = end.y - start.y;
        int attempts = 0;
        int lastWidth = 0;
        int w1 = 0;
        int secondaryOffsetY;

        while (attempts < m_platformMaxAttempts)
        {
            //checks if reached the end
            if ((lastPoint.x + lastWidth >= end.x - m_minWidth) &&
                (Mathf.Abs(lastPoint.y - end.y) <= m_playerJumpHeight))
            {
                return true;
            }

            //vertical platform offset
            int offsetY = (lastPoint.y > end.y ? -1 : 1) * Random.Range(m_minDist, m_playerJumpHeight);
            //platform offset compared to the remaining height
            float progress = Mathf.Abs(offsetY) * 1.0f / Mathf.Max(1, Mathf.Abs(end.y - lastPoint.y));
            //possible X position
            int x = lastPoint.x + lastWidth + (int)(progress * (end.x - lastPoint.x - lastWidth));

            //длина прыжка игрока на новую платформу в зависимости от высоты вертикального отступа
            int jumpWidth = lastPoint.y < end.y ? GetJumpWidth(offsetY) : m_playerJumpWidth;
            int offsetX = x - lastPoint.x - lastWidth;
            //if the distance between the platforms is less than the jump width
            if (offsetX < jumpWidth)
            {
                offsetX = Random.Range(-jumpWidth, jumpWidth);
            }
            else
            {   
                int minOffset = jumpWidth - offsetX + m_minWidth;
                int maxOffset = offsetX - m_minWidth;
                offsetX = Random.Range(minOffset, maxOffset);
            }
            //cuts off the horizontal offset along the borders of the chunk
            offsetX = Mathf.Clamp(offsetX, start.x - x + m_minWidth, end.x - x - m_minWidth * 2);
            //checks the surroundings for collisions with platforms
            Vector3Int pos = new Vector3Int(x + offsetX, lastPoint.y + offsetY);

            //checks the platform for viability and selecting the platform width
            if (!CheckSurroundings(chunk, pos) || Mathf.Abs(lastPoint.x + lastWidth - pos.x) > GetJumpWidth(offsetY) ||
                !AvailablePlatformWidth(chunk, pos, GetMaxWidth(chunk, pos), ref lastWidth, end) ||
                pos.x > end.x - m_minWidth * 2 || pos.x <= start.x)
            {
                attempts++;
                continue;
            }

            //second platform
            lastWidth = Mathf.Clamp(lastWidth, m_minWidth, end.x - pos.x - m_minWidth);
            lastPoint = pos;
            chunk.CreatePlatform(lastPoint, lastWidth);

            //if the horizontal offset sign of the platform and the height sign of the chunk match
            bool offsetDirection = offsetX * chunkHeight >= 0;
            secondaryOffsetY = (offsetDirection ? -1 : 1) * Random.Range(m_minDist - 1, m_playerJumpHeight);

            int secondaryOffsetX;
            if (offsetX > 0)
            {
                //with a positive offset, it more tends to be the right offset
                int min = offsetDirection ? Mathf.Clamp(-m_minDist + 1, lastOffset.x + w1 - lastPoint.x + m_minWidth, 0) : -m_minDist + 1;
                secondaryOffsetX = Random.Range(min, GetJumpWidth(secondaryOffsetY) + lastWidth);
            }
            else
            {
                //with a negative offset, it more tends to be the right offset
                int min = offsetDirection ? Mathf.Clamp(-GetJumpWidth(secondaryOffsetY) - m_minWidth,
                                            lastOffset.x + w1 - lastPoint.x + m_minWidth, 0) :
                                            -GetJumpWidth(secondaryOffsetY) - m_minWidth;
                secondaryOffsetX = Random.Range(min, m_minDist);
            }
            //cuts off the horizontal offset along the borders of the chunk
            secondaryOffsetX = Mathf.Clamp(secondaryOffsetX, start.x - lastPoint.x + m_minWidth, end.x - lastPoint.x - m_minWidth * 2);
            //проверяем окрестности на стокновение с платформами
            pos = new Vector3Int(lastPoint.x + secondaryOffsetX, lastPoint.y + secondaryOffsetY);

            //checks the platform for viability and selecting the platform width
            if (!CheckSurroundings(chunk, pos) || !AvailablePlatformWidth(chunk, pos, GetMaxWidth(chunk, pos), ref w1, end) ||
                pos.x > end.x - m_minWidth || pos.x <= start.x)
            {
                attempts++;
                continue;
            }   

            //width of the second platform
            w1 = Mathf.Clamp(w1, m_minWidth, end.x - pos.x - m_minWidth);
            chunk.CreatePlatform(pos, w1);
            lastOffset = pos;
            //resets attempts after successful generation
            attempts = 0;
        }

        return false;
    }
    /// <summary>
    /// Checks whether the platform can be placed and selects the width for it
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="currentPos">start of the platform</param>
    /// <param name="maxWidth">max platform width</param>
    /// <param name="currentWidth">variable for width</param>
    /// <param name="end">end of the chunk</param>
    /// <returns>is the platform suitable and has the width been found for it</returns>
    bool AvailablePlatformWidth(Chunk chunk, Vector3Int currentPos, int maxWidth, ref int currentWidth, Vector3Int end)
    {
        if (maxWidth < m_minWidth) return false;

        for (int i = 0; i < m_playerJumpHeight; i++)
        {
            //if the platform overlaps the platform from below at a jump distance - not suitable
            if (CheckVerticalCollision(chunk, currentPos, i))
                return false;
            //if there is another platform at the distance of the lower offset - selects the width based on it.
            if (chunk.PositionIsUsed(new Vector3Int(currentPos.x, currentPos.y + i)))
                return SelectPlatformWidthWithOffset(chunk, currentPos, maxWidth, ref currentWidth, end, i);
            //if there is a platform at the distance of the maxWidth width and the lower offset - tries to choose the width based on it
            if (CheckHorizontalCollision(chunk, currentPos, maxWidth, i, ref currentWidth, end))
                return true;
        }

        currentWidth = Random.Range(m_minWidth, maxWidth);
        return true;
    }
    /// <summary>
    /// Selects the width of the platform so that it overlaps the other platform on the right
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">start of the platform</param>
    /// <param name="maxWidth">max platform width</param>
    /// <param name="width">variable for width</param>
    /// <param name="end">end of the chunk</param>
    /// <param name="offset">vertical offset where the other platform is located</param>
    /// <returns>was it possible to get the width</returns>
    bool SelectPlatformWidthWithOffset(Chunk chunk, Vector3Int pos, int maxWidth, ref int width, Vector3Int end, int offset)
    {
        for (int i = m_minWidth; i < maxWidth; i++)
        {
            //когда другая платформа закончится
            if (!chunk.PositionIsUsed(new Vector3Int(pos.x + i, pos.y + offset)))
            {
                //если слишком мало места до конца или длина больше макс длины - не подходит
                if (pos.x + i + 1 > end.x - m_minWidth || i + 1 == Mathf.Clamp(maxWidth, 0, end.x - pos.x) - 1)
                    return false;
                //платформа должна быть длиннее нижней млатформы
                width = Random.Range(i + 1, Mathf.Clamp(maxWidth, 0, end.x - pos.x));
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Looks for the presence of a platform under the lower offset at a distance of the max width
    /// and selects the width depending on the platform, if there is a platform
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">start of the platform</param>
    /// <param name="maxWidth">max platform width</param>
    /// <param name="verticalOffset"></param>
    /// <param name="width">variable for width</param>
    /// <param name="end">end of the chunk</param>
    /// <returns>if the width is determined based on the found platform</returns>
    bool CheckHorizontalCollision(Chunk chunk, Vector3Int pos, int maxWidth, int verticalOffset, ref int width, Vector3Int end)
    {
        for (int i = 0; i < maxWidth; i++)
        {
            //if there is a platform whith the offset at a given width
            if (chunk.PositionIsUsed(new Vector3Int(pos.x + i, pos.y - verticalOffset)))
            {
                for (int j = m_minWidth; j <= maxWidth - i; j++)
                {
                    //checks when the platform ends
                    if (!chunk.PositionIsUsed(new Vector3Int(pos.x + i + j, pos.y - verticalOffset)))
                    {
                        width = Random.Range(m_minWidth, Mathf.Clamp(i + j, 0, end.x - pos.x));
                        return true;
                    }
                }
            }
        }
        return false;
    }
    /// <summary>
    /// Checks whether a platform overlaps another platform with an offset 
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">start of the platform</param>
    /// <param name="offset">platform vertical offset</param>
    /// <returns></returns>
    bool CheckVerticalCollision(Chunk chunk, Vector3Int pos, int offset)
    {
        return chunk.PositionIsUsed(new Vector3Int(pos.x, pos.y - offset)) &&
               !chunk.PositionIsUsed(new Vector3Int(pos.x - 1, pos.y - offset)) &&
               !chunk.PositionIsUsed(new Vector3Int(pos.x + 1, pos.y - offset));
    }
    /// <summary>
    /// Defines the max width for the platform
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">start of the platform</param>
    /// <returns></returns>
    int GetMaxWidth(Chunk chunk, Vector3Int pos)
    {
        for (int i = 0; i < m_maxWidth; i++)
        {
            //if there are intersections at a given width - max width
            if (CheckInterferingPlarforms(chunk, pos, i))
                return i;
        }
        return m_maxWidth;
    }
    /// <summary>
    /// It looks for a certain width (offset + 1), whether there are other platforms in the radius
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">start of the platform</param>
    /// <param name="offset">horizontal offset - intended end of the platform</param>
    /// <returns></returns>
    bool CheckInterferingPlarforms(Chunk chunk, Vector3Int pos, int offset)
    {
        for (int i = 0; i < m_minDist; i++)
        {
            //if there is a platform at the top, right, or bottom of the distance i for a given width 
            if (chunk.PositionIsUsed(new Vector3Int(pos.x + offset + i, pos.y)) ||
                chunk.PositionIsUsed(new Vector3Int(pos.x + offset, pos.y - i)) ||
                chunk.PositionIsUsed(new Vector3Int(pos.x + offset, pos.y + i)))
            {
                return true;
            }
        }
        //if there is another platform adjacent to the platform within a radius of 1 on the right
        return chunk.PositionIsUsed(new Vector3Int(pos.x + offset + 1, pos.y + 1)) ||
               chunk.PositionIsUsed(new Vector3Int(pos.x + offset + 1, pos.y - 1));
    }
    /// <summary>
    /// Checks for another platforms around
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">start of the platform</param>
    /// <returns>true - there are no other platforms around</returns>
    bool CheckSurroundings(Chunk chunk, Vector3Int pos)
    {
        for (int x = -m_minDist + 1; x < m_minDist; x++)
        {
            for (int y = -m_minDist + 1; y < m_minDist; y++)
            {
                if (chunk.PositionIsUsed(new Vector3Int(pos.x + x, pos.y + y)))
                    return false;
            }
        }
        return true;
    }
}