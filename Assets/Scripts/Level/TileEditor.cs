using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileEditor : MonoBehaviour
{
    [SerializeField]
    //Чейнджеры тайлов, которые содержат информацию о окружении конкретных тайлов
    TileChanger[] m_tileChangers;
    [SerializeField]
    //Аналоги тайлов, которые подбирают аналоги тайлов в зависимости от их окружения и темы уровня
    List<TilePlaceAnalog> m_tileAnalogs;

    Tilemap m_ground;
    Tilemap m_slope;
    Tilemap m_walls;
    Tilemap m_notCollidable;
    //Словарь, где каждому тайлу соответствует его чейнджер
    Dictionary<TileBase, TileChanger> m_tileToChanger = new Dictionary<TileBase, TileChanger>();
    //Сколько тайлов создается за один фрейм
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
    /// Проверяет, есть ли над тайлом тайл травы
    /// </summary>
    /// <param name="tilePos"></param>
    /// <returns>есть ли над тайлом тайл травы</returns>
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
    /// Удаляет тайлы
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
    /// Удаляет тайлы постепенно
    /// Каждый фрейм удаляет TILES_PER_FRAME тайлов
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
    /// Создает тайлы
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="callback">функция, которую нужно выполнить после создания тайлов</param>
    /// <param name="isInitial">начальный чанк</param>
    public void SetTiles(HashSet<Vector3Int> tilePositions, System.Action callback, bool isInitial)
    {
        //если начальный чанк - создает тайлы сразу
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
    /// Постепенно создает тайлы
    /// Каждый фрейм создает TILES_PER_FRAME тайлов
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="callback">функция, которую нужно выполнить после создания тайлов</param>
    /// <returns></returns>
    public IEnumerator InstantiateTilesAsync(HashSet<Vector3Int> tilePositions, System.Action callback)
    {
        Dictionary<Vector3Int, bool> tilePositionsUsage = new Dictionary<Vector3Int, bool>(tilePositions.Count);
        foreach (var pos in tilePositions)
            tilePositionsUsage[pos] = false;

        Dictionary<Vector3Int, TileBase> tilemap = new Dictionary<Vector3Int, TileBase>();
        int i = 0;
        //подбирает тайлы под конуретную позицию
        foreach (var pos in tilePositions)
        {
            InstantiateTile(pos, tilePositionsUsage, tilemap);
            i++;
            if (i % TILES_PER_FRAME == 0)
                yield return null;
        }
        i = 0;
        //рисует подобранные тайлы в игре
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
    /// Сразу создает все тайлы за 1 фрейм
    /// </summary>
    /// <param name="tilePositions"></param>
    /// <param name="callback">функция, которую нужно выполнить после создания тайлов</param>
    public void InstantiateTiles(HashSet<Vector3Int> tilePositions, System.Action callback)
    {
        Dictionary<Vector3Int, bool> tilePositionsUsage = new Dictionary<Vector3Int, bool>(tilePositions.Count);
        foreach (var pos in tilePositions)
            tilePositionsUsage[pos] = false;

        Dictionary<Vector3Int, TileBase> tilemap = new Dictionary<Vector3Int, TileBase>();
        //подбирает тайлы под конуретную позицию
        foreach (var pos in tilePositions)
        {
            InstantiateTile(pos, tilePositionsUsage, tilemap);
        }
        //рисует подобранные тайлы в игре
        foreach (var (pos, tile) in tilemap)
        {
            SetTile(m_tileToChanger[tile], tile, pos);
        }

        callback?.Invoke();
    }
    /// <summary>
    /// Подбирает подходящий тайл для позиции в зависимости от его окружения
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilePositionsUsage">словарь с позициями и индикаторами их заполнения (выбран ли на позицию тайл)</param>
    /// <param name="tilemap">словарь с позициями и подобранными к ним тайлами</param>
    void InstantiateTile(Vector3Int position, Dictionary<Vector3Int, bool> tilePositionsUsage, Dictionary<Vector3Int, TileBase> tilemap)
    {
        //находит аналог тайла в зависимости от его окружения
        TilePlaceAnalog analog = GetTileAnalog(position, tilePositionsUsage);
        if (!tilePositionsUsage[position] && analog != null)
        {
            TileBase tile = null;
            Vector3Int surPosition;
            //подбирает все возможные тайла для аналога
            List<TileBase> tiles = new List<TileBase>(GetTilesAnalog(analog));

            try
            {
                //выбирает тайлы из аналоговых, которые подходят окружению
                for (int i = 0; i < analog.surroundings.Length; i++)
                {
                    if (analog.surroundings[i])
                    {
                        surPosition = new Vector3Int(position.x + (int)Mathf.Pow(-1, i) * (3 - i) / 2, position.y + (int)Mathf.Pow(-1, i) * i / 2);
                        //если соседний тайл уже подобран - берет группу тайлов, которая подходит для данного тайла с учетом соседнего тайла
                        if (tilePositionsUsage.ContainsKey(surPosition) && tilePositionsUsage[surPosition])
                        {
                            //если есть ограничение для данного тайла из соседнего тайла - отбирает из аналогов тайлы, которые подходят для соседнего тайла
                            if (m_tileToChanger[tilemap[surPosition]].changeTiles[i + (int)Mathf.Pow(-1, i % 2)] != null)
                                tiles = m_tileToChanger[tilemap[surPosition]].changeTiles[i + (int)Mathf.Pow(-1, i % 2)].MatchesTiles(tiles);
                        }
                    }
                }
                while (tiles.Count > 0)
                {
                    //выбирает рандомный тайл
                    tile = tiles[Random.Range(0, tiles.Count)];
                    //если вдруг тайла нет - удаляет тайл с тайлмепа и выдает ошибку
                    if (tile == null)
                    {
                        m_ground.SetTile(position, null);
                        m_walls.SetTile(position, null);
                        m_notCollidable.SetTile(position, null);
                        throw new System.ArgumentNullException();
                    }
                    //чейнджер тайла
                    TileChanger newChanger = m_tileToChanger[tile];
                    //проходит по соседним тайлам
                    for (int i = 0; i < newChanger.analogTiles.surroundings.Length; i++)
                    {
                        surPosition = new Vector3Int(position.x + (int)Mathf.Pow(-1, i) * (3 - i) / 2, position.y + (int)Mathf.Pow(-1, i) * i / 2);
                        //соседний тайл
                        tilemap.TryGetValue(surPosition, out TileBase surTile);

                        if (newChanger.analogTiles.surroundings[i])
                        {
                            //если соседний тайл не подходит данному тайлу и наоборот - удаляет тайл из возможных тайлов и начинает заново с другим тайлом
                            if (tilePositionsUsage.ContainsKey(surPosition) && CheckSurrounding(tile, surPosition, i, tilePositionsUsage, tilemap))
                            {
                                tiles.Remove(tile);
                                break;
                            }
                        }
                        //если соседний тайл поставлен, но нулевой, удаляет каждый тайл
                        else if (tilePositionsUsage.ContainsKey(surPosition) && surTile == null)
                        {
                            tiles.Remove(tile);
                            break;
                        }
                        //если была трава, а теперь не нужна - удалить ее
                        else if (i == 2 && tilemap.ContainsKey(surPosition) && surTile != null && !newChanger.addGrass)
                        {
                            tilemap[surPosition] = null;
                        }
                        //если нужно добавить траву
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
    /// Проверяет, подходит ли данный тайл для соседнего тайла и наоборот
    /// </summary>
    /// <param name="tile">тайл для проверки</param>
    /// <param name="surPos">позиция соседнего тайла</param>
    /// <param name="surIndex">номер группы окружени соседнего тайла в чейнджере текущего тайла</param>
    /// <param name="tilePositionsUsage">подобранные тайлы</param>
    /// <param name="tilemap">словарь с позициями и подобранными к ним тайлами</param>
    /// <returns>тайл не подходит</returns>
    bool CheckSurrounding(TileBase tile, Vector3Int surPos, int surIndex, Dictionary<Vector3Int, bool> tilePositionsUsage, Dictionary<Vector3Int, TileBase> tilemap)
    {
        TilePlaceAnalog surAnalog = GetTileAnalog(surPos, tilePositionsUsage);

        tilemap.TryGetValue(surPos, out TileBase surTile);
        //если для данного тайла соседний тайл должен ограничиваться группой
        if (m_tileToChanger[tile].changeTiles[surIndex] != null)
        {
            //если соседний тайл уже подобран - проверить, есть ли он в группе окружения чейнджера
            if (tilePositionsUsage[surPos])
                //окружение чейнджера текущего тайла не содержит соседний тайл
                return !m_tileToChanger[tile].changeTiles[surIndex].ContainsTile(surTile) ||
                //окружение чейнджера соседнего тайла не содержит текущий тайл
                (m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile));
            else
                //если соседний тайл еще не подобран - проверить, есть ли варианты, подходящие для тайла
                //аналогов соседнего тайла нет в группе окружения чейнджера
                return m_tileToChanger[tile].changeTiles[surIndex].MatchesTiles(GetTilesAnalog(surAnalog)).Count == 0 ||
                //окружение чейнджеров всех аналогов соседнего тайла не содержит текущий тайл
                GetTilesAnalog(surAnalog).All(a => a != null && m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile));
        }
        else
        {
            //если соседний тайл подобран
            if (tilePositionsUsage[surPos])
                //окружение чейнджера соседнего тайла не содержит текущий тайл
                return m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[surTile].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile);
            else
                //окружение чейнджеров всех аналогов соседнего тайла не содержит текущий тайл
                return GetTilesAnalog(surAnalog).All(a => a != null && m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)] != null && !m_tileToChanger[a].changeTiles[surIndex + (int)Mathf.Pow(-1, surIndex % 2)].ContainsTile(tile));
        }
    }
    /// <summary>
    /// Тайлы аналога тайла в зависимости от темы уровня
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
    /// Берет название тайлмепа из чейнджера тайла и ставит тайл на тайлмепе
    /// </summary>
    /// <param name="changer">ченджера</param>
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
    /// Пытается достать тайл из каждой тайлмеп
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
    /// Находит аналолг тайла в зависимости от его окружения
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tilePositionsUsage">подобранные ранее тайлы</param>
    /// <returns></returns>
    public TilePlaceAnalog GetTileAnalog(Vector3Int position, Dictionary<Vector3Int, bool> tilePositionsUsage)
    {
        //находит аналоги, которые соответствуют окружающим подобранным тайлам
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
        //если у аналога есть дубль с таким же окружением - уточняет окружение
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