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

    readonly float m_minVerticalDist = 1;
    readonly float m_minHorizontalDist = 5;

    AnimationCurve m_speed;

    enum Trajectory { Horizontal, Vertical, Diagonal, Circular }


    public MovingPlatformStrategy(LevelTheme levelTheme, AnimationCurve speed) : base(levelTheme)
    {
        m_speed = speed;
    }
    /// <summary>
    /// Создает чанк с движущимися платформами
    /// </summary>
    /// <param name="prevChunk">предыдущий чанк</param>
    /// <param name="transitionStrategy"></param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //очищает переход на этот чанк, он не нужен
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
        //время, за которое предыдущая платформа дойдет до следующей
        float prev = 0;

        while (lastPoint.x < end.x - m_levelTheme.m_movingPlatform.GetWidth())
        {
            float speed = m_speed.Evaluate(m_lvlBuilder.LevelProgress());
            Vector3 first = lastPoint;
            Vector3 second;
            MovingPlatform platform = movingPlatform;
            //оставшееся горизонтальное пространство
            float hSpace = end.x - lastPoint.x + m_levelTheme.m_movingPlatform.GetOffset().x;
            //оставшееся вертикальное пространство
            float vSpace = Mathf.Abs(end.y + 1 - m_levelTheme.m_movingPlatform.GetHeight() - lastPoint.y);

            Trajectory currentTrajectory;
            //выбирает траекторию дивжения новой платформы
            if (vSpace > m_minVerticalDist
                && hSpace > m_minHorizontalDist)
            {
                //веса траекторий движения платформы
                //горизонталь — чем шире, тем чаще
                float weightH = hSpace;
                //вертикаль  — чем выше, тем чаще
                float weightV = vSpace;
                //диагональ — чем квадратнее, тем чаще
                float weightD = Mathf.Min(vSpace, hSpace);
                //круговая — растёт с размерами чанка
                float weightC = (height + vSpace) * 0.5f;        

                float pick = Random.value * (weightV + weightH + weightD + weightC);
                if (pick < weightH)
                {
                    currentTrajectory = Trajectory.Horizontal;
                }
                else if (pick < weightH + weightV)
                {
                    currentTrajectory = Trajectory.Vertical;
                }
                else if (pick < weightV + weightH + weightD)
                {
                    currentTrajectory = Trajectory.Diagonal;
                }
                else
                {
                    currentTrajectory = Trajectory.Circular;
                }
            }
            //если мало места - выбирает траекторию в зависимости от оставшегося пространства
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
                    //если начальная точка предыдущей платформы  не рядом с новой
                    if (prev != 0)
                    {
                        //сколько минимальных проходов платформы можно уложить во время прохода предыдущей платформы
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (m_minWidth / speed + movingPlatform.GetWaitTime()));
                        //сколько максимальных проходов можно уложить в пространство и чтобы совпадало со временем прохода предыдущей платформы
                        int maxN = Mathf.FloorToInt((Mathf.Min(m_maxWidth, vSpace) / speed + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //количество проходов платформы, пока она не встретиться с предыдущей
                        int n;
                        if (Random.value > 0.5f)
                        {
                            n = Random.Range(1, minN + 1);
                            //определяет следующую точку
                            //время n проходов и их ожиданий должно быть равно времени прохода предыдущей платформы
                            second = first + Vector3.up * speed * (height < 0 ? -1 : 1) * (prev - (n - 1) * movingPlatform.GetWaitTime()) / n;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //определяет следующую точку
                            //время прохода платформы должно быть равно n проходам предыдущей платформы
                            second = first + Vector3.up * speed * (height < 0 ? -1 : 1) * (n * prev + (n - 1) * movingPlatform.GetWaitTime());
                        }

                        platform = PlacePlatform(movingPlatform, first, second, ref prev, speed, n % 2 != 0);
                    }
                    else
                    {
                        //размер прохода рандомный в зависимости оставшегося пространства
                        second = first + Vector3.up * (height < 0 ? -1 : 1) * Mathf.Min(Random.Range(m_minWidth, m_maxWidth), vSpace);
                        platform = AddPlatform(movingPlatform, first, second);
                        prev = Mathf.Abs(second.y - first.y) / speed;
                    }


                    lastPoint = second;
                    break;

                case Trajectory.Horizontal:

                    if (prev != 0)
                    {
                        //сколько минимальных проходов платформы можно уложить во время прохода предыдущей платформы
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (m_minWidth / speed + movingPlatform.GetWaitTime()));
                        //сколько максимальных проходов можно уложить в пространство и чтобы совпадало со временем прохода предыдущей платформы
                        int maxN = Mathf.FloorToInt((Mathf.Min(m_maxWidth, hSpace) / speed + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //количество проходов платформы, пока она не встретиться с предыдущей
                        int n;
                        if (Random.value > 0.5f)
                        {
                            n = Random.Range(1, minN + 1);
                            //определяет следующую точку
                            //время n проходов и их ожиданий должно быть равно времени прохода предыдущей платформы
                            second = first + Vector3.right * speed * (prev - (n - 1) * movingPlatform.GetWaitTime()) / n;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //определяет следующую точку
                            //время прохода платформы должно быть равно n проходам предыдущей платформы
                            second = first + Vector3.right * speed * (n * prev + (n - 1) * movingPlatform.GetWaitTime());
                        }
                        platform = PlacePlatform(movingPlatform, first, second, ref prev, speed, n % 2 != 0);
                    }
                    else
                    {
                        //размер прохода рандомный в зависимости оставшегося пространства
                        second = first + Vector3.right * Mathf.Min(Random.Range(m_minWidth, m_maxWidth), hSpace);
                        platform = AddPlatform(movingPlatform, first, second);
                        prev = (second.x - first.x) / speed;
                    }

                    lastPoint = second;
                    break;

                case Trajectory.Diagonal:

                    if (prev != 0)
                    {
                        //сколько минимальных проходов платформы можно уложить во время прохода предыдущей платформы
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (Mathf.Sqrt(Mathf.Pow(m_minWidth, 2) + Mathf.Pow(m_minWidth, 2)) / speed + movingPlatform.GetWaitTime()));
                        //сколько максимальных проходов можно уложить в пространство и чтобы совпадало со временем прохода предыдущей платформы
                        int maxN = Mathf.FloorToInt((Mathf.Min(Mathf.Sqrt(Mathf.Pow(m_maxWidth, 2) + Mathf.Pow(m_maxWidth, 2)), Mathf.Min(vSpace, hSpace)) / speed
                            + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //количество проходов платформы, пока она не встретиться с предыдущей
                        int n;
                        if (Random.value > 0.5f)
                        {
                            n = Random.Range(1, minN + 1);
                            //время прохода новой платформы - 1/n времени прохода предыдущей платформы 
                            prev = (prev - (n - 1) * movingPlatform.GetWaitTime()) / n;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //время прохода новой платформы - время n проходов предыдущей платформы
                            prev = n * prev + (n - 1) * movingPlatform.GetWaitTime();
                        }
                        //вычисляет координаты диагонали по теореме Пифагора
                        //длина диагонали - prev * speed
                        //величина одной координаты
                        float a = Mathf.Sqrt(Random.Range(m_minWidth * m_minWidth, prev * prev * speed * speed));
                        //величина другой координаты
                        float b = Mathf.Sqrt(prev * prev * speed * speed - a);
                        //расставляет рандомно координаты
                        second = first + (Random.value > 0.5f ? new Vector3(a, b * (height < 0 ? -1 : 1)) : new Vector3(b, a * (height < 0 ? -1 : 1)));
                        //в зависимости от количества проходов определяется начало платформы
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
                        //размер прохода рандомный в зависимости оставшегося пространства
                        second = first +
                        new Vector3(Mathf.Min(Random.Range(m_minWidth / 2, m_maxWidth / 2), hSpace), (height < 0 ? -1 : 1) * Mathf.Min(Random.Range(m_minWidth / 2, m_maxWidth / 2), vSpace));
                        platform = AddPlatform(movingPlatform, first, second);
                        prev = Mathf.Sqrt(Mathf.Pow(second.y - first.y, 2) + Mathf.Pow(second.x - first.x, 2)) / speed;
                    }

                    lastPoint = second;
                    break;

                case Trajectory.Circular:
                    //длина полукруга траектории
                    float d;
                    Vector3 fifth;
                    //движение по часовой или против часовой
                    bool reverse = false;
                    if (prev != 0)
                    {
                        //сколько минимальных полукругов траектории можно уложить во время прохода предыдущей платформы
                        int minN = Mathf.FloorToInt((prev + movingPlatform.GetWaitTime()) / (Mathf.Sqrt(Mathf.Pow(m_minWidth, 2) + Mathf.Pow(m_minWidth, 2)) / speed + movingPlatform.GetWaitTime()));
                        //сколько максимальных полукругов траектории можно уложить в пространство и чтобы совпадало со временем прохода предыдущей платформы
                        int maxN = Mathf.FloorToInt((Mathf.Min(Mathf.Sqrt(Mathf.Pow(m_maxWidth, 2) + Mathf.Pow(m_maxWidth, 2)), Mathf.Min(vSpace, hSpace)) / speed
                            + movingPlatform.GetWaitTime()) / (prev + movingPlatform.GetWaitTime()));
                        //количество проходов платформы, пока она не встретиться с предыдущей
                        int n;
                        if (Random.value > 0.5f)
                        {
                            n = Random.Range(1, minN + 1);
                            //время прохождения полукруга траектории - 1/n времени прохода предыдущей платформы 
                            //вычитсляется длина полукруга
                            d = (prev - (n - 1) * movingPlatform.GetWaitTime()) / n * speed;
                        }
                        else
                        {
                            n = Random.Range(1, maxN + 1);
                            //время прохождения полукруга траектории - время n проходов предыдущей платформы
                            //вычитсляется длина полукруга
                            d = n * prev * speed + (n - 1) * movingPlatform.GetWaitTime() * speed;
                        }
                        //направление движения зависит от n 
                        reverse = n % 2 != 0;
                    }
                    else
                    {
                        //длинна полукруга траектории рандомная в зависимости оставшегося пространства
                        d = Mathf.Min(Random.Range(m_minWidth, m_maxWidth), Mathf.Min(vSpace, hSpace));
                    }
                    //точки движения платформы
                    second = first + new Vector3(1, 1) * d / 4;
                    Vector3 third = first + new Vector3(1, 1) * d / 2;
                    Vector3 fourth = first + new Vector3(d * 3, d) / 4;
                    fifth = first + Vector3.right * d;
                    Vector3 sixth = first + new Vector3(d * 3, -d) / 4;
                    Vector3 seventh = first + new Vector3(d, -d) / 2;
                    Vector3 eighth = first + new Vector3(d, -d) / 4;
                    //распологает точки для платформы в зависимости от направления движения
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
        //подгоняет конец чанка под конец последней платформы
        end = new Vector3Int(Mathf.CeilToInt(lastPoint.x) - 1, Mathf.CeilToInt(lastPoint.y) - 1);
        chunk.SetEndPosition(end);
        //создает границу для падения игрока
        chunk.AddEnviromentObject(CreateHorizontalBounds(start, end, end.x - start.x, height));
        //создает боковые границы под предыдущим и следующим чанками
        CreateSideBound(chunk, height < 0);

        chunk.AddTransition(new Chunk(end, end));
        prevChunk.AddTransition(transition);

        return chunk;
    }
    /// <summary>
    /// Создает платформу и добавляет ей чекпоинт
    /// </summary>
    /// <param name="prefab">префаб платформы</param>
    /// <param name="first">первая точка платформы</param>
    /// <param name="second">вторая точка платформы</param>
    /// <returns>созданная платформа</returns>
    MovingPlatform AddPlatform(MovingPlatform prefab, Vector3 first, Vector3 second)
    {
        MovingPlatform platform = Object.Instantiate(prefab, first, Quaternion.identity);
        platform.AddCheckpoint(second);
        return platform;
    }
    /// <summary>
    /// Создает и размещает платформу, задает ее характеристики
    /// </summary>
    /// <param name="prefab">префаб платформы</param>
    /// <param name="first">первая точка платформы</param>
    /// <param name="second">вторая точка платформы</param>
    /// <param name="prev"></param>
    /// <param name="speed">скорость платформы</param>
    /// <param name="isStrat">стоит ли платформа в начале</param>
    /// <returns>созданная платформа</returns>
    MovingPlatform PlacePlatform(MovingPlatform prefab, Vector3 first, Vector3 second, ref float prev, float speed, bool isStrat)
    {
        MovingPlatform platform = AddPlatform(prefab, isStrat ? second : first, isStrat ? first : second);
        platform.SetSpeed(speed);

        //добавляет чекпоинт в нужном порядке
        if (isStrat)
            platform.AddCheckpoint(first);
        else
            platform.AddCheckpoint(second);

        //если платформа в начале - ставит время прохода, ингаче обнуляет
        prev = isStrat ? 0f : Vector3.Distance(first, second) / speed;
        return platform;
    }
    /// <summary>
    /// Создает переход между чанками, состоящий из движущейся платформаы,
    /// которая начинает двигаться, когда игрок на нее ступит
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

        //создает границу для падения игрока
        transition.AddEnviromentObject(CreateHorizontalBounds(transition.GetStartPosition(), end, width, height));

        return transition;
    }
}
