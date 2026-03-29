using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TilePlaceAnalog : ScriptableObject
{
    //Tiles for a dark theme
    public TileBase[] dark;
    //Tiles for a swamp theme
    public TileBase[] calm;
    //Tilemap tag
    public string tilemapTag;
    //Does the tile have neighboring tiles
    public bool[] surroundings;
    //If there are two analogs with the same surroundings - another analogue is used
    public TilePlaceAnalog placeAnalog;
}
