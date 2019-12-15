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
    [SerializeField] private EnemyPawnTarget _target = default;

    public EnemyPawnTarget Target { get => _target; set { _target = value; } }
    public Vector3 TargetPosition { get => _target.transform.position; }
    public bool TargetIsOtherPawn { get => _target.GetComponent<EnemyPawn>(); }

    public void SetTarget(Vector2Int targetCell, BoardTilemap tilemap)
    {
        BoardTile tile = tilemap.GetTile(targetCell);
        if (tile)
        {
            if (tile.Content is EnemyPawnTarget)
            {
                _target = tile.Content as EnemyPawnTarget;
            }
            else
            {
                _target = tile.Content.GetComponent<EnemyPawnTarget>();
                if (!_target)
                {
                    _target = tile.Content.gameObject.AddComponent<EnemyPawnTarget>();
                }
            }
        }
    }
}
