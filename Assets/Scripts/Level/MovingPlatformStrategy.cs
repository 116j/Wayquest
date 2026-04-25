using UnityEngine;

public class MovingPlatformStrategy : FillStrategy
{
    protected int m_maxChunkSize = 50;
    protected int m_minChunkSize = 25;

    protected new int m_minTransitionWidth = 5;
    protected new int m_maxTransitionWidth = 10;
    protected int m_minTransitionHeight = 11;
    protected new int m_maxTransitionHeight = 25;

    readonly float m_minWidth = 5;
    readonly float m_maxWidth = 15;
    readonly int m_maxAllowedN = 4;
    readonly float m_maxPrev = 2f;

    readonly float m_minVerticalDist = 1;
    readonly float m_minHorizontalDist = 5;

    AnimationCurve m_speed;

    enum Trajectory { Horizontal, Vertical, Diagonal, Circular }


    public MovingPlatformStrategy(LevelTheme levelTheme, AnimationCurve speed) : base(levelTheme)
    {
        m_speed = speed;
    }
    /// <summary>
    /// Creats a chunk with moving platforms
    /// </summary>
    /// <param name="prevChunk">previous chunk</param>
    /// <param name="transitionStrategy"></param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //clears the transition to this chunk
        prevChunk.GetNextTransition().Clear(m_editor);
        Chunk transition = new Chunk(prevChunk.GetEndPosition(), prevChunk.GetEndPosition());

        Vector3Int start = prevChunk.GetEndPosition();
        int width = Random.Range(m_minChunkSize, m_maxChunkSize);
        int height = Random.Range(m_minChunkSize, m_maxChunkSize);
        if (Random.value > 0.5)
        {
            height = -height;
        }
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk chunk = new Chunk(start, end, transition);

        MovingPlatform movingPlatform = m_levelTheme.m_movingPlatform.GetComponent<MovingPlatform>();
        Vector3 lastPoint = start + new Vector3(m_levelTheme.m_movingPlatform.GetWidth(), 1 - m_levelTheme.m_movingPlatform.GetHeight());
        //time it takes for the previous platform to reach the current one
        float prev = 0;

        while (lastPoint.x < end.x - m_levelTheme.m_movingPlatform.GetWidth())
        {
            float speed = m_speed.Evaluate(m_lvlBuilder.LevelProgress());
            Vector3 first = lastPoint;
            Vector3 second;
            MovingPlatform platform = movingPlatform;
            //remaining horizontal space
            float hSpace = end.x - lastPoint.x + m_levelTheme.m_movingPlatform.GetOffset().x;
            //remaining vertical space
            float vSpace = Mathf.Abs(end.y + 1 - m_levelTheme.m_movingPlatform.GetHeight() - lastPoint.y);

            Trajectory currentTrajectory;
            //chooses a trajectory for a platform
            if (vSpace > m_minVerticalDist
                && hSpace > m_minHorizontalDist)
            {
                //weights of the platform movement paths
                //horizontal - the wider, the more often
                float weightH = hSpace;
                //vertical - the higher, the more often
                float weightV = vSpace;
                //diagonal - the squarer, the more often
                float weightD = Mathf.Min(vSpace, hSpace);
                //circular - increases with the size of the chunk
                float weightC = Mathf.Abs(lastPoint.y - start.y) < m_minWidth / 2 ? 0 : (height + vSpace) * 0.5f;

                float pick = Random.value * (weightV + weightH + weightD + weightC);
                if (pick < weightH)
                {
                    currentTrajectory = Trajectory.Horizontal;
                }
                else if (pick < weightH + weightV)
                {
                    currentTrajectory = Trajectory.Vertical;
                }
                else if (weightC == 0 || pick < weightV + weightH + weightD)
                {
                    currentTrajectory = Trajectory.Diagonal;
                }
                else
                {
                    currentTrajectory = Trajectory.Circular;
                }
            }
            //if there is not enough space - chooses a trajectory depending on the remaining space.
            else
            {
                if (vSpace > m_minVerticalDist
                && vSpace < m_maxWidth
                && hSpace < m_minHorizontalDist)
                {
                    currentTrajectory = Trajectory.Vertical;
                }
                else if (vSpace < m_minVerticalDist
                && hSpace > m_minHorizontalDist
                && hSpace < m_maxWidth)
                {
                    currentTrajectory = Trajectory.Horizontal;
                }
                else
                {
                    currentTrajectory = Trajectory.Diagonal;
                }
            }

            switch (currentTrajectory)
            {
                case Trajectory.Vertical:
                    //if the starting point of the previous platform is not near the new one
                    if (prev != 0)
                    {
                        //how many min platform passes can be laid during the passage of the previous platform
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (m_minWidth / speed + movingPlatform.GetWaitTime()));
                        //how many max passages can be placed in the space and so that it coincides with the passage time of the previous platform
                        int maxN = Mathf.FloorToInt((Mathf.Min(m_maxWidth, vSpace) / speed + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //number of passes of the platform until it meets the previous one
                        minN = Mathf.Min(minN, m_maxAllowedN);
                        maxN = Mathf.Min(maxN, m_maxAllowedN);
                        int n;
                        if (prev > m_maxPrev)
                        {
                            n = Random.Range(1, minN + 1);
                            //defines the next point
                            //the time of n passes and their waits should be equal to the time of the passage of the previous platform
                            second = first + Vector3.up * speed * (height < 0 ? -1 : 1) * (prev - (n - 1) * movingPlatform.GetWaitTime()) / n;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //defines the next point
                            //the passage time of the platform must be equal to n passes of the previous platform
                            second = first + Vector3.up * speed * (height < 0 ? -1 : 1) * (n * prev + (n - 1) * movingPlatform.GetWaitTime());
                        }

                        platform = PlacePlatform(movingPlatform, first, second, ref prev, speed, n % 2 != 0);
                    }
                    else
                    {
                        //size of the passage is random depending on the remaining space
                        second = first + Vector3.up * (height < 0 ? -1 : 1) * Mathf.Min(Random.Range(m_minWidth, m_maxWidth), vSpace);
                        platform = AddPlatform(movingPlatform, first, second);
                        prev = Mathf.Abs(second.y - first.y) / speed;
                    }


                    lastPoint = second;
                    break;

                case Trajectory.Horizontal:

                    if (prev != 0)
                    {
                        //how many min platform passes can be laid during the passage of the previous platform
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (m_minWidth / speed + movingPlatform.GetWaitTime()));
                        //how many max passages can be placed in the space and so that it coincides with the passage time of the previous platform
                        int maxN = Mathf.FloorToInt((Mathf.Min(m_maxWidth, hSpace) / speed + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //number of passes of the platform until it meets the previous one
                        minN = Mathf.Min(minN, m_maxAllowedN);
                        maxN = Mathf.Min(maxN, m_maxAllowedN);
                        int n;
                        if (prev > m_maxPrev)
                        {
                            n = Random.Range(1, minN + 1);
                            //defines the next point
                            //the time of n passes and their waits should be equal to the time of the passage of the previous platform
                            second = first + Vector3.right * speed * (prev - (n - 1) * movingPlatform.GetWaitTime()) / n;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //defines the next point
                            //the passage time of the platform must be equal to n passes of the previous platform
                            second = first + Vector3.right * speed * (n * prev + (n - 1) * movingPlatform.GetWaitTime());
                        }
                        platform = PlacePlatform(movingPlatform, first, second, ref prev, speed, n % 2 != 0);
                    }
                    else
                    {
                        //size of the passage is random depending on the remaining space
                        second = first + Vector3.right * Mathf.Min(Random.Range(m_minWidth, m_maxWidth), hSpace);
                        platform = AddPlatform(movingPlatform, first, second);
                        prev = (second.x - first.x) / speed;
                    }

                    lastPoint = second;
                    break;

                case Trajectory.Diagonal:

                    if (prev != 0)
                    {
                        //how many min platform passes can be laid during the passage of the previous platform
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (Mathf.Sqrt(Mathf.Pow(m_minWidth, 2) + Mathf.Pow(m_minWidth, 2)) / speed + movingPlatform.GetWaitTime()));
                        //how many max passages can be placed in the space and so that it coincides with the passage time of the previous platform
                        int maxN = Mathf.FloorToInt((Mathf.Min(Mathf.Sqrt(Mathf.Pow(m_maxWidth, 2) + Mathf.Pow(m_maxWidth, 2)), Mathf.Min(vSpace, hSpace)) / speed
                            + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //number of passes of the platform until it meets the previous one
                        minN = Mathf.Min(minN, m_maxAllowedN);
                        maxN = Mathf.Min(maxN, m_maxAllowedN);
                        int n;
                        if (prev > m_maxPrev)
                        {
                            n = Random.Range(1, minN + 1);
                            //passage time of the new platform - 1/n of the passage time of the previous platform 
                            prev = (prev - (n - 1) * movingPlatform.GetWaitTime()) / n;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //passage time of the new platform - the time of n passes of the previous platform
                            prev = n * prev + (n - 1) * movingPlatform.GetWaitTime();
                        }
                        //calculates the coordinates of the diagonal according to the Pythagorean theorem
                        //diagonal length - prev * speed
                        //value of one coordinate
                        float a = Mathf.Sqrt(Random.Range(m_minWidth * m_minWidth, prev * prev * speed * speed));
                        //value of another coordinate
                        float b = Mathf.Sqrt(prev * prev * speed * speed - a);
                        //randomly assigns coordinates
                        second = first + (Random.value > 0.5f ? new Vector3(a, b * (height < 0 ? -1 : 1)) : new Vector3(b, a * (height < 0 ? -1 : 1)));
                        //depending on the number of passages, the beginning of the platform is determined
                        if (n % 2 == 0)
                        {
                            platform = AddPlatform(movingPlatform, first, second);
                        }
                        else
                        {
                            platform = AddPlatform(movingPlatform, second, first);
                            prev = 0;
                        }
                    }
                    else
                    {
                        //size of the passage is random depending on the remaining space
                        second = first +
                        new Vector3(Mathf.Min(Random.Range(m_minWidth / 2, m_maxWidth / 2), hSpace), (height < 0 ? -1 : 1) * Mathf.Min(Random.Range(m_minWidth / 2, m_maxWidth / 2), vSpace));
                        platform = AddPlatform(movingPlatform, first, second);
                        prev = Mathf.Sqrt(Mathf.Pow(second.y - first.y, 2) + Mathf.Pow(second.x - first.x, 2)) / speed;
                    }

                    lastPoint = second;
                    break;

                case Trajectory.Circular:
                    //length of the semicircle of the trajectory
                    float d;
                    Vector3 fifth;
                    //clockwise or counterclockwise movement
                    bool reverse = false;
                    if (prev != 0)
                    {
                        vSpace = Mathf.Min(Mathf.Abs(start.y + 1 - m_levelTheme.m_movingPlatform.GetHeight() - lastPoint.y), vSpace);
                        //how many min semicircles of the trajectory can be laid during the passage of the previous platform
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (Mathf.Sqrt(Mathf.Pow(m_minWidth, 2) + Mathf.Pow(m_minWidth, 2)) / speed + movingPlatform.GetWaitTime()));
                        //how many max semicircles of the trajectory can be placed in the space and so that it coincides with the passage time of the previous platform
                        int maxN = Mathf.FloorToInt((Mathf.Min(Mathf.Sqrt(Mathf.Pow(m_maxWidth, 2) + Mathf.Pow(m_maxWidth, 2)), Mathf.Min(vSpace, hSpace)) / speed
                            + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //number of passes of the platform until it meets the previous one
                        int n;
                        minN = Mathf.Min(minN, m_maxAllowedN);
                        maxN = Mathf.Min(maxN, m_maxAllowedN);
                        if (prev > m_maxPrev)
                        {
                            n = Random.Range(1, minN + 1);
                            //time of passage of the semicircle of the trajectory - 1/n the time of passage of the previous platform 
                            //length of the semicircle is calculated
                            d = (prev - (n - 1) * movingPlatform.GetWaitTime()) / n * speed;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //time of passage of the semicircle of the trajectory - the time of n passes of the previous platform
                            //length of the semicircle is calculated
                            d = n * prev * speed + (n - 1) * movingPlatform.GetWaitTime() * speed;
                        }
                        //direction of movement depends on n 
                        reverse = n % 2 != 0;
                    }
                    else
                    {
                        //length of the semicircle of the trajectory is random depending on the remaining space
                        d = Mathf.Min(Random.Range(m_minWidth, m_maxWidth), Mathf.Min(vSpace, hSpace));
                    }
                    //points of movement of the platform
                    second = first + new Vector3(1, 1) * d / 4;
                    Vector3 third = first + new Vector3(1, 1) * d / 2;
                    Vector3 fourth = first + new Vector3(d * 3, d) / 4;
                    fifth = first + Vector3.right * d;
                    Vector3 sixth = first + new Vector3(d * 3, -d) / 4;
                    Vector3 seventh = first + new Vector3(d, -d) / 2;
                    Vector3 eighth = first + new Vector3(d, -d) / 4;
                    //places points for the platform depending on the direction of movement
                    if (reverse)
                    {
                        platform = Object.Instantiate(movingPlatform, fifth, Quaternion.identity);
                        prev = 0;
                        platform.AddCheckpoint(sixth, false);
                        platform.AddCheckpoint(seventh, false);
                        platform.AddCheckpoint(eighth, false);
                        platform.AddCheckpoint(first);
                        platform.AddCheckpoint(second, false);
                        platform.AddCheckpoint(third, false);
                        platform.AddCheckpoint(fourth, false);
                    }
                    else
                    {
                        platform = Object.Instantiate(movingPlatform, first, Quaternion.identity);
                        prev = 4 * Mathf.Sqrt(d * d / 8) / speed;
                        platform.AddCheckpoint(second, false);
                        platform.AddCheckpoint(third, false);
                        platform.AddCheckpoint(fourth, false);
                        platform.AddCheckpoint(fifth);
                        platform.AddCheckpoint(sixth, false);
                        platform.AddCheckpoint(seventh, false);
                        platform.AddCheckpoint(eighth, false);
                    }

                    lastPoint = fifth;
                    break;
            }

            platform.SetSpeed(speed);
            chunk.AddEnviromentObject(platform.gameObject);
            chunk.CreatePlatform(Vector3Int.CeilToInt(lastPoint) - Vector3Int.right, 2);
            lastPoint += Vector3.right * (m_levelTheme.m_movingPlatform.GetWidth() + 1);
        }
        //adjusts the end of the chunk to the end of the last platform
        end = new Vector3Int(Mathf.CeilToInt(lastPoint.x) - 1, Mathf.CeilToInt(lastPoint.y) - 1);
        chunk.SetEndPosition(end);
        //creates a bound for the player to fall
        chunk.AddEnviromentObject(CreateHorizontalBounds(start, end, end.x - start.x, height));
        //creates side borders under the previous and next chunks
        //CreateSideBound(chunk, height < 0);

        chunk.AddTransition(new Chunk(end, end));
        prevChunk.AddTransition(transition);

        return chunk;
    }
    /// <summary>
    /// Creates a platform and adds an checkpoint to it
    /// </summary>
    /// <param name="prefab">platform prefab</param>
    /// <param name="first">first platform point</param>
    /// <param name="second">second platform point</param>
    /// <returns>created platform</returns>
    MovingPlatform AddPlatform(MovingPlatform prefab, Vector3 first, Vector3 second)
    {
        MovingPlatform platform = Object.Instantiate(prefab, first, Quaternion.identity);
        platform.AddCheckpoint(second);
        return platform;
    }
    /// <summary>
    /// Creates and hosts the platform, sets its characteristics
    /// </summary>
    /// <param name="prefab">platform prefab</param>
    /// <param name="first">first platform point</param>
    /// <param name="second">second platform point</param>
    /// <param name="prev">time it takes for the previous platform to reach the current one</param>
    /// <param name="speed">platform speed</param>
    /// <param name="isStart">is the platform at the first point</param>
    /// <returns>created platform</returns>
    MovingPlatform PlacePlatform(MovingPlatform prefab, Vector3 first, Vector3 second, ref float prev, float speed, bool isStart)
    {
        MovingPlatform platform = AddPlatform(prefab, isStart ? second : first, isStart ? first : second);
        platform.SetSpeed(speed);

        //adds a checkpoint in the required order
        if (isStart)
            platform.AddCheckpoint(first);
        else
            platform.AddCheckpoint(second);

        //if the platform is at the beginning - sets the passage time, otherwise it resets it
        prev = isStart ? 0f : Vector3.Distance(first, second) / speed;
        return platform;
    }
    /// <summary>
    /// Creates a transition between chunks consisting of a moving platform,
    /// which starts moving when the player steps on it
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public override Chunk FillTransition(Chunk chunk)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(m_minTransitionHeight, m_maxTransitionHeight);
        if (Random.value > 0.5)
        {
            height = -height;
        }
        Vector3Int end = new Vector3Int(chunk.GetEndPosition().x + width, chunk.GetEndPosition().y + height);
        Chunk transition = new Chunk(chunk.GetEndPosition(), end);
        transition.DontFillTiles();

        MovingPlatform platform = Object.Instantiate(m_levelTheme.m_movingPlatform, chunk.GetEndPosition() + new Vector3(width * 1.0f / 2, 1 - m_levelTheme.m_movingPlatform.GetHeight()), Quaternion.identity).GetComponent<MovingPlatform>();
        platform.DisableAutoMovement();
        platform.AddCheckpoint(platform.transform.position + Vector3.up * height);
        transition.AddEnviromentObject(platform.gameObject);

        //creates a bound for the player to fall
        transition.AddEnviromentObject(CreateHorizontalBounds(transition.GetStartPosition(), end, width, height));

        return transition;
    }
}
