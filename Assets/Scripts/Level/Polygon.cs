using System.Collections.Generic;
using UnityEngine;

public class Polygon
{
    HashSet<Vector3Int> m_tilePositions = new HashSet<Vector3Int>();
    HashSet<Vector3Int> m_ground = new HashSet<Vector3Int>();
    /// <summary>
    /// Отрисовывает тайлы полигона
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="callback">функция, которую нужно выполнить после отрисовки тайлов</param>
    /// <param name="isInitial">начальный чанк</param>
    public void DrawTiles(TileEditor editor, System.Action<HashSet<Vector3Int>> callback, bool isInitial)
    {
        editor.SetTiles(m_tilePositions, () => callback?.Invoke(m_ground), isInitial);
    }
    /// <summary>
    /// Удаляет тайлы полигона из игры
    /// </summary>
    /// <param name="editor"></param>
    /// <param name="async">удалять асинхронно</param>
    public void ClearTiles(TileEditor editor, bool async)
    {
        editor.ClearTiles(m_tilePositions, async);
    }
    /// <summary>
    /// Добавляет тайлы от startPosition до (startPosition.x + width,startPosition.y - height)
    /// </summary>
    /// <param name="height">высота участка тайлов вниз</param>
    /// <param name="width">ширина участка тайлов</param>
    /// <param name="startPosition">начало участка тайлов</param>
    public void AddTiles(int height, int width, Vector3Int startPosition, bool ground = true)
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                Vector3Int pos = new Vector3Int(startPosition.x + j, startPosition.y - i);
                if (!m_tilePositions.Contains(pos))
                {
                    m_tilePositions.Add(pos);
                }
            }
        }
    }

    public void AddTile(Vector3Int tilePos)
    {
        if (!m_tilePositions.Contains(tilePos))
        {
            m_tilePositions.Add(tilePos);
        }
    }

    public void AddGround(int width, Vector3Int startPosition)
    {
        for (int i = 0; i < width; i++)
        {
            Vector3Int pos = new Vector3Int(startPosition.x + i, startPosition.y);
            if (!m_ground.Contains(pos))
            {
                m_ground.Add(pos);
            }
        }
    }

    public HashSet<Vector3Int> Ground() => m_ground;

    public bool ContainsTile(Vector3Int tilePos)
    {
        return m_tilePositions.Contains(tilePos);
    }
}
