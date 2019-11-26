using MustHave;
using MustHave.Utilities;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardTilemap : GridTilemap<BoardTile>
{
    [SerializeField] private Transform _pawnsContainer = default;

    protected override Tile CreateTile(int x, int y)
    {
        Tile tile = Instantiate(_tiles[0]);
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

    public BoundsInt GetPawnsBounds()
    {
        Vector3Int boundsMin = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        Vector3Int boundsMax = new Vector3Int(int.MinValue, int.MinValue, 0);
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            Vector3Int cell = _tilemap.WorldToCell(pawnTransform.position);
            boundsMin.x = Mathf.Min(cell.x, boundsMin.x);
            boundsMin.y = Mathf.Min(cell.y, boundsMin.y);
            boundsMax.x = Mathf.Max(cell.x, boundsMax.x);
            boundsMax.y = Mathf.Max(cell.y, boundsMax.y);
        }
        return new BoundsInt {
            min = boundsMin,
            max = boundsMax
        };
    }
}
