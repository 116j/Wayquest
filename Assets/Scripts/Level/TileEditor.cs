using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileEditor : MonoBehaviour
{
    [SerializeField]
    //Tile changers that contain information about the environment of specific tiles
    TileChanger[] m_tileChangers;
    [SerializeField]
    //Tile analogs, which select tile analogs depending on their environment and level theme
    List<TilePlaceAnalog> m_tileAnalogs;

    Tilemap m_ground;
    Tilemap m_slope;
    Tilemap m_walls;
    Tilemap m_notCollidable;
    //Dictionary where each tile corresponds to its changer
    Dictionary<TileBase, TileChanger> m_tileToChanger = new Dictionary<TileBase, TileChanger>();
    //How many tiles are created in one frame
    const int TILES_PER_FRAME = 20;

    int m_tilePaletteIndex;

    private void Awake()
    {
        m_ground = transform.GetChild(0).GetComponent<Tilemap>();
        m_slope = transform.GetChild(1).GetComponent<Tilemap>();
        m_walls = transform.GetChild(2).GetComponent<Tilemap>();
        m_notCollidable = transform.GetChild(3).GetComponent<Tilemap>();

        foreach (var changer in m_tileChangers)
        {
            foreach (var tile in changer.tiles)
            {
                m_tileToChanger.Add(tile, changer);
            }
        }
    }

    public void SetTheme(int num)
    {
        m_tilePaletteIndex = num;
    }
    /// <summary>
    /// Checks if there is a grass tile above the tile
    /// </summary>
    /// <param name="tilePos"></param>
    /// <returns></returns>
    public bool AddGrass(Vector3Int tilePos)
    {
        TileBase tile = m_ground.GetTile(tilePos);
        if (tile != null && m_tileToChanger.ContainsKey(tile))
        {
            return m_tileToChanger[tile].addGrass;
        }

        Debug.Log(tilePos);
        return false;

    }
    /// <summary>
    /// Destroys tiles at once
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="async"></param>
    public void ClearTiles(HashSet<Vector3Int> tilePositions, bool async)
    {
        if (async)
        {
            StartCoroutine(ClearTilesAsync(tilePositions));
        }
        else
        {
            //удаляет всс тайлы сразу за 1 фрейм
            TileBase tile = null;
            foreach (var pos in tilePositions)
            {
                if (tile = GetTileFromTilemap(pos))
                {
                    SetTile(m_tileToChanger[tile], null, pos);
                    if (m_tileToChanger[tile].addGrass)
                    {
                        m_notCollidable.SetTile(new Vector3Int(pos.x, pos.y + 1), null);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Deletes tiles gradually
    /// Each frame deletes TILES_PER_FRAME tiles
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <returns></returns>
    public IEnumerator ClearTilesAsync(HashSet<Vector3Int> tilePositions)
    {
        TileBase tile;
        int i = 0;
        foreach (var pos in tilePositions)
        {
            if (tile = GetTileFromTilemap(pos))
            {
                SetTile(m_tileToChanger[tile], null, pos);
                if (m_tileToChanger[tile].addGrass)
                {
                    m_notCollidable.SetTile(new Vector3Int(pos.x, pos.y + 1), null);
                }
                i++;
                if (i % TILES_PER_FRAME == 0)
                    yield return null;
            }
        }
    }
    /// <summary>
    /// Sets tiles
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="callback">function to execute after creating the tiles</param>
    /// <param name="isInitial">is the initial chunk</param>
    public void SetTiles(HashSet<Vector3Int> tilePositions, System.Action callback, bool isInitial)
    {
        //if the initial chunk is created - creates tiles immediately
        if (isInitial)
        {
            InstantiateTiles(tilePositions, callback);
        }
        else
        {
            StartCoroutine(InstantiateTilesAsync(tilePositions, callback));
        }
    }
    /// <summary>
    /// Creates tiles gradually
    /// Each frame creates TILES_PER_FRAME tiles
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="callback">function to execute after creating the tiles</param>
    /// <returns></returns>
    public IEnumerator InstantiateTilesAsync(HashSet<Vector3Int> tilePositions, System.Action callback)
    {
        Dictionary<Vector3Int, bool> tilePositionsUsage = new Dictionary<Vector3Int, bool>(tilePositions.Count);
        foreach (var pos in tilePositions)
            tilePositionsUsage[pos] = false;

        Dictionary<Vector3Int, TileBase> tilemap = new Dictionary<Vector3Int, TileBase>();
        int i = 0;
        //selects tiles for a specific position
        foreach (var pos in tilePositions)
        {
            InstantiateTile(pos, tilePositionsUsage, tilemap);
            i++;
            if (i % TILES_PER_FRAME == 0)
                yield return null;
        }
        i = 0;
        //draws selected tiles in the game
        foreach (var (pos, tile) in tilemap)
        {
            SetTile(m_tileToChanger[tile], tile, pos);
            i++;
            if (i % TILES_PER_FRAME == 0)
                yield return null;
        }

        callback?.Invoke();
    }
    /// <summary>
    /// Creates all tiles in 1 frame at once
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="callback">function to execute after creating the tiles</param>
    public void InstantiateTiles(HashSet<Vector3Int> tilePositions, System.Action callback)
    {
        Dictionary<Vector3Int, bool> tilePositionsUsage = new Dictionary<Vector3Int, bool>(tilePositions.Count);
        foreach (var pos in tilePositions)
            tilePositionsUsage[pos] = false;

        Dictionary<Vector3Int, TileBase> tilemap = new Dictionary<Vector3Int, TileBase>();
        //selects tiles for a specific position
        foreach (var pos in tilePositions)
        {
            InstantiateTile(pos, tilePositionsUsage, tilemap);
        }
        //draws selected tiles in the game
        foreach (var (pos, tile) in tilemap)
        {
            SetTile(m_tileToChanger[tile], tile, pos);
        }

        callback?.Invoke();
    }
    /// <summary>
    /// Selects the appropriate tile for the position depending on its environment
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilePositionsUsage">dictionary with positions and indicators of their filling (whether a tile is selected for the position)</param>
    /// <param name="tilemap">a dictionary with positions and tiles matched to them</param>
    void InstantiateTile(Vector3Int position, Dictionary<Vector3Int, bool> tilePositionsUsage, Dictionary<Vector3Int, TileBase> tilemap)
    {
        //finds an analog of a tile depending on its environment
        TilePlaceAnalog analog = GetTileAnalog(position, tilePositionsUsage);
        if (!tilePositionsUsage[position] && analog != null)
        {
            TileBase tile = null;
            Vector3Int surPosition;
            //selects all possible tiles for an analog
            List<TileBase> tiles = new List<TileBase>(GetTilesAnalog(analog));

            try
            {
                //selects tiles from analog ones that match the environment
                for (int i = 0; i < analog.surroundings.Length; i++)
                {
                    if (analog.surroundings[i])
                    {
                        surPosition = new Vector3Int(position.x + (int)Mathf.Pow(-1, i) * (3 - i) / 2, position.y + (int)Mathf.Pow(-1, i) * i / 2);
                        //if the neighbor tile has already been selected - takes a tile group that is suitable for this tile, taking into account the neighbor tile
                        if (tilePositionsUsage.ContainsKey(surPosition) && tilePositionsUsage[surPosition])
                        {
                            //if there is a restriction for a given tile from a neighbor tile - selects tiles from analogs that are suitable for the neighbor tile
                            if (m_tileToChanger[tilemap[surPosition]].changeTiles[i + (int)Mathf.Pow(-1, i % 2)] != null)
                                tiles = m_tileToChanger[tilemap[surPosition]].changeTiles[i + (int)Mathf.Pow(-1, i % 2)].MatchesTiles(tiles);
                        }
                    }
                }
                while (tiles.Count > 0)
                {
                    //chooses a random tile
                    tile = tiles[Random.Range(0, tiles.Count)];
                    //if suddenly there is no tile, it deletes the tile from the tilemap and throws an exception
                    if (tile == null)
                    {
                        m_ground.SetTile(position, null);
                        m_walls.SetTile(position, null);
                        m_notCollidable.SetTile(position, null);
                        throw new System.ArgumentNullException();
                    }
                    //tile changer
                    TileChanger newChanger = m_tileToChanger[tile];
                    //passes through neighbor tiles
                    for (int i = 0; i < newChanger.analogTiles.surroundings.Length; i++)
                    {
                        surPosition = new Vector3Int(position.x + (int)Mathf.Pow(-1, i) * (3 - i) / 2, position.y + (int)Mathf.Pow(-1, i) * i / 2);
                        //neighbor tile
                        tilemap.TryGetValue(surPosition, out TileBase surTile);

                        if (newChanger.analogTiles.surroundings[i])
                        {
                            //if the neighbor tile does not match this tile and vice versa - removes the tile from the possible tiles and starts over with another tile
                            if (tilePositionsUsage.ContainsKey(surPosition) && CheckSurrounding(tile, surPosition, i, tilePositionsUsage, tilemap))
                            {
                                tiles.Remove(tile);
                                break;
                            }
                        }
                        //if an adjacent tile is set but null -  deletes tile from the list
                        else if (tilePositionsUsage.ContainsKey(surPosition) && surTile == null)
                        {
                            tiles.Remove(tile);
                            break;
                        }
                        //If there was grass, but now it isn't needed - removes it
                        else if (i == 2 && tilemap.ContainsKey(surPosition) && surTile != null && !newChanger.addGrass)
                        {
                            tilemap[surPosition] = null;
                        }
                        //if it's needed to add grass
                        else if (i == 2 && newChanger.addGrass)
                        {
                            tilemap[surPosition] = newChanger.changeTiles[i].GetTiles()[Random.Range(0, newChanger.changeTiles[i].GetTiles().Count)];
                        }
                    }

                    if (tiles.Contains(tile))
                    {
                        break;
                    }
                }
                tilePositionsUsage[position] = true;
                tilemap[position] = tile;
            }
            catch (System.ArgumentNullException)
            {
                Debug.Log("Null " + position);
            }
        }
    }
    /// <summary>
    /// Checks whether a given tile is suitable for an neighbor tile and vice versa
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="surPos">position of the neighbor tile</param>
    /// <param name="surIndex">the number of the neighbor tile's surrounding group in the current tile's changer</param>
    /// <param name="tilePositionsUsage">chosen tiles</param>
    /// <param name="tilemap">dictionary with positions and tiles matched to them</param>
    /// <returns>tile doesn't fit</returns>
    bool CheckSurrounding(TileBase tile, Vector3Int surPos, int surIndex, Dictionary<Vector3Int, bool> tilePositionsUsage, Dictionary<Vector3Int, TileBase> tilemap)
    {
        TilePlaceAnalog surAnalog = GetTileAnalog(surPos, tilePositionsUsage);

        tilemap.TryGetValue(surPos, out TileBase surTile);
        //if for a given tile, the neighbor tile should be limited to a group
        if (m_tileToChanger[tile].changeTiles[surIndex] != null)
        {
            //if the neighbor tile has already been selected - check if it is in the surrounding group of the changer
            if (tilePositionsUsage[surPos])
                //the surroundings of the current tile's changer does not contain the neighbor tile
                return !m_tileToChanger[tile].changeTiles[surIndex].ContainsTile(surTile) ||
                //the surroundings of the neighbor tile's changer does not contain the current tile
                (m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile));
            else
                //if the neighbor tile has not been selected yet - checks if there are any suitable options for the tile
                //there are no analogs of the neighbor tile in the surrounding group of the changer
                return m_tileToChanger[tile].changeTiles[surIndex].MatchesTiles(GetTilesAnalog(surAnalog)).Count == 0 ||
                //окружение чейнджеров всех аналогов соседнего тайла не содержит текущий тайл
                GetTilesAnalog(surAnalog).All(a => a != null && m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile));
        }
        else
        {
            //if the neighbor tile is chosen
            if (tilePositionsUsage[surPos])
                //the surroundings of the neighbor tile's changer does not contain the current tile
                return m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile);
            else
                //the surroundings of the changers of all analogs of the neighbor tile does not contain the current tile
                return GetTilesAnalog(surAnalog).All(a => a != null && m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile));
        }
    }
    /// <summary>
    /// Tiles of the tile's analog, depending on the theme of the level
    /// </summary>
    /// <param name="analog"></param>
    /// <returns></returns>
    TileBase[] GetTilesAnalog(TilePlaceAnalog analog)
    {
        return m_tilePaletteIndex switch
        {
            0 => analog.dark,
            1 => analog.calm,
            _ => null,
        };
    }
    /// <summary>
    /// Takes the name of the tilemap from the tile changer and puts the tile on the tilemap
    /// </summary>
    /// <param name="changer"></param>
    /// <param name="tile"></param>
    /// <param name="position"></param>
    void SetTile(TileChanger changer, TileBase tile, Vector3Int position)
    {
        if (m_ground.CompareTag(changer.analogTiles.tilemapTag))
        {
            m_ground.SetTile(position, tile);
        }
        else if (m_walls.CompareTag(changer.analogTiles.tilemapTag))
        {
            m_walls.SetTile(position, tile);
        }
        else if (m_notCollidable.CompareTag(changer.analogTiles.tilemapTag))
        {
            m_notCollidable.SetTile(position, tile);
        }
        else if (m_slope.CompareTag(changer.analogTiles.tilemapTag))
        {
            m_slope.SetTile(position, tile);
        }
    }
    /// <summary>
    /// Trys to get a tile out of each tilemap
    /// </summary>
    /// <param name="position"></param>
    /// <returns>tile</returns>
    TileBase GetTileFromTilemap(Vector3Int position)
    {
        TileBase tile;
        if (tile = m_ground.GetTile(position))
        {
            return tile;
        }
        if (tile = m_walls.GetTile(position))
        {
            return tile;
        }
        if (tile = m_notCollidable.GetTile(position))
        {
            return tile;
        }
        if (tile = m_slope.GetTile(position))
        {
            return tile;
        }

        return null;
    }
    /// <summary>
    /// Finds an analog of a tile depending on its surroundings
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilePositionsUsage">tiles that were selected earlier</param>
    /// <returns></returns>
    public TilePlaceAnalog GetTileAnalog(Vector3Int position, Dictionary<Vector3Int, bool> tilePositionsUsage)
    {
        //finds analogs that match the surrounding selected tiles
        TilePlaceAnalog result = m_tileAnalogs.Find(a =>
        {
            for (int i = 0; i < 4; i++)
            {
                int x = (int)Mathf.Pow(-1, i) * (3 - i) / 2;
                int y = (int)Mathf.Pow(-1, i) * i / 2;
                if (tilePositionsUsage.ContainsKey(new Vector3Int(position.x + x, position.y + y)) != a.surroundings[i])
                    return false;
            }
            return true;
        });
        //if an analog has a double with the same surroundings - clarifies the surroundings
        if (result != null && result.placeAnalog != null)
        {
            bool isSlopeConditionMet = result.tilemapTag == "slope"
            && (tilePositionsUsage.ContainsKey(new Vector3Int(position.x + 1, position.y))
            && GetTileAnalog(new Vector3Int(position.x + 1, position.y), tilePositionsUsage) == GetTileAnalog(new Vector3Int(position.x, position.y - 1), tilePositionsUsage)
             || tilePositionsUsage.ContainsKey(new Vector3Int(position.x - 1, position.y))
             && GetTileAnalog(new Vector3Int(position.x - 1, position.y), tilePositionsUsage) == GetTileAnalog(new Vector3Int(position.x, position.y - 1), tilePositionsUsage)
             || tilePositionsUsage.ContainsKey(new Vector3Int(position.x - 1, position.y - 1))
             && result == GetTileAnalog(new Vector3Int(position.x - 1, position.y - 1), tilePositionsUsage)
             || tilePositionsUsage.ContainsKey(new Vector3Int(position.x + 1, position.y - 1))
             && result == GetTileAnalog(new Vector3Int(position.x + 1, position.y - 1), tilePositionsUsage))
             || result.tilemapTag == "ground";

            return isSlopeConditionMet ? result : result.placeAnalog;
        }

        return result;
    }
}