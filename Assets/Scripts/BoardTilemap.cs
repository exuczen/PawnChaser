using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardTilemap : GridTilemap<BoardTile>
{
    [SerializeField] private Transform _pawnsContainer = default;

    protected override Tile CreateTile(int x, int y)
    {
        return Instantiate(_tiles[0]);
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

    protected override void OnStart()
    {
    }
}
