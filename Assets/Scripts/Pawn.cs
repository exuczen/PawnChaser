using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MustHave.Utilities;
using System;
using MustHave;
using UnityEngine.Tilemaps;

public class Pawn : TileContent
{
    private List<Vector2Int> _cellsStack = new List<Vector2Int>();

    public void SetPreviousCellPosition(BoardTilemap tilemap)
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
            Vector2Int cell = _cellsStack[_cellsStack.Count - 1];
            transform.position = tilemap.GetCellCenterWorld(cell);
            tile = tilemap.GetTile(cell);
            if (tile)
            {
                tile.Content = transform;
            }
        }
    }

    public void AddCellPositionToStack(BoardTilemap tilemap)
    {
        Vector2Int cell = tilemap.WorldToCell(transform.position);
        _cellsStack.Add(cell);
    }

    public IEnumerator MoveRoutine(BoardTilemap tilemap, Vector2Int destCell, Action onEnd = null)
    {
        Vector2Int cell = tilemap.WorldToCell(transform.position);
        Vector3 begPos = transform.position;
        Vector3 endPos = tilemap.GetCellCenterWorld(destCell);

        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            transform.position = Vector3.Lerp(begPos, endPos, shift);
        });
        transform.position = endPos;
        BoardTile tile;
        if (tile = tilemap.GetTile(cell))
            tile.Content = null;
        if (tile = tilemap.GetTile(destCell))
            tile.Content = transform;

        onEnd?.Invoke();
    }

}
