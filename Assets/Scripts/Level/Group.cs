using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class Group : ScriptableObject
{
    public abstract List<TileBase> GetTiles();
}
