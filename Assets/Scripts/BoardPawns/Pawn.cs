using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MustHave.Utilities;
using System;
using MustHave;
using UnityEngine.Tilemaps;

public class PawnTransition
{
    private Transform _transform = default;
    private Vector2Int _begCell = default;
    private Vector2Int _endCell = default;
    private Vector3 _begPos = default;
    private Vector3 _endPos = default;

    public Vector2Int BegCell { get => _begCell; }
    public Vector2Int EndCell { get => _endCell; }

    public PawnTransition(Pawn pawn, BoardTilemap tilemap, Vector2Int endCell)
    {
        _transform = pawn.transform;
        _begCell = tilemap.WorldToCell(_transform.position);
        _endCell = endCell;
        _begPos = _transform.position;
        _endPos = tilemap.GetCellCenterWorld(endCell);
    }

    public void Update(float transition)
    {
        _transform.position = Vector3.Lerp(_begPos, _endPos, transition);
    }

    public void Finish(BoardTilemap tilemap)
    {
        _transform.position = _endPos;
        BoardTile tile;
        if (tile = tilemap.GetTile(_begCell))
            tile.Content = null;
        if (tile = tilemap.GetTile(_endCell))
            tile.Content = _transform.GetComponent<TileContent>();
    }
}

public class Pawn : TileContent
{
    private List<Vector2Int> _cellsStack = new List<Vector2Int>();

    public int CellsStackCount { get => _cellsStack.Count; }

    public bool SetPreviousCellPosition(BoardTilemap tilemap)
    {
        if (_cellsStack.Count > 0)
        {
            if (_cellsStack.Count > 1)
                _cellsStack.RemoveAt(_cellsStack.Count - 1);

            BoardTile tile = tilemap.GetTile(transform.position);
            if (tile)
            {
                tile.Content = null;
            }
            Vector2Int currCell = tilemap.WorldToCell(transform.position);
            Vector2Int prevCell = _cellsStack[_cellsStack.Count - 1];
            transform.position = tilemap.GetCellCenterWorld(prevCell);
            tile = tilemap.GetTile(prevCell);
            if (tile)
            {
                tile.Content = this;
            }
            return currCell != prevCell;
        }
        return false;
    }

    public void AddCellPositionToStack(BoardTilemap tilemap)
    {
        Vector2Int cell = tilemap.WorldToCell(transform.position);
        _cellsStack.Add(cell);
    }

    public IEnumerator MoveRoutine(BoardTilemap tilemap, Vector2Int destCell, Action onEnd = null)
    {
        PawnTransition pawnTransition = new PawnTransition(this, tilemap, destCell);
        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            pawnTransition.Update(shift);
        });
        pawnTransition.Finish(tilemap);
        onEnd?.Invoke();
    }
}
