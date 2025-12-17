using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileGroup : Group
{
    public List<TileBase> tiles;

    public override List<TileBase> GetTiles()
    {
        return this.tiles;
    }
    /// <summary>
    /// Проверяет, есть ли в группе заданный тайл
    /// </summary>
    /// <param name="tile">тайл для поиска</param>
    /// <returns>есть ли тайл</returns>
    public virtual bool ContainsTile(TileBase tile)
    {
        return tiles.Contains(tile);
    }
    /// <summary>
    /// Проходит по группе заданных тайлов и смотрит, есть ли такие же в группе объекта
    /// </summary>
    /// <param name="tiles">список тайлов для поиска</param>
    /// <returns>список совпавших в обоих группах тайлов</returns>
    public virtual List<TileBase> MatchesTiles(IEnumerable<TileBase> tiles)
    {
        List<TileBase> matches = new List<TileBase>();
        foreach (TileBase tile in this.tiles)
        {
            if (tiles.Contains(tile))
            {
                matches.Add(tile);
            }
        }
        return matches;
    }

}
