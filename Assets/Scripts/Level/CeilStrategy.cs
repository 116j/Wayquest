using System.Collections.Generic;
using UnityEngine;

public class CeilStrategy : FillStrategy
{
    protected new int m_maxChunkWidth = 40;
    protected new int m_minChunkWidth = 15;

    protected new int m_minElevationHeight = 2;
    protected new int m_maxElevationHeight = 20;

    public CeilStrategy(LevelTheme levelTheme) : base(levelTheme)
    {
    }

    /// <summary>
    /// Создает возвышенности и низменности для чанка, добавляет потолок и размещает на нем ловушки, затем добавляет ландшавт и отрисовывает тайлы
    /// </summary>
    /// <param name="prevChunk">предыдущий чанк</param>
    /// <param name="transitionStrategy">стратегия построения перехода на следующий чанк</param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //рисует тайлы перехода от предыдущего чанка к этому
        prevChunk.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevChunk.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevChunk.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minChunkWidth, m_maxChunkWidth), start.y);
        //ширина начального прямого участка
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        Chunk chunk = new Chunk(end, startWidth, prevChunk.GetNextTransition());
        //без холмлмов
        m_slopeChance = 1f;
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        CreateElevationsAndLowlands(chunk, start + startWidth * Vector3Int.right, startWidth, height, false);
        chunk.AddTransition(transitionStrategy.FillTransition(chunk));
        MakeCeil(chunk);
        return chunk;
    }

    /// <summary>
    /// Создает потолок
    /// </summary>
    /// <param name="chunk"></param>
    void MakeCeil(Chunk chunk)
    {
        List<(GameObject, Vector3)> coins = new List<(GameObject, Vector3)>();
        Trap trap = m_levelTheme.m_ceilTraps[Random.Range(0, m_levelTheme.m_ceilTraps.Length)];
        m_container.Inject(trap);
        trap.SetTrapNum();
        //отступ потолка над полом
        int offset = Mathf.CeilToInt(trap.GetHeight());

        var ground = chunk.GetGround();
        //ширина сегмента потолка
        int width = 0;
        // ширина сегмента для расположения ловушек
        int groundWidth = 0;
        // начало сегмента потолка
        Vector3Int start = chunk.GetStartPosition() + Vector3Int.right;
        // начало сегмента для расположения ловушек
        int groundStart = start.x;
        //высота потолка (количество тайлов)
        int height = chunk.GetChunkHighestPoint() - start.y + m_minStraightSection;
        //если новый чанк ниже предыдущего - ставим высоту потолка выше, чтобы игрок не смог запрыгнуть
        if (chunk.GetTransitionHeight() < 0)
        {
            height = Mathf.Max(chunk.GetNextTransition().GetTransitionRightHeight() + m_minStraightSection, height);
        }
        // полигон потолка
        chunk.MakePolygon(0, start, false);
        for (int i = 1; i < ground.Count - 1; i++)
        {
            //идет по земле, пока не закончится ровный участок
            if (ground[i].y == start.y && ground[i].x == start.x + width)
            {
                width++;
                groundWidth++;
            }
            //когда заканчивается ровный участок 
            else
            {
                if (start.y > ground[i].y)
                {
                    width += offset;
                }
                else
                {
                    groundWidth -= offset;
                    width -= offset;
                    //если возвышенность выше прыжка игрока - добавляет место для платформы
                    if (ground[i].y - start.y > m_playerJumpHeight)
                    {
                        groundWidth--;
                        width--;
                    }
                }

                if (width > 1)
                {
                    chunk.AddTiles(height, Mathf.Min(width, chunk.GetEndPosition().x - start.x), start + Vector3Int.up * (height + offset), false);
                    AddCeilTraps(trap, groundWidth, new Vector3Int(groundStart, start.y + offset), chunk);
                    width = 1;
                    height += start.y - ground[i].y;
                    //если низменность - отступает несколько клеток
                    if (start.y > ground[i].y)
                    {
                        i += offset;
                        start = ground[i];
                    }
                    //если возвышенность - добавляет дополнительную ширину слева
                    else
                    {
                        width += offset + 1;
                        start = ground[i] - Vector3Int.right * (offset + 1);
                    }
                }
                else
                {
                    width = ground[i].x - start.x;
                    height += start.y - ground[i].y;
                    start.y = ground[i].y;
                }
                groundWidth = 1;

                if (i >= ground.Count - 1)
                    break;
                groundStart = ground[i].x;
                //дообавляет ловушки на потолок
                trap = m_levelTheme.m_ceilTraps[Random.Range(0, m_levelTheme.m_ceilTraps.Length)];
                m_container.Inject(trap);
                trap.SetTrapNum();
                // добавляет монетку в конце ровного сегмента
                Coin coin = m_container.InstantiatePrefabForComponent<Coin>(m_levelTheme.m_coin, ground[i], Quaternion.identity, null);
                coin.SetCost(Random.Range(10, 100), false);
                chunk.AddEnviromentObject(coin.gameObject);
                coins.Add((coin.gameObject, new Vector3(ground[i].x + m_levelTheme.m_coin.GetOffset().x, ground[i].y + m_levelTheme.m_coin.GetOffset().y)));
            }
        }
        //последний ровный сегмент
        chunk.AddTiles(height, Mathf.Min(width, chunk.GetEndPosition().x - start.x), start + Vector3Int.up * (height + offset), false);
        AddCeilTraps(trap, groundWidth, new Vector3Int(groundStart, start.y + offset), chunk);
        //отрисовываем тайлы
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) =>
        {
            AddLandscape(chunk, groundTiles, offset, true);
            foreach (var (obj, pos) in coins)
            {
                obj.transform.position = pos;
            }
        });
    }
    /// <summary>
    /// Добавляет серию ловушек на потолок
    /// </summary>
    /// <param name="trap">тип ловушки</param>
    /// <param name="width">ширина потолка</param>
    /// <param name="start">начало потолка</param>
    /// <returns></returns>
    void AddCeilTraps(Trap trap, int width, Vector3Int start, Chunk chunk)
    {
        float offset = Random.Range(1.5f * m_playerWidth, 2 * m_playerWidth);
        float leftBorder = start.x - trap.GetLeftBorder() + offset;
        float rightBorder = start.x + width - trap.GetRightBorder();

        for (float x = leftBorder; x <= rightBorder;)
        {
            Trap newTrap = m_container.InstantiatePrefabForComponent<Trap>(trap, new Vector3(x, start.y), Quaternion.identity, null);
            newTrap.SetTrap(trap.GetTrapNum());
            chunk.AddEnviromentObject(newTrap.gameObject);
            x += trap.GetRightBorder() + offset;
        }
    }
}
