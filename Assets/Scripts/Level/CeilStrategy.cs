using System.Collections.Generic;
using UnityEngine;

public class CeilStrategy : FillStrategy
{
    protected new int m_maxChunkWidth = 40;
    protected new int m_minChunkWidth = 15;

    protected new int m_minElevationHeight = 2;
    protected new int m_maxElevationHeight = 20;
    //Ceil offset above the floor
    readonly int m_ceilOffset = 3;

    public CeilStrategy(LevelTheme levelTheme) : base(levelTheme)
    {
    }

    /// <summary>
    /// Creates elevations and lowlands for the chunk, adds a ceiling and places traps on it, than adds a landscape and draws tiles
    /// </summary>
    /// <param name="prevChunk">previous chunk</param>
    /// <param name="transitionStrategy">strategy for building a transition to the next chunk</param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //draws transition tiles from the previous chunk to this one
        prevChunk.GetNextTransition().DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(prevChunk.GetNextTransition(), groundTiles, int.MaxValue, false));

        Vector3Int start = prevChunk.GetNextTransition().GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + Random.Range(m_minChunkWidth, m_maxChunkWidth), start.y);
        //width of the start section
        int startWidth = Random.Range(m_minStraightSection, end.x - start.x);
        Chunk chunk = new Chunk(end, startWidth, prevChunk.GetNextTransition());
        //without slopes
        m_slopeChance = -1f;
        int height = Random.Range(m_minElevationHeight, m_maxElevationHeight) * (Random.value > 0.5f ? -1 : 1);
        SetRightOffset(height);
        CreateElevationsAndLowlands(chunk, start + startWidth * Vector3Int.right, startWidth, height, false);
        chunk.AddTransition(transitionStrategy.FillTransition(chunk));
        MakeCeiling(chunk);
        return chunk;
    }

    /// <summary>
    /// Creates a ceiling
    /// </summary>
    /// <param name="chunk"></param>
    void MakeCeiling(Chunk chunk)
    {
        List<(GameObject, Vector3)> coins = new List<(GameObject, Vector3)>();
        Trap trap = m_levelTheme.m_ceilTraps[Random.Range(0, m_levelTheme.m_ceilTraps.Length)];
        m_container.Inject(trap);
        trap.SetTrapNum();

        var ground = chunk.GetGround();
        //ceiling section width
        int width = 0;
        //traps section width
        int groundWidth = 0;
        //ceiling section start
        Vector3Int start = chunk.GetStartPosition() + Vector3Int.right;
        //the traps section width
        int groundStart = start.x;
        //ceiling height
        int height = chunk.GetChunkHighestPoint() - start.y + m_minStraightSection;
        //if the new chunk is lower than the previous one, sets the ceiling height higher so that the player cannot jump
        if (chunk.GetTransitionHeight() < 0)
        {
            height = Mathf.Max(chunk.GetNextTransition().GetTransitionRightHeight() + m_minStraightSection, height);
        }
        //ceiling's polygon
        chunk.MakePolygon(0, start, false);
        for (int i = 1; i < ground.Count - 1; i++)
        {
            //runs along the ground until a section
            if (ground[i].y == start.y && ground[i].x == start.x + width)
            {
                width++;
                groundWidth++;
            }
            //when a section ends 
            else
            {
                if (start.y > ground[i].y)
                {
                    width += m_ceilOffset;
                }
                else
                {
                    groundWidth -= m_ceilOffset;
                    width -= m_ceilOffset;
                    //if the elevation is higher than the player's jump - adds space for the platform.
                    if (ground[i].y - start.y > m_playerJumpHeight)
                    {
                        groundWidth--;
                        width--;
                    }
                }

                if (width > 1)
                {
                    chunk.AddTiles(height, Mathf.Min(width, chunk.GetEndPosition().x - start.x), start + Vector3Int.up * (height + m_ceilOffset), false);
                    AddCeilTraps(trap, groundWidth, new Vector3Int(groundStart, start.y + m_ceilOffset), chunk);
                    width = 1;
                    height += start.y - ground[i].y;
                    //if the lowland - several tiles offset
                    if (start.y > ground[i].y)
                    {
                        i += m_ceilOffset;
                        start = ground[i];
                    }
                    //if the elevation - adds extra width on the left
                    else
                    {
                        width += m_ceilOffset + 1;
                        start = ground[i] - Vector3Int.right * (m_ceilOffset + 1);
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
                //selects traps on the ceiling for the next section
                trap = m_levelTheme.m_ceilTraps[Random.Range(0, m_levelTheme.m_ceilTraps.Length)];
                m_container.Inject(trap);
                trap.SetTrapNum();
                //adds a coin at the end of the section
                Coin coin = m_container.InstantiatePrefabForComponent<Coin>(m_levelTheme.m_coin, ground[i], Quaternion.identity, null);
                coin.SetCost(Random.Range(10, 100), false);
                chunk.AddEnviromentObject(coin.gameObject);
                coins.Add((coin.gameObject, new Vector3(ground[i].x + m_levelTheme.m_coin.GetOffset().x, ground[i].y + m_levelTheme.m_coin.GetOffset().y)));
            }
        }
        //last section
        chunk.AddTiles(height, Mathf.Min(width, chunk.GetEndPosition().x - start.x), start + Vector3Int.up * (height + m_ceilOffset), false);
        AddCeilTraps(trap, groundWidth, new Vector3Int(groundStart, start.y + m_ceilOffset), chunk);
        //draws tiles
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) =>
        {
            AddLandscape(chunk, groundTiles, m_ceilOffset, true);
            foreach (var (obj, pos) in coins)
            {
                obj.transform.position = pos;
            }
        });
    }
    /// <summary>
    /// Adds a series of traps to the ceiling
    /// </summary>
    /// <param name="trap">trap type</param>
    /// <param name="width">ceiling width</param>
    /// <param name="start">ceiling start</param>
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
