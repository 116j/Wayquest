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
    /// Checks if there is a specified tile in the group
    /// </summary>
    /// <param name="tile">tile for the search</param>
    /// <returns>if contains the tile</returns>
    public virtual bool ContainsTile(TileBase tile)
    {
        return tiles.Contains(tile);
    }
    /// <summary>
    /// Passes through a group of specified tiles and searches for matches with the tiles
    /// </summary>
    /// <param name="tiles">list of tiles to search for</param>
    /// <returns>list of matched tiles</returns>
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
