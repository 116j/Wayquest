using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
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
    /// It goes through all the groups and selects tiles,
    /// which are in both the tiles and the object group
    /// </summary>
    /// <param name="tiles">tiles for match</param>
    /// <returns>matched tiles list</returns>
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
    /// It goes through all the groups and checks
    /// if there are matching tiles in these groups and the specified group
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
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
