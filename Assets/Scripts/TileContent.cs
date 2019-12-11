using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileContent : MonoBehaviour
{
    public T CreateInstance<T>(Vector2Int cellXY, BoardTilemap tilemap, Transform parent) where T : TileContent
    {
        return Instantiate(this as T, tilemap.GetCellCenterWorld(cellXY), Quaternion.identity, parent);
    }
}
