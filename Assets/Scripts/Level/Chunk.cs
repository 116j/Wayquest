using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chunk
{
    protected Vector3Int m_startPosition;
    protected Vector3Int m_endPosition;

    List<Polygon> m_polygons = new List<Polygon>();
    List<GameObject> m_enviroment = new List<GameObject>();
    Chunk m_prevTransition;
    Chunk m_nextTransition;

    //Last position of the additionally added section width
    Vector3Int m_lastExtraWidthPoint;
    //X coordinate of the start of the last section
    int m_lastSectionPoint;
    //The lowest Y coordinate of the chunk
    int m_lowestPoint;
    //Y coordinate of the start of the camera bounds
    int m_cameraBoundsStart;
    int m_chunkHeight = 0;

    //Height of the left transition's bound
    int m_transitionLeftBoundHeight;
    //Height of the right transition's bound
    int m_transitionRightBoundHeight;
    readonly int m_minHeight = 6;
    readonly int m_minWidth = 12;

    bool m_tilesFilled = false;
    /// <summary>
    /// Adds the initial polygon, adds tiles, if necessary, so that the end of the chunk is not visible.
    /// </summary>
    /// <param name="end">end of the chunk</param>
    /// <param name="startwWidth">width of the initial straight section</param>
    /// <param name="transition">transition between this and the previous chunk</param>
    public Chunk(Vector3Int end, int startwWidth, Chunk transition)
    {
        m_endPosition = end;
        m_prevTransition = transition;
        m_startPosition = m_prevTransition.m_endPosition;
        m_lastExtraWidthPoint = m_startPosition - Vector3Int.up * m_minHeight;
        m_chunkHeight = m_minHeight;
        m_lastSectionPoint = m_startPosition.x;
        m_cameraBoundsStart = m_lowestPoint = m_startPosition.y - m_chunkHeight;

        MakePolygon(startwWidth, m_startPosition);
        //adds tiles so that the end of the chunk is not visible if the transition is ascending
        if (GetTransitionHeight() > 0)
        {
            m_polygons[0].AddTiles(m_prevTransition.m_transitionRightBoundHeight, Mathf.Min(startwWidth, m_minWidth), m_startPosition - Vector3Int.up * m_minHeight);
            m_chunkHeight += m_prevTransition.m_transitionRightBoundHeight;
            m_lastExtraWidthPoint += new Vector3Int(Mathf.Min(startwWidth, m_minWidth), -m_prevTransition.m_transitionRightBoundHeight);
            m_lowestPoint = m_startPosition.y - m_chunkHeight;
        }
    }
    /// <summary>
    /// The transition constructor
    /// Sets the bounds, but does not add a polygon
    /// It is also used to create a chunk in which you do not need to create a polygon
    /// </summary>
    /// <param name="start">start of the chunk</param>
    /// <param name="end">end of the chunk</param>
    /// <param name="transition">when creating a chunk without polygons, the previous transition is needed</param>
    public Chunk(Vector3Int start, Vector3Int end, Chunk transition = null)
    {
        m_prevTransition = transition;
        m_startPosition = start;
        m_endPosition = end;
        m_lastSectionPoint = start.x;
        m_chunkHeight = Mathf.Abs(end.y - start.y) + m_minHeight;
        m_transitionLeftBoundHeight = m_transitionRightBoundHeight = Mathf.Abs(start.y - end.y);
        m_lowestPoint = m_cameraBoundsStart = (end.y > start.y ? end.y : start.y) - m_chunkHeight;
        m_lastExtraWidthPoint = start - Vector3Int.up * m_minHeight;
    }

    public Vector3Int GetEndPosition() => m_endPosition;

    public Vector3Int GetStartPosition() => m_startPosition;
    /// <summary>
    /// If the point is lower than the lowest point, change the lowest point, the start of the camera bounds, and increase the height of the chunk
    /// </summary>
    /// <param name="point">new point</param>
    void SetLowestPoint(int point)
    {
        if (m_lowestPoint > point - m_minHeight)
        {
            m_chunkHeight += m_lowestPoint - point + m_minHeight;
            m_lowestPoint = point - m_minHeight;
        }

        if (m_cameraBoundsStart > point - m_minHeight)
        {
            m_cameraBoundsStart = point - m_minHeight;
        }
    }
    /// <summary>
    /// If the point is higher than the highest point, increase the height of the chunk
    /// </summary>
    /// <param name="point">new point</param>
    void SetHighestPoint(int point)
    {
        if (m_lowestPoint + m_chunkHeight < point)
        {
            m_chunkHeight = point - m_lowestPoint;
        }
    }
    /// <summary>
    /// Calculates the new height of the side border    
    /// </summary>
    /// <param name="end">lower point</param>
    /// <param name="pos">upper point</param>
    /// <param name="oldValue">previous value</param>
    /// <returns>new value</returns>
    int SetTransitionSideBoundHeight(Vector3Int end, Vector3Int pos, int oldValue)
    {
        //if the distance to the end point is less than the minimum and no value has been set, set the height from the point to the end
        return end.x - pos.x <= m_minWidth && oldValue == 0 ? Mathf.Abs(end.y - pos.y) :
            //if the distance is greater than the minimum and a value has been set, resets it,
            //otherwise, leaves the old value
            end.x - pos.x > m_minWidth && oldValue != 0 ? 0 : oldValue;
    }
    /// <summary>
    /// Sets a new end of the chunk
    /// </summary>
    /// <param name="end"></param>
    public void SetEndPosition(Vector3Int end)
    {
        m_transitionLeftBoundHeight = SetTransitionSideBoundHeight(end, m_startPosition, m_transitionLeftBoundHeight);

        m_endPosition = end;

        SetLowestPoint(end.y);
        SetHighestPoint(end.y);
    }
    /// <summary>
    /// Sets a new start of the chunk
    /// </summary>
    /// <param name="start"></param>
    public void SetStartPosition(Vector3Int start)
    {
        m_transitionRightBoundHeight = SetTransitionSideBoundHeight(m_endPosition, start, m_transitionRightBoundHeight);

        m_startPosition = start;

        SetLowestPoint(start.y);
        SetHighestPoint(start.y);
    }

    public Chunk GetNextTransition() => m_nextTransition;

    public Chunk GetPreviousTransition() => m_prevTransition;
    //List of coordinates of tiles that the player can walk on (ground)
    public List<Vector3Int> GetGround() => m_polygons.Count == 1 ? m_polygons[0].Ground().ToList() : m_polygons.SelectMany(p => p.Ground()).ToList();

    public int GetTransitionRightHeight() => m_transitionRightBoundHeight;

    public int GetTransitionLeftHeight() => m_transitionLeftBoundHeight;

    public int GetTransitionHeight() => m_prevTransition.m_endPosition.y - m_prevTransition.m_startPosition.y;
    //The highest point of the chunk
    public int GetChunkHighestPoint() => m_lowestPoint + m_chunkHeight;
    //Height for setting the camera bounds of the chunk
    public int GetChunkCameraHeight() => m_lowestPoint + m_chunkHeight - m_cameraBoundsStart;

    public void DontFillTiles()
    {
        m_tilesFilled = true;
    }

    public void SetCameraBounds(CameraBounds bounds)
    {
        bounds.SetHeight(new Vector3(m_startPosition.x, GetChunkHighestPoint()), GetChunkCameraHeight());
    }

    public void SetTransitionCameraBounds(CameraBounds bounds)
    {
        m_prevTransition.SetCameraBounds(bounds);
    }
    /// <summary>
    /// Destroys tiles and objects in the chunk
    /// </summary>
    public void Clear(TileEditor editor, bool async = false)
    {
        m_nextTransition?.Clear(editor, async);
        foreach (var obj in m_enviroment)
        {
            Object.Destroy(obj);
        }
        if (m_tilesFilled)
        {
            foreach (Polygon poly in m_polygons)
            {
                poly.ClearTiles(editor, async);
            }
        }
    }

    public void ClearGrid()
    {
        m_polygons.Clear();
    }
    /// <summary>
    /// Restarts all objects in the chunk
    /// </summary>
    public void Restart()
    {
        m_nextTransition?.Restart();
        WalkEnemy enemy;
        MovingPlatform platform;
        Cat cat;
        DestroyableBrick brick;
        foreach (var obj in m_enviroment)
        {
            if (obj != null)
            {
                if (obj.TryGetComponent(out enemy))
                {
                    enemy.Reset();
                }
                if (obj.TryGetComponent(out platform))
                {
                    platform.Restart();
                }
                if (obj.TryGetComponent(out cat))
                {
                    cat.Reset();
                }
                if (obj.TryGetComponent(out brick))
                {
                    brick.Restart();
                }
            }
        }
    }
    /// <summary>
    /// Creates a polygon
    /// </summary>
    /// <param name="width"></param>
    /// <param name="startPos"></param>
    /// <param name="addGround">if it's needed to add a landscape, yes by default</param>
    public void MakePolygon(int width, Vector3Int startPos, bool addGround = true)
    {
        Polygon polygon = new Polygon();
        m_lowestPoint = startPos.y - m_minHeight;
        polygon.AddTiles(m_minHeight, width, startPos);
        if (addGround)
            polygon.AddGround(width, startPos);
        m_polygons.Add(polygon);
    }

    /// <summary>
    /// Adds tiles to a polygon for lowlands or elevations 
    /// </summary>
    /// <param name="height">height of the section</param>
    /// <param name="width">width of the section</param>
    /// <param name="prevEnd">end of the previous section</param>
    public void CreateElevationOrLowland(int height, int width, Vector3Int prevEnd)
    {
        //if the elevation
        if (height >= 0)
        {
            SetHighestPoint(prevEnd.y + height);
            //adds tiles from the bottom to the minimum width
            int w = Mathf.Min(m_minWidth, width);
            m_polygons[0].AddTiles(height + m_minHeight, w, prevEnd + height * Vector3Int.up);
            //adds the remaining tiles to the height of the section
            m_polygons[0].AddTiles(m_minHeight, Mathf.Max(width - m_minWidth, 0), new Vector3Int(prevEnd.x + w, prevEnd.y + height));
            m_lastExtraWidthPoint = new Vector3Int(prevEnd.x + w, prevEnd.y - m_minHeight);
        }
        //if the lowland
        else
        {
            SetLowestPoint(prevEnd.y + height);
            //adds tiles for the new section
            m_polygons[0].AddTiles(m_minHeight, width, new Vector3Int(prevEnd.x, prevEnd.y + height));
            //adds tiles between the previous section and the new one 
            int w;
            //if additional tiles were added at the bottom of the previous section,
            //and the last position X of such a tile coincides with the start X of a new section -
            //adds tiles under the previous section
            if (prevEnd.x == m_lastExtraWidthPoint.x)
            {
                w = Mathf.Min(m_minWidth, prevEnd.x - m_lastSectionPoint);
            }
            //if no additional tiles were added at the bottom of the previous section,
            //or the last tile was before the end of the section -
            //adds tiles first between the additional tiles and the end of the previous section,
            //and then the rest 
            else
            {
                //the section between the additional tiles at the bottom and the end of the section
                w = Mathf.Min(m_minWidth, prevEnd.x - m_lastExtraWidthPoint.x);
                m_polygons[0].AddTiles(-height, w, new Vector3Int(prevEnd.x - w, prevEnd.y - m_minHeight));
                //the rest of the section
                w = Mathf.Min(m_minWidth - w, m_lastExtraWidthPoint.x - m_lastSectionPoint);
            }

            m_polygons[0].AddTiles(m_lastExtraWidthPoint.y - prevEnd.y - height + m_minHeight, w, new Vector3Int(m_lastExtraWidthPoint.x - w, m_lastExtraWidthPoint.y));
        }
        m_polygons[0].AddGround(width, prevEnd + Vector3Int.up * height);
        m_lastSectionPoint = prevEnd.x;
        m_endPosition = new Vector3Int(m_endPosition.x, m_endPosition.y + height);
    }
    /// <summary>
    /// Adds tiles
    /// </summary>
    /// <param name="height">height of the tile's section</param>
    /// <param name="width">width of the tile's section</param>
    /// <param name="startPos">start of the tile's section</param>
    /// <param name="addGround">if it's needed to add a landscape, yes by default</param>
    public void AddTiles(int height, int width, Vector3Int startPos, bool addGround = true)
    {
        m_polygons[m_polygons.Count - 1].AddTiles(height, width, startPos, addGround);
        if (addGround)
        {
            m_polygons[m_polygons.Count - 1].AddGround(width, startPos);
            SetLowestPoint(startPos.y + height);
            SetHighestPoint(startPos.y + height);
        }
    }
    /// <summary>
    /// Creates a slope
    /// </summary>
    /// <param name="height">slope height</param>
    /// <param name="straightSection">width of the straight section at the top of the slope</param>
    /// <param name="startPos">start of the slope</param>
    public void CreateSlope(int height, int straightSection, Vector3Int startPos)
    {
        //adds slope positions
        for (int j = 1; j <= height; j++)
        {
            for (int i = startPos.x + j; i <= startPos.x + height * 2 + straightSection - j + 1; i++)
            {
                m_polygons[0].AddTile(new Vector3Int(i, startPos.y + j));
            }
        }
        SetHighestPoint(startPos.y + height);
        //the starting position of the slope - is not the start yet, it is an ordinary section
        m_polygons[0].AddGround(1, startPos);
        //the staright section on the top of the slope
        m_polygons[0].AddGround(straightSection, new Vector3Int(startPos.x + height + 1, startPos.y + height));
        m_polygons[0].AddGround(m_minHeight, new Vector3Int(startPos.x + height * 2 + straightSection + 1, startPos.y));
        //the straight section after the slope
        m_polygons[0].AddTiles(m_minHeight, height * 2 + straightSection + 1 + m_minHeight, startPos);
    }
    /// <summary>
    /// Creates a ledge for the transition
    /// </summary>
    /// <param name="pos">ledge position</param>
    public void CreateLedge(Vector3Int pos)
    {
        int height = pos.y - (m_endPosition.y > m_startPosition.y ? m_startPosition.y : m_endPosition.y) + m_minHeight;
        Polygon polygon = new Polygon();
        for (int j = 0; j < height; j++)
        {
            polygon.AddTile(new Vector3Int(pos.x, pos.y - j));
        }
        //если выступ расположен близко к концу и было поставлено новое значение - ставим высоту от начала выступа до конца перехода
        if (m_endPosition.x - pos.x <= m_minWidth && m_transitionLeftBoundHeight != Mathf.Abs(m_startPosition.y - m_endPosition.y))
        {
            m_transitionRightBoundHeight = Mathf.Abs(m_endPosition.y - pos.y);
        }
        //если выступ расположен близко к началу и было поставлено новое значение - ставим высоту от начала до начала выступа
        if (pos.x - m_startPosition.x >= m_minWidth && m_transitionLeftBoundHeight != Mathf.Abs(m_startPosition.y - m_endPosition.y))
        {
            m_transitionLeftBoundHeight = Mathf.Abs(pos.y - m_startPosition.y);
        }
        //верх выступа - земля
        polygon.AddGround(1, pos);
        m_polygons.Add(polygon);
    }
    /// <summary>
    /// Creats a platform 
    /// </summary>
    /// <param name="pos">platform start</param>
    /// <param name="width">platform width</param>
    public void CreatePlatform(Vector3Int pos, int width)
    {
        Polygon polygon = new Polygon();
        polygon.AddTiles(1, width, pos);
        polygon.AddGround(width, pos);
        //определяем высоту боковых границ
        m_transitionRightBoundHeight = SetTransitionSideBoundHeight(m_endPosition, pos, m_transitionRightBoundHeight);
        m_transitionLeftBoundHeight = SetTransitionSideBoundHeight(pos, m_startPosition, m_transitionLeftBoundHeight);

        SetHighestPoint(pos.y);
        SetLowestPoint(pos.y);

        m_polygons.Add(polygon);
    }
    /// <summary>
    /// Checks whether the tile is drawn in position
    /// </summary>
    /// <param name="pos">tile position</param>
    /// <returns></returns>
    public bool PositionIsUsed(Vector3Int pos)
    {
        for (int i = 0; i < m_polygons.Count; ++i)
        {
            if (m_polygons[i].ContainsTile(pos))
                return true;
        }
        return false;
    }
    /// <summary>
    /// Draws chunk's tiles in the game
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="callback">function to execute after rendering the tiles</param>
    /// <param name="isInitial">if it's the initial chunk </param>
    public void DrawTiles(TileEditor editor, System.Action<HashSet<Vector3Int>> callback, bool isInitial = false)
    {
        if (!m_tilesFilled)
        {
            m_tilesFilled = true;
            foreach (var poly in m_polygons)
            {
                poly.DrawTiles(editor, callback, isInitial);
            }
        }
    }

    public void AddEnviromentObject(GameObject obj)
    {
        m_enviroment.Add(obj);
    }
    /// <summary>
    /// Adds a transition from this chunk to the next and adds extra tiles if the transition is descending so the end of the chunk is not visible
    /// </summary>
    /// <param name="transition">new transition</param>
    public void AddTransition(Chunk transition, bool addExtraTiles = true)
    {
        m_nextTransition = transition;

        if (!addExtraTiles || transition.m_endPosition.y >= m_endPosition.y)
            return;
        int w = Mathf.Min(m_minWidth, m_endPosition.x - m_lastSectionPoint);
        m_polygons[0].AddTiles(m_nextTransition.m_transitionLeftBoundHeight, w, new Vector3Int(m_endPosition.x - w, m_endPosition.y - m_minHeight));
    }
}