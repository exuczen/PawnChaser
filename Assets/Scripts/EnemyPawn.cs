using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnemyPawnData
{
    [SerializeField] public Vector2Int cell;
    [SerializeField] public Vector2Int targetCell;
}

public class EnemyPawn : Pawn
{
    [SerializeField] private TileContent _target = default;

    public TileContent Target { get => _target; set { _target = value; } }
    public Vector3 TargetPosition { get => _target.transform.position; }

    public void SetTarget(Vector2Int targetCell, BoardTilemap tilemap)
    {
        BoardTile tile = tilemap.GetTile(targetCell);
        if (tile)
        {
            _target = tile.Content;
        }
    }
}
