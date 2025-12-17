using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TilePlaceAnalog : ScriptableObject
{
    //Тайлы для темной темы
    public TileBase[] dark;
    //Тайлы для болотной темы
    public TileBase[] calm;
    //Тег тайлмеп
    public string tilemapTag;
    //Есть ли у тайла соседние тайлы
    public bool[] surroundings;
    //Если два аналога с одинаковыми surroundings - другой аналог
    public TilePlaceAnalog placeAnalog;
}
