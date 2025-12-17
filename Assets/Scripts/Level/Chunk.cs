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

    //Последняя позиция дополнительно добавленной ширины участка
    Vector3Int m_lastExtraWidthPoint;
    //X координата начала крайнего участка
    int m_lastSectionPoint;
    //Самая низкая Y координата участка
    int m_lowestPoint;
    //Y координата начала границ камеры
    int m_cameraBoundsStart;
    int m_chunkHeight = 0;

    //Высота левой границы перехода
    int m_transitionLeftBoundHeight;
    //Высота правой границы перехода
    int m_transitionRightBoundHeight;
    readonly int m_minHeight = 6;
    readonly int m_minWidth = 12;

    bool m_tilesFilled = false;
    /// <summary>
    /// Добавляет начальный полигон, добавляет тайлы, если нужно, чтобы не было видно конца чанка
    /// </summary>
    /// <param name="end">конец чанка</param>
    /// <param name="startwWidth">ширина начального прямого участка</param>
    /// <param name="transition">переход между этим и предыдущим чанком</param>
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
        //добавляет тайлы, чтобы конец чанка не был виден, если переход восходящий
        if (GetTransitionHeight() > 0)
        {
            m_polygons[0].AddTiles(m_prevTransition.m_transitionRightBoundHeight, Mathf.Min(startwWidth, m_minWidth), m_startPosition - Vector3Int.up * m_minHeight);
            m_chunkHeight += m_prevTransition.m_transitionRightBoundHeight;
            m_lastExtraWidthPoint += new Vector3Int(Mathf.Min(startwWidth, m_minWidth), -m_prevTransition.m_transitionRightBoundHeight);
            m_lowestPoint = m_startPosition.y - m_chunkHeight;
        }
    }
    /// <summary>
    /// Конструктор для перехода
    /// Устанавливает границы, но не добавляет полигона
    /// Также служит для создания чанка, в котором не нужно создавать полигон
    /// </summary>
    /// <param name="start">начало перехода</param>
    /// <param name="end">конец перехода</param>
    /// <param name="transition">при создании чанка без полигонов нужен предыдущий переход</param>
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
    /// Если точка ниже, чем самая нижняя точка - изменить самую нижнюю точку, начало границ камеры и увеличить высоту чанка
    /// </summary>
    /// <param name="point">точка для сравнения</param>
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
    /// Если точка выше, чем самая высокая точка - увеличить высоту чанка
    /// </summary>
    /// <param name="point">точка для сравнения</param>
    void SetHighestPoint(int point)
    {
        if (m_lowestPoint + m_chunkHeight < point)
        {
            m_chunkHeight = point - m_lowestPoint;
        }
    }
    /// <summary>
    /// Высчитывает новую высоту боковой границы 
    /// </summary>
    /// <param name="end">нижняя точка</param>
    /// <param name="pos">верхняя точка</param>
    /// <param name="oldValue">предыдущее значенте</param>
    /// <returns>новое значение</returns>
    int SetTransitionSideBoundHeight(Vector3Int end, Vector3Int pos, int oldValue)
    {
        //если расстояние до конечной точки меньше минимального и не было поставлено значение - ставим высоту от точки до конца
        return end.x - pos.x <= m_minWidth && oldValue == 0 ? Mathf.Abs(end.y - pos.y) :
            //если расстояние больше минимального и было поставлено значение - сбрасываем,
            //иначе оставляем старое значение
            end.x - pos.x > m_minWidth && oldValue != 0 ? 0 : oldValue;
    }
    /// <summary>
    /// Устанавливает новый конец чанка
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
    /// Устанавливает новое начало чанка
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
    //Лист координат тайлов, по которым может ходить игрок (земля)
    public List<Vector3Int> GetGround() => m_polygons.Count == 1 ? m_polygons[0].Ground().ToList() : m_polygons.SelectMany(p => p.Ground()).ToList();

    public int GetTransitionRightHeight() => m_transitionRightBoundHeight;

    public int GetTransitionLeftHeight() => m_transitionLeftBoundHeight;

    public int GetTransitionHeight() => m_prevTransition.m_endPosition.y - m_prevTransition.m_startPosition.y;
    //Самая высокая точка чанка
    public int GetChunkHighestPoint() => m_lowestPoint + m_chunkHeight;
    //Высота для установки границ камеры этого чанка
    public int GetChunkCameraHeight() => m_chunkHeight - m_cameraBoundsStart + m_lowestPoint;

    public void DontFillTiles()
    {
        m_tilesFilled = true;
    }

    public void SetCameraBounds()
    {
        CameraBounds.Instance.SetHeight(new Vector3(m_startPosition.x, GetChunkHighestPoint()), GetChunkCameraHeight());
    }

    public void SetTransitionCameraBounds()
    {
        m_prevTransition.SetCameraBounds();
    }
    /// <summary>
    /// Удаляет тайлы и объекты в чанке
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
    /// Перезвпускает все объекты чанка
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
    /// Создает полигон
    /// </summary>
    /// <param name="width"></param>
    /// <param name="startPos"></param>
    /// <param name="addGround">если нужно добавить ладшафт, по умолчанию - да</param>
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
    /// Добавляет тайлы в полигон для низменности или возвышенности 
    /// </summary>
    /// <param name="height">высота участка</param>
    /// <param name="width">ширина участка</param>
    /// <param name="startPos">конец предыдущего участка</param>
    public void CreateElevationOrLowland(int height, int width, Vector3Int startPos)
    {
        //если возвышенность
        if (height >= 0)
        {
            SetHighestPoint(startPos.y + height);
            // добавляет тайлы снизу на минимальную ширину
            int w = Mathf.Min(m_minWidth, width);
            m_polygons[0].AddTiles(height + m_minHeight, w, startPos + height * Vector3Int.up);
            // добавляет оставшиеся тайлы на высоту участка
            m_polygons[0].AddTiles(m_minHeight, Mathf.Max(width - m_minWidth, 0), new Vector3Int(startPos.x + w, startPos.y + height));
            m_lastExtraWidthPoint = new Vector3Int(startPos.x + w, startPos.y - m_minHeight);
        }
        //если низменность
        else
        {
            SetLowestPoint(startPos.y + height);
            // добавлет тайлы нового участка
            m_polygons[0].AddTiles(m_minHeight, width, new Vector3Int(startPos.x, startPos.y + height));
            //добавляет тайлы между предыдущим участком и новым 
            int w;
            //если на предыдущем участке были добавлены внизу доп. тайлы,
            //и последняя позиция Х такого тайла совпадает с началом X нового участка -
            //добавить тайлы под предыдущим участком
            //if previous lowland or elevation width's last point is the same as start, add tiles below previous lowland or elevation
            if (startPos.x == m_lastExtraWidthPoint.x)
            {
                w = Mathf.Min(m_minWidth, startPos.x - m_lastSectionPoint);
            }
            //если на предыдущем участке не было добавлено доп. тайлов внизу, 
            //или последний тайл был до конца участка -
            //добавить тайлы сначала между доп. тайлами и концом предыдущего участка,
            //а затем оставшуюся часть 
            // if previous lowland or elevation width's last point is farther then start, add tiles below previous lowland or elevation and bettween start and lowland or elevation width's last point
            else
            {
                //секция между доп. тайлами снизу и концом участка
                w = Mathf.Min(m_minWidth, startPos.x - m_lastExtraWidthPoint.x);
                m_polygons[0].AddTiles(-height, w, new Vector3Int(startPos.x - w, startPos.y - m_minHeight));
                //оставшаяся секция
                w = Mathf.Min(m_minWidth - w, m_lastExtraWidthPoint.x - m_lastSectionPoint);
            }

            m_polygons[0].AddTiles(m_lastExtraWidthPoint.y - startPos.y - height + m_minHeight, w, new Vector3Int(m_lastExtraWidthPoint.x - w, m_lastExtraWidthPoint.y));
        }
        m_polygons[0].AddGround(width, startPos + Vector3Int.up * height);
        m_lastSectionPoint = startPos.x;
        m_endPosition = new Vector3Int(m_endPosition.x, m_endPosition.y + height);
    }
    /// <summary>
    /// Добавляет тайлы
    /// </summary>
    /// <param name="height">высота участка</param>
    /// <param name="width">ширина участка</param>
    /// <param name="startPos"></param>
    /// <param name="addGround">если нужно добавить ладшафт, по умолчанию - да</param>
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
    /// Создает холм
    /// </summary>
    /// <param name="height">высота холма</param>
    /// <param name="straightSection">ширина прямого участка на вершине холма</param>
    /// <param name="startPos">начало холма</param>
    public void CreateSlope(int height, int straightSection, Vector3Int startPos)
    {
        //добавляет позиции холма
        for (int j = 1; j <= height; j++)
        {
            for (int i = startPos.x + j; i <= startPos.x + height * 2 + straightSection - j + 1; i++)
            {
                m_polygons[0].AddTile(new Vector3Int(i, startPos.y + j));
            }
        }
        SetHighestPoint(startPos.y + height);
        //стартовая позиция холма - еще не начало, обычный участок
        m_polygons[0].AddGround(1, startPos);
        //прямой участок на вершине хома
        m_polygons[0].AddGround(straightSection, new Vector3Int(startPos.x + height + 1, startPos.y + height));
        m_polygons[0].AddGround(m_minHeight, new Vector3Int(startPos.x + height * 2 + straightSection + 1, startPos.y));
        //прямой участок после холма
        m_polygons[0].AddTiles(m_minHeight, height * 2 + straightSection + 1 + m_minHeight, startPos);
    }
    /// <summary>
    /// Создает выступ для перехода
    /// </summary>
    /// <param name="pos">позиция выступа</param>
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
    /// Создает платформу для перехода
    /// </summary>
    /// <param name="pos">начало платформы</param>
    /// <param name="width">ширина платформы</param>
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
    /// Проверяет, нарисован ли тайл на позиции
    /// </summary>
    /// <param name="pos">позиция тайла</param>
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
    /// Рисует тайлы чанка в игре
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="callback">функция, которую нужно выполнить после отрисовки тайлов</param>
    /// <param name="isInitial">начальный чанк</param>
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
    /// Добавляет переход от этого чанка к следующему и добавляет тайлы, если переход низходящий, чтобы не было видно каонца чанка
    /// </summary>
    /// <param name="transition">новый переход</param>
    public void AddTransition(Chunk transition, bool addExtraTiles = true)
    {
        m_nextTransition = transition;

        if (!addExtraTiles || transition.m_endPosition.y >= m_endPosition.y)
            return;
        int w = Mathf.Min(m_minWidth, m_endPosition.x - m_lastSectionPoint);
        m_polygons[0].AddTiles(m_nextTransition.m_transitionLeftBoundHeight, w, new Vector3Int(m_endPosition.x - w, m_endPosition.y - m_minHeight));
    }
}