using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileChanger : ScriptableObject
{
    //Main tiles
    public TileBase[] tiles;
    //Group of analog for tiles
    public TilePlaceAnalog analogTiles;
    //Does a grass need to be added
    public bool addGrass;
    //Four groups of neighboring tiles
    //If there are no restrictions on the surrounding tile - null
    public TileGroup[] changeTiles;
}
