using MustHave;
using MustHave.Utilities;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardTilemap : GridTilemap<BoardTile>
{
    private Transform _pawnsContainer = default;
    private Transform _targetsContainer = default;

    protected override void OnAwake()
    {
        Board board = GetComponentInParent<Board>();
        _pawnsContainer = board.PawnsContainer;
        _targetsContainer = board.TargetsContainer;
    }

    protected override BoardTile CreateTile(int x, int y)
    {
        BoardTile tile = Instantiate(_tiles[0]);
        tile.color = Color.Lerp(Color.HSVToRGB(0f, 0f, 0.7f), Color.HSVToRGB(0f, 0f, 0.8f), UnityEngine.Random.Range(0f, 1f));
        //tile.transform = Matrix4x4.Rotate(Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 4) * 90f));
        return tile;
    }

    protected override void OnShiftEnd()
    {
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            BoardTile boardTile = GetTile(pawnTransform.position);
            if (boardTile)
                boardTile.Content = pawnTransform;
        }
    }

    public Bounds2Int GetChildrenCellBounds(Vector2Int min, Vector2Int max, Transform parent)
    {
        foreach (Transform child in parent)
        {
            Vector2Int cell = WorldToCell(child.position);
            min = Maths.Min(cell, min);
            max = Maths.Max(cell, max);
        }
        return new Bounds2Int(min, max - min + Vector2Int.one);
    }

    public Bounds2Int GetPawnsCellBounds()
    {
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);

        Bounds2Int bounds = GetChildrenCellBounds(min, max, _pawnsContainer);
        bounds = GetChildrenCellBounds(bounds.Min, bounds.Max, _targetsContainer);
        bounds.Min -= Vector2Int.one;
        bounds.Max += Vector2Int.one;
        return bounds;
    }
}
