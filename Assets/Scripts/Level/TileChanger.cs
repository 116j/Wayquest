using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileChanger : ScriptableObject
{
    //Главные тайлы
    public TileBase[] tiles;
    //Группа аналогов для тайлов
    public TilePlaceAnalog analogTiles;
    //Нужно ли добавить тайл травы сверху
    public bool addGrass;
    //Четыре группы соседних тайлов
    //Если нет ограничений к окружающему тайлу - null
    public TileGroup[] changeTiles;
}
