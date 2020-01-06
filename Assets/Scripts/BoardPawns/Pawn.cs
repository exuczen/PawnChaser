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
    [SerializeField] private SpriteRenderer _pathSpritePrefab = default;

    private List<SpriteRenderer> _pathSprites = new List<SpriteRenderer>();
    private List<Vector2Int> _cellsStack = new List<Vector2Int>();

    public int CellsStackCount { get => _cellsStack.Count; }
    public List<Vector2Int> CellsStack { get => _cellsStack; }

    public bool SetPreviousCellPosition(BoardTilemap tilemap)
    {
        if (_cellsStack.Count > 0)
        {
            if (_cellsStack.Count > 1)
            {
                _cellsStack.RemoveAt(_cellsStack.Count - 1);
                if (_pathSprites.Count > 1)
                {
                    SpriteRenderer lastSprite = _pathSprites.PickLastElement();
                    if (lastSprite)
                        Destroy(lastSprite.gameObject);
                }
            }
            BoardTile tile = tilemap.GetTile(transform.position);
            if (tile)
            {
                tile.Content = null;
            }
            Vector2Int currCell = tilemap.WorldToCell(transform.position);
            Vector2Int prevCell = _cellsStack[_cellsStack.Count - 1];
            if (_pathSprites.Count > 0)
            {
                SpriteRenderer lastSprite = _pathSprites.FindLast(sprite => sprite != null);
                if (lastSprite)
                    lastSprite.enabled = false;
            }
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

    public void AddPathSpriteOnCurrentCell(BoardTilemap tilemap, Transform parent)
    {
        if (_cellsStack.Count > 0)
        {
            bool cellChanged = false;
            Vector2Int currCell = _cellsStack[_cellsStack.Count - 1];
            if (_cellsStack.Count > 1)
            {
                Vector2Int prevCell = _cellsStack[_cellsStack.Count - 2];
                cellChanged = currCell != prevCell;
                if (cellChanged)
                {
                    SpriteRenderer lastSprite = _pathSprites.FindLast(sprite => sprite != null);
                    if (lastSprite)
                    {
                        lastSprite.enabled = true;
                    }
                    else if (prevCell == _cellsStack[0] && _pathSprites[0] == null)
                    {
                        _pathSprites[0] = CreatePathSprite(tilemap, _cellsStack[0], parent);
                    }
                }
            }
            if (_cellsStack.Count <= 1 || cellChanged)
            {
                SpriteRenderer sprite = CreatePathSprite(tilemap, currCell, parent);
                sprite.color = Color.black;
                sprite.enabled = false;
                _pathSprites.Add(sprite);
            }
            else
            {
                _pathSprites.Add(null);
            }
        }
    }

    private SpriteRenderer CreatePathSprite(BoardTilemap tilemap, Vector2Int cell, Transform parent)
    {
        SpriteRenderer sprite = Instantiate(_pathSpritePrefab, tilemap.GetCellCenterWorld(cell), Quaternion.identity, parent);
        sprite.color = Color.black;
        return sprite;
    }

    public void DestroyPathSprites()
    {
        for (int i = 0; i < _pathSprites.Count - 2; i++)
        {
            SpriteRenderer sprite = _pathSprites[i];
            if (sprite != null)
            {
                sprite.transform.SetParent(null);
                Destroy(sprite.gameObject);
                _pathSprites[i] = null;
            }
        }
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
