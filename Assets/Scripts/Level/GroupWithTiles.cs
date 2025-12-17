using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class GroupWithTiles : TileGroup
{
    public TileGroup[] tileGroups;

    public override List<TileBase> GetTiles()
    {
        List<TileBase> tiles = new List<TileBase>();
        foreach (var group in tileGroups)
        {
            tiles.AddRange(group.tiles);
        }
        tiles.AddRange(this.tiles);
        return tiles;
    }
    /// <summary>
    /// Проходит по всем группам и подбирает тайлы, 
    /// которые есть как в tiles, так и в группе объекта
    /// </summary>
    /// <param name="tiles">тайлы для совпадения</param>
    /// <returns>лист совпавших тайлов</returns>
    public override List<TileBase> MatchesTiles(IEnumerable<TileBase> tiles)
    {
        List<TileBase> matches = new List<TileBase>();
        foreach (TileGroup group in tileGroups)
        {
            matches.AddRange(group.MatchesTiles(tiles));
        }

        matches.AddRange(base.MatchesTiles(tiles));
        return matches;
    }
    /// <summary>
    /// Проходит по всем группам и проверяет, 
    /// есть ли в этих группах и заданной группе совпавшие тайлы
    /// </summary>
    /// <param name="tile">заданный тайл</param>
    /// <returns>есть ли в группе</returns>
    public override bool ContainsTile(TileBase tile)
    {
        foreach (TileGroup group in tileGroups)
        {
            if (group.ContainsTile(tile))
                return true;
        }
        return base.ContainsTile(tile);
    }
}
