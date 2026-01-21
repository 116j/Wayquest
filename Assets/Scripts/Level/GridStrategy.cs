using System.Collections.Generic;
using UnityEngine;

public class GridStrategy : FillStrategy
{
    protected int m_maxChunkSize = 50;
    protected int m_minChunkSize = 20;

    protected new int m_minTransitionWidth = 4;
    protected new int m_maxTransitionWidth = 10;
    protected int m_minTransitionHeight = 11;
    protected new int m_maxTransitionHeight = 25;

    //Мин длина платформа
    readonly int m_minWidth = 1;
    //Макс длина платформы
    readonly int m_maxWidth = 6;
    //Мин расстояние между платформами
    readonly int m_minDist = 3;

    //Макс количество попыток генерации чанка
    readonly int m_maxAttempts = 3;
    //Макс количество попыток генерации платформы
    readonly int m_platformMaxAttempts = 100;

    readonly float m_negativeChunkHeightChance = 0.7f;

    public GridStrategy(LevelTheme levelTheme) : base(levelTheme)
    {
    }
    /// <summary>
    /// Созддает чанк состоящий из плтформ, ведущий вверх или вниз, по которым нужно прыгать, 
    /// добавляет ландшавт и отрисовывает
    /// </summary>
    /// <param name="prevChunk">предыдущий чанк</param>
    /// <param name="transitionStrategy">стратегия построения перехода на следующий чанк</param>
    /// <returns></returns>
    public override Chunk FillChunk(Chunk prevChunk, FillStrategy transitionStrategy)
    {
        //рисует тайлы перехода от предыдущего чанка к этому
        Chunk transition = new Chunk(prevChunk.GetEndPosition(), prevChunk.GetEndPosition());

        int width = Random.Range(m_minChunkSize, m_maxChunkSize);
        int height = Random.Range(m_minChunkSize, m_maxChunkSize);
        if (Random.value > m_negativeChunkHeightChance)
        {
            height = -height;
        }
        Vector3Int start = prevChunk.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk chunk = new Chunk(start, end, transition);
        int attempts = 0;
        //генерирует уровень m_maxAttempts раз, если не получаеятся, то пробует другую стратегию
        while (!MakeGrid(chunk))
        {
            attempts++;
            chunk.ClearGrid();
            if (attempts >= m_maxAttempts)
            {
                Debug.Log("Grid failed");
                return null;
            }
        }
        prevChunk.GetNextTransition().Clear(m_editor);

        //создает границы для падения игрока
        chunk.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));

        CreateSideBound(chunk, height < 0);

        prevChunk.AddTransition(transition);
        chunk.AddTransition(new Chunk(end, end));
        chunk.DrawTiles(m_editor, (HashSet<Vector3Int> groundTiles) => AddLandscape(chunk, groundTiles, int.MaxValue, false));
        return chunk;
    }
    /// <summary>
    /// Создает переход наверх из маленьких платформ
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public override Chunk FillTransition(Chunk chunk)
    {
        int width = Random.Range(m_minTransitionWidth, m_maxTransitionWidth);
        int height = Random.Range(m_minTransitionHeight, m_maxTransitionHeight);
        Vector3Int start = chunk.GetEndPosition();
        Vector3Int end = new Vector3Int(start.x + width, start.y + height);
        Chunk transition = new Chunk(start, end);

        Vector3Int lastPoint = start + Vector3Int.right;
        int platformWidth = (width - 2) / 2;
        int horOffset = width - 2 - platformWidth * 2;
        int vertOffset = Random.Range(m_minDist, GetJumpHeight(horOffset));
        bool posOffset = true;
        do
        {
            transition.CreatePlatform(lastPoint, platformWidth);
            lastPoint += new Vector3Int((posOffset ? 1 : -1) * (platformWidth + horOffset), vertOffset);
            posOffset = !posOffset;
        }
        while (lastPoint.y < end.y);
        //создает границы для падения игрока
        transition.AddEnviromentObject(CreateHorizontalBounds(start, end, width + 1, height));

        return transition;
    }
    /// <summary>
    /// Создает сетку из платформ
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns>удалось ли создать сетку из платформ</returns>
    bool MakeGrid(Chunk chunk)
    {
        Vector3Int start = chunk.GetStartPosition();
        Vector3Int end = chunk.GetEndPosition();
        Vector3Int lastPoint = start;
        //позиция последней второй платформы
        Vector3Int lastOffset = start;
        int chunkHeight = end.y - start.y;
        int attempts = 0;
        int lastWidth = 0;
        int w1 = 0;
        int secondaryOffsetY;

        while (attempts < m_platformMaxAttempts)
        {
            //проверяет не достигли ли конца чанка
            if ((lastPoint.x + lastWidth >= end.x - m_minWidth) &&
                (Mathf.Abs(lastPoint.y - end.y) <= m_playerJumpHeight))
            {
                return true;
            }

            //вериикальный отступ платформы
            int offsetY = (lastPoint.y > end.y ? -1 : 1) * Random.Range(m_minDist, m_playerJumpHeight);
            //отступ платформы по сравнению с оставшейся высотой
            float progress = Mathf.Abs(offsetY) * 1.0f / Mathf.Max(1, Mathf.Abs(end.y - lastPoint.y));
            //возможная позиция X
            int x = lastPoint.x + lastWidth + (int)(progress * (end.x - lastPoint.x - lastWidth));

            //длина прыжка игрока на новую платформу в зависимости от высоты вертикального отступа
            int jumpWidth = lastPoint.y < end.y ? GetJumpWidth(offsetY) : m_playerJumpWidth;
            int offsetX = x - lastPoint.x - lastWidth;
            //если расстояние между платформами меньше ширины прыжка
            if (offsetX < jumpWidth)
            {
                offsetX = Random.Range(-jumpWidth, jumpWidth);
            }
            else
            {   
                int minOffset = jumpWidth - offsetX + m_minWidth;
                int maxOffset = offsetX - m_minWidth;
                offsetX = Random.Range(minOffset, maxOffset);
            }
            //обрезает горизонтальный отступ по границам чанка
            offsetX = Mathf.Clamp(offsetX, start.x - x + m_minWidth, end.x - x - m_minWidth * 2);
            //проверяет окрестности на стокновение с платформами
            Vector3Int pos = new Vector3Int(x + offsetX, lastPoint.y + offsetY);

            //проверка платформы на жизнеспособность и подбор ширины платформы
            if (!CheckSurroundings(chunk, pos) || Mathf.Abs(lastPoint.x + lastWidth - pos.x) > GetJumpWidth(offsetY) ||
                !AvailablePlatformWidth(chunk, pos, GetMaxWidth(chunk, pos), ref lastWidth, end) ||
                pos.x > end.x - m_minWidth * 2 || pos.x <= start.x)
            {
                attempts++;
                continue;
            }

            //новая платформа
            lastWidth = Mathf.Clamp(lastWidth, m_minWidth, end.x - pos.x - m_minWidth);
            lastPoint = pos;
            chunk.CreatePlatform(lastPoint, lastWidth);

            //если знак горизонтального отступа платформы и знак высоты чанка совпадают
            bool offsetDirection = offsetX * chunkHeight >= 0;
            secondaryOffsetY = (offsetDirection ? -1 : 1) * Random.Range(m_minDist - 1, m_playerJumpHeight);

            int secondaryOffsetX;
            if (offsetX > 0)
            {
                //при положительном отступе больше склоняется к правому отступу
                int min = offsetDirection ? Mathf.Clamp(-m_minDist + 1, lastOffset.x + w1 - lastPoint.x + m_minWidth, 0) : -m_minDist + 1;
                secondaryOffsetX = Random.Range(min, GetJumpWidth(secondaryOffsetY) + lastWidth);
            }
            else
            {
                //при отрицательном отступе больше склоняется к левому отступу
                int min = offsetDirection ? Mathf.Clamp(-GetJumpWidth(secondaryOffsetY) - m_minWidth,
                                            lastOffset.x + w1 - lastPoint.x + m_minWidth, 0) :
                                            -GetJumpWidth(secondaryOffsetY) - m_minWidth;
                secondaryOffsetX = Random.Range(min, m_minDist);
            }
            //обрезает горизонтальный отступ по границам чанка
            secondaryOffsetX = Mathf.Clamp(secondaryOffsetX, start.x - lastPoint.x + m_minWidth, end.x - lastPoint.x - m_minWidth * 2);
            //проверяем окрестности на стокновение с платформами
            pos = new Vector3Int(lastPoint.x + secondaryOffsetX, lastPoint.y + secondaryOffsetY);

            //проверка платформы на жизнеспособность и подбор ширины платформы
            if (!CheckSurroundings(chunk, pos) || !AvailablePlatformWidth(chunk, pos, GetMaxWidth(chunk, pos), ref w1, end) ||
                pos.x > end.x - m_minWidth || pos.x <= start.x)
            {
                attempts++;
                continue;
            }   

            //вторая платформа
            w1 = Mathf.Clamp(w1, m_minWidth, end.x - pos.x - m_minWidth);
            chunk.CreatePlatform(pos, w1);
            lastOffset = pos;
            //сбрасывает попытки после успешной генерации
            attempts = 0;
        }

        return false;
    }
    /// <summary>
    /// Проверяет, можно ли разместить платформу и подбирает для нее длину
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="currentPos">позиция начала платформы</param>
    /// <param name="maxWidth">макс длина платформы</param>
    /// <param name="currentWidth">переменная для длина</param>
    /// <param name="end">конец чанка</param>
    /// <returns>подходит ли платформа и нашлась ли для нее длина</returns>
    bool AvailablePlatformWidth(Chunk chunk, Vector3Int currentPos, int maxWidth, ref int currentWidth, Vector3Int end)
    {
        if (maxWidth < m_minWidth) return false;

        for (int i = 0; i < m_playerJumpHeight; i++)
        {
            //если платформа будет перекрывать платфорому снизу на расстоянии прыжка - не подходит
            if (CheckVerticalCollision(chunk, currentPos, i))
                return false;
            //если на расстоянии нижнего отступа i находится другая платформа - подбирает длину с учетом нее
            if (chunk.PositionIsUsed(new Vector3Int(currentPos.x, currentPos.y + i)))
                return SelectPlatformWidthWithOffset(chunk, currentPos, maxWidth, ref currentWidth, end, i);
            //если на расстоянии длины maxWidth и нижнего ортступа находится платформа - пробует подобрать длину с учетом нее
            if (CheckHorizontalCollision(chunk, currentPos, maxWidth, i, ref currentWidth, end))
                return true;
        }

        currentWidth = Random.Range(m_minWidth, maxWidth);
        return true;
    }
    /// <summary>
    /// Подбирает длину платформы, чтобы она перекрывала справа другую платформу
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">начало платформы</param>
    /// <param name="maxWidth">макс длина платформы</param>
    /// <param name="width">переменная для записи длины</param>
    /// <param name="end">конец чанка</param>
    /// <param name="offset">вертикальный отступ, где находится другая платформа</param>
    /// <returns>удалось ли побобрать длину</returns>
    bool SelectPlatformWidthWithOffset(Chunk chunk, Vector3Int pos, int maxWidth, ref int width, Vector3Int end, int offset)
    {
        for (int i = m_minWidth; i < maxWidth; i++)
        {
            //когда другая платформа закончится
            if (!chunk.PositionIsUsed(new Vector3Int(pos.x + i, pos.y + offset)))
            {
                //если слишком мало места до конца или длина больше макс длины - не подходит
                if (pos.x + i + 1 > end.x - m_minWidth || i + 1 == Mathf.Clamp(maxWidth, 0, end.x - pos.x) - 1)
                    return false;
                //платформа должна быть длиннее нижней млатформы
                width = Random.Range(i + 1, Mathf.Clamp(maxWidth, 0, end.x - pos.x));
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Смотрит наличие платформы под отступом внизу на растоянни макс длины1
    /// и подбирате длину в зависимости от платформы, если платформа есть
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">начало платформы</param>
    /// <param name="maxWidth">макс длина платформы</param>
    /// <param name="verticalOffset">вертикальный отступ</param>
    /// <param name="width">переменная для записи длины</param>
    /// <param name="end">конец чанка</param>
    /// <returns>если длина определена с учетом найденной платформы</returns>
    bool CheckHorizontalCollision(Chunk chunk, Vector3Int pos, int maxWidth, int verticalOffset, ref int width, Vector3Int end)
    {
        for (int i = 0; i < maxWidth; i++)
        {
            //если на данной длине под отступом находится платформа
            if (chunk.PositionIsUsed(new Vector3Int(pos.x + i, pos.y - verticalOffset)))
            {
                for (int j = m_minWidth; j <= maxWidth - i; j++)
                {
                    //смотрит когда платформа закончится
                    if (!chunk.PositionIsUsed(new Vector3Int(pos.x + i + j, pos.y - verticalOffset)))
                    {
                        width = Random.Range(m_minWidth, Mathf.Clamp(i + j, 0, end.x - pos.x));
                        return true;
                    }
                }
            }
        }
        return false;
    }
    /// <summary>
    /// Проверяет, перекрывает ли платформа другую платформу с отступом offset
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">начало платформы</param>
    /// <param name="offset">вертикальный отступ от платформы</param>
    /// <returns></returns>
    bool CheckVerticalCollision(Chunk chunk, Vector3Int pos, int offset)
    {
        return chunk.PositionIsUsed(new Vector3Int(pos.x, pos.y - offset)) &&
               !chunk.PositionIsUsed(new Vector3Int(pos.x - 1, pos.y - offset)) &&
               !chunk.PositionIsUsed(new Vector3Int(pos.x + 1, pos.y - offset));
    }
    /// <summary>
    /// Определяет максимальную длину для платформы
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">начало платформы</param>
    /// <returns></returns>
    int GetMaxWidth(Chunk chunk, Vector3Int pos)
    {
        for (int i = 0; i < m_maxWidth; i++)
        {
            //если на данной длине есть пересечения - макс длина
            if (CheckInterferingPlarforms(chunk, pos, i))
                return i;
        }
        return m_maxWidth;
    }
    /// <summary>
    /// Смотрит для определенной длины (offset + 1), есть ли другие платформы в радиусе
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">начало платформы</param>
    /// <param name="offset">горизонтальный отступ - предполагаемый конец платформы</param>
    /// <returns></returns>
    bool CheckInterferingPlarforms(Chunk chunk, Vector3Int pos, int offset)
    {
        for (int i = 0; i < m_minDist; i++)
        {
            //если для данной длины мверху, справа или снизу на расстоянии i есть платформа 
            if (chunk.PositionIsUsed(new Vector3Int(pos.x + offset + i, pos.y)) ||
                chunk.PositionIsUsed(new Vector3Int(pos.x + offset, pos.y - i)) ||
                chunk.PositionIsUsed(new Vector3Int(pos.x + offset, pos.y + i)))
            {
                return true;
            }
        }
        // если впритык к платформе в радиусе 1 справа есть другая платформа
        return chunk.PositionIsUsed(new Vector3Int(pos.x + offset + 1, pos.y + 1)) ||
               chunk.PositionIsUsed(new Vector3Int(pos.x + offset + 1, pos.y - 1));
    }
    /// <summary>
    /// Смотрит, есть ли вокруг другие платформы
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="pos">начало платформы</param>
    /// <returns>true - вокруг нет других платформ</returns>
    bool CheckSurroundings(Chunk chunk, Vector3Int pos)
    {
        for (int x = -m_minDist + 1; x < m_minDist; x++)
        {
            for (int y = -m_minDist + 1; y < m_minDist; y++)
            {
                if (chunk.PositionIsUsed(new Vector3Int(pos.x + x, pos.y + y)))
                    return false;
            }
        }
        return true;
    }
}