using System.Collections.Generic;
using UnityEngine;

public class GridStrategy : FillStrategy
{
    protected int m_maxRoomSize = 50;
    protected int m_minRoomSize = 20;

    protected new int m_minTransitionWidth = 4;
    protected new int m_maxTransitionWidth = 10;
    protected int m_minTransitionHeight = 11;
    protected new int m_maxTransitionHeight = 25;

    readonly int m_minWidth = 1;
    readonly int m_maxWidth = 6;
    readonly int m_minDist = 3;

    readonly int m_maxAttempts = 3;
    readonly int m_platformMaxAttempts = 100;

    public GridStrategy(LevelTheme levelTheme) : base(levelTheme)
    {
    }

    public override Room FillRoom(Room prevRoom, FillStrategy transitionStrategy)
    {
        Room transition = new Room(prevRoom.GetEndPosition(), prevRoom.GetEndPosition());

        int width = Random.Range(m_minRoomSize, m_maxRoomSize);
        int height = Random.Range(m_minRoomSize, m_maxRoomSize);
        if (Random.value > 0.7f)
        {
            height = -height;
        }
        Vector3Int start = prevRoom.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Room room = new Room(start, end, transition);
        int attempts = 0;

        while (!MakeGrid(room))
        {
            attempts++;
            room.ClearGrid();
            if (attempts >= m_maxAttempts)
            {
                Debug.Log("Grid failed");
                return null;
            }
        }
        prevRoom.GetNextTransition().Clear(m_editor);

        // create bounds for player's fall
        room.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));

        CreateSideBound(room, height < 0);

        prevRoom.AddTransition(transition);
        room.AddTransition(new Room(end,end));
        room.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(room, groundTiles, int.MaxValue, false));
        return room;
    }

    public override Room FillTransition(Room room)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(m_minTransitionHeight, m_maxTransitionHeight);
        Vector3Int start = room.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Room transition = new Room(start, end);

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
        // create bounds for player's fall
        transition.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));

        return transition;
    }

    bool MakeGrid(Room room)
    {
        Vector3Int start = room.GetStartPosition();
        Vector3Int end = room.GetEndPosition();
        Vector3Int lastPoint = start;
        Vector3Int lastOffset = start;
        int attempts = 0;
        int lastWidth = 0;
        int w1 = 0;
        int platformsOffsetY = 0;

        while (attempts < m_platformMaxAttempts)
        {
            // Check if we've reached the end
            if ((lastPoint.x + lastWidth >= end.x - m_minWidth) &&
                (Mathf.Abs(lastPoint.y - end.y) <= m_playerJumpHeight))
            {
                return true;
            }

            // Calculate next platform position
            int offsetY = (lastPoint.y > end.y ? -1 : 1) * Random.Range(m_minDist, m_playerJumpHeight);
            float progress = Mathf.Abs(offsetY) * 1.0f / Mathf.Max(1, Mathf.Abs(end.y - lastPoint.y));
            int x = lastPoint.x + lastWidth + (int)(progress * (end.x - lastPoint.x - lastWidth));

            // Calculate horizontal offset
            int jumpWidth = lastPoint.y < end.y ? GetJumpWidth(offsetY) : m_playerJumpWidth;
            int offsetX;

            if (x - lastPoint.x - lastWidth < jumpWidth)
            {
                offsetX = Random.Range(-jumpWidth, jumpWidth);
            }
            else
            {
                int rangeStart = jumpWidth - x + lastPoint.x + lastWidth + m_minWidth;
                int rangeEnd = -lastPoint.x + x - jumpWidth - m_minWidth;
                offsetX = Random.Range(rangeStart, rangeEnd);
            }

            offsetX = Mathf.Clamp(offsetX, start.x - x + m_minWidth, end.x - x - m_minWidth * 2);

            Vector3Int pos = CheckSurroundings(room, new Vector3Int(x + offsetX, lastPoint.y + offsetY), end);

            // Validate platform position
            if (Mathf.Abs(lastPoint.x + lastWidth - pos.x) > GetJumpWidth(offsetY) ||
                !AvailablePlatform(room, lastPoint.y > lastOffset.y && end.y - start.y > 0 ? lastPoint.y : lastOffset.y,
                                 pos, GetMaxWidth(room, pos), ref lastWidth, end) ||
                pos.x > end.x - m_minWidth * 2 || pos.x <= start.x)
            {
                attempts++;
                continue;
            }

            // Create main platform
            lastWidth = Mathf.Clamp(lastWidth, m_minWidth, end.x - pos.x - m_minWidth);
            lastPoint = pos;
            room.CreatePlatform(lastPoint, lastWidth);

            // Create secondary platform
            platformsOffsetY = (offsetX * (end.y - start.y) >= 0 ? -1 : 1) * Random.Range(m_minDist - 1, m_playerJumpHeight);
            bool offsetDirection = (offsetX * (end.y - start.y) >= 0);

            int offsetX1;
            if (offsetX > 0)
            {
                int min = offsetDirection ? Mathf.Clamp(-m_minDist + 1, lastOffset.x + w1 - lastPoint.x + m_minWidth, 0) : -m_minDist + 1;
                offsetX1 = Random.Range(min, GetJumpWidth(platformsOffsetY) + lastWidth);
            }
            else
            {
                int min = offsetDirection ? Mathf.Clamp(-GetJumpWidth(platformsOffsetY) - m_minWidth,
                                                     lastOffset.x + w1 - lastPoint.x + m_minWidth, 0) :
                                         -GetJumpWidth(platformsOffsetY) - m_minWidth;
                offsetX1 = Random.Range(min, m_minDist);
            }

            offsetX1 = Mathf.Clamp(offsetX1, start.x - lastPoint.x + m_minWidth, end.x - lastPoint.x - m_minWidth * 2);
            pos = CheckSurroundings(room, new Vector3Int(lastPoint.x + offsetX1, lastPoint.y + platformsOffsetY), end);

            if (!AvailablePlatform(room, lastPoint.y > lastOffset.y && end.y - start.y > 0 ? lastPoint.y : lastOffset.y,
                                pos, GetMaxWidth(room, pos), ref w1, end) ||
                pos.x > end.x - m_minWidth || pos.x <= start.x)
            {
                platformsOffsetY = lastOffset.y;
                attempts++;
                continue;
            }

            // Create secondary platform
            w1 = Mathf.Clamp(w1, m_minWidth, end.x - pos.x - m_minWidth);
            room.CreatePlatform(pos, w1);
            lastOffset = pos;
            attempts = 0; // Reset attempts after successful placement
        }

        return false;
    }

    bool AvailablePlatform(Room room, int lastPointY, Vector3Int currentPoint, int maxWidth, ref int currentWidth, Vector3Int end)
    {
        if (maxWidth < m_minWidth) return false;

        for (int i = 0; i < m_playerJumpHeight; i++)
        {
            if (CheckVerticalCollision(room, currentPoint, i)) return false;

            if (room.PositionIsUsed(new Vector3Int(currentPoint.x, currentPoint.y + i)))
            {
                return HandlePlatformWidth(room, currentPoint, maxWidth, ref currentWidth, end, i);
            }

            if (CheckHorizontalCollision(room, currentPoint, maxWidth, i, ref currentWidth, end))
            {
                return true;
            }
        }

        currentWidth = Random.Range(m_minWidth, maxWidth);
        return true;
    }

    // Helper methods for AvailablePlatform
    private bool CheckVerticalCollision(Room room, Vector3Int point, int offset)
    {
        return room.PositionIsUsed(new Vector3Int(point.x, point.y - offset)) &&
               !room.PositionIsUsed(new Vector3Int(point.x - 1, point.y - offset)) &&
               !room.PositionIsUsed(new Vector3Int(point.x + 1, point.y - offset));
    }

    private bool HandlePlatformWidth(Room room, Vector3Int point, int maxWidth, ref int width, Vector3Int end, int offset)
    {
        for (int j = 1; j < maxWidth; j++)
        {
            if (!room.PositionIsUsed(new Vector3Int(point.x + j, point.y + offset)))
            {
                if (point.x + j + 1 > end.x - m_minWidth || j + 1 == Mathf.Clamp(maxWidth, 0, end.x - point.x) - 1)
                    return false;

                width = Random.Range(j + 1, Mathf.Clamp(maxWidth, 0, end.x - point.x));
                return true;
            }
        }
        return false;
    }

    private bool CheckHorizontalCollision(Room room, Vector3Int point, int maxWidth, int verticalOffset, ref int width, Vector3Int end)
    {
        for (int j = 0; j < maxWidth; j++)
        {
            if (room.PositionIsUsed(new Vector3Int(point.x + j, point.y - verticalOffset)))
            {
                for (int k = 1; k <= maxWidth - j; k++)
                {
                    if (!room.PositionIsUsed(new Vector3Int(point.x + j + k, point.y - verticalOffset)))
                    {
                        width = Random.Range(m_minWidth, Mathf.Clamp(j + k, 0, end.x - point.x));
                        return true;
                    }
                }
            }
        }
        return false;
    }

    int GetMaxWidth(Room room, Vector3Int pos)
    {
        for (int i = 0; i < m_maxWidth; i++)
        {
            if (CheckImmediateCollision(room, pos, i) || CheckDistanceCollision(room, pos, i))
            {
                return i;
            }
        }
        return m_maxWidth;
    }

    // Helper methods for GetMaxWidth
    private bool CheckImmediateCollision(Room room, Vector3Int pos, int offset)
    {
        return room.PositionIsUsed(new Vector3Int(pos.x + offset + 1, pos.y + 1)) ||
               room.PositionIsUsed(new Vector3Int(pos.x + offset + 1, pos.y - 1));
    }

    private bool CheckDistanceCollision(Room room, Vector3Int pos, int offset)
    {
        for (int j = 0; j < m_minDist; j++)
        {
            if (room.PositionIsUsed(new Vector3Int(pos.x + offset + j, pos.y)) ||
                room.PositionIsUsed(new Vector3Int(pos.x + offset, pos.y - j)) ||
                room.PositionIsUsed(new Vector3Int(pos.x + offset, pos.y + j)))
            {
                return true;
            }
        }
        return false;
    }
    Vector3Int CheckSurroundings(Room room, Vector3Int pos, Vector3Int end)
    {
        // First check diagonal collisions
        if (HasDiagonalCollision(room, pos))
            return end;

        // Check minimum distances (1 horizontal, 2 vertical)
        if (!CheckMinimumDistances(room, pos))
            return end;

        // Check all 8 surrounding corners
        Vector3Int[] cornerOffsets = new Vector3Int[]
        {
        new Vector3Int(1, 1, 0),    // Top-right
        new Vector3Int(-1, 1, 0),   // Top-left
        new Vector3Int(1, -1, 0),   // Bottom-right
        new Vector3Int(-1, -1, 0),  // Bottom-left
        new Vector3Int(1, 0, 0),    // Right
        new Vector3Int(-1, 0, 0),   // Left
        new Vector3Int(0, 1, 0),    // Top
        new Vector3Int(0, -1, 0)    // Bottom
        };

        foreach (Vector3Int offset in cornerOffsets)
        {
            if (room.PositionIsUsed(pos + offset))
                return end;
        }

        // If we get here, position is valid
        return pos;
    }

    bool CheckMinimumDistances(Room room, Vector3Int pos)
    {
        // Check horizontal clearance (1 unit)
        for (int x = -2; x <= 2; x++)
        {

            for (int y = -2; y <= 2; y++)
            {
                if (room.PositionIsUsed(new Vector3Int(pos.x + x, pos.y + y)))
                    return false;
            }
        }

        return true;
    }

    bool HasDiagonalCollision(Room room, Vector3Int pos)
    {
        // Your original diagonal collision check
        return (room.PositionIsUsed(new Vector3Int(pos.x - 1, pos.y - 1)) &&
               room.PositionIsUsed(new Vector3Int(pos.x + 1, pos.y + 1))) ||
               (room.PositionIsUsed(new Vector3Int(pos.x - 1, pos.y + 1)) &&
               room.PositionIsUsed(new Vector3Int(pos.x + 1, pos.y - 1)));
    }
}