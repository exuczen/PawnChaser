using MustHave.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteInEditMode]
[RequireComponent(typeof(Tilemap))]
public abstract class GridTilemap<T> : MonoBehaviour where T : Tile
{
    [SerializeField] protected Camera _camera = default;
    [SerializeField] protected Tile[] _tiles = default;

    protected Grid _grid = default;
    protected Tilemap _tilemap = default;
    protected readonly List<int> _tileIndexPool = new List<int>();
    protected Vector3Int _cameraCell = default;

    public Camera Camera { get => _camera; set => _camera = value; }
    public Tilemap Tilemap { get => _tilemap; set => _tilemap = value; }

    protected abstract Tile CreateTile(int x, int y);
    protected abstract void OnStart();
    protected abstract void OnShiftEnd();

    private void Start()
    {
        _tilemap = GetComponent<Tilemap>();
        _grid = _tilemap.layoutGrid;
        _cameraCell = _tilemap.WorldToCell(_camera.transform.position);
        FillMapInView();
        OnShiftEnd();
        OnStart();
    }

    public T GetTile(Vector3Int cell)
    {
        return _tilemap.GetTile<T>(cell);
    }

    public T GetTile(Vector3 worldPoint)
    {
        return GetTile(worldPoint, out _);
    }

    public T GetTile(Vector3 worldPoint, out Vector3Int cell)
    {
        //cell = _grid.WorldToCell(worldPoint);
        cell = _tilemap.WorldToCell(worldPoint);
        return GetTile(cell);
    }

    protected Tile GetRandomTile()
    {
        if (_tileIndexPool.Count == 0)
        {
            _tileIndexPool.AddIntRange(0, _tiles.Length);
        }
        int randIndex = UnityEngine.Random.Range(0, _tileIndexPool.Count);
        int tileIndex = _tileIndexPool[randIndex];
        _tileIndexPool.RemoveAt(randIndex);
        return _tiles[tileIndex];
    }

    private void GetHalfRowsAndColsCount(out int halfYCount, out int halfXCount)
    {
        halfYCount = (int)(_camera.orthographicSize / _grid.cellSize.y);
        halfXCount = (halfYCount * Screen.width / Screen.height) + 2;
        halfYCount += 1;
    }

    private void FillMapInView()
    {

        GetHalfRowsAndColsCount(out int halfYCount, out int halfXCount);
        Vector3Int camCurrTilePos = _tilemap.WorldToCell(_camera.transform.position);
        _tilemap.ClearAllTiles();
        for (int y = -halfYCount; y <= halfYCount; y++)
        {
            for (int x = -halfXCount; x <= halfXCount; x++)
            {
                Tile tile = CreateTile(x, y);
                tile.transform = Matrix4x4.Rotate(Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 4) * 90f));
                _tilemap.SetTile(new Vector3Int(camCurrTilePos.x + x, camCurrTilePos.y + y, 0), tile);
            }
        }
    }

    private void Shift(Vector3Int camPrevCell, Vector3Int camCurrCell)
    {
        GetHalfRowsAndColsCount(out int halfYCount, out int halfXCount);
        int deltaY = camCurrCell.y - camPrevCell.y;
        int deltaX = camCurrCell.x - camPrevCell.x;
        Tile[,] viewTiles = new Tile[2 * halfXCount + 1, 2 * halfYCount + 1];
        for (int y = -halfYCount; y <= halfYCount; y++)
        {
            int tileY = y + halfYCount;
            for (int x = -halfXCount; x <= halfXCount; x++)
            {
                int tileX = x + halfXCount;
                viewTiles[tileX, tileY] = _tilemap.GetTile<Tile>(new Vector3Int(camCurrCell.x + x, camCurrCell.y + y, 0));
            }
        }
        if (deltaY != 0)
        {
            int begY = deltaY > 0 ? Math.Max(-halfYCount, halfYCount - deltaY + 1) : -halfYCount;
            int endY = Math.Min(halfYCount, begY + Math.Abs(deltaY) - 1);
            for (int y = begY; y <= endY; y++)
            {
                int tileY = y + halfYCount;
                for (int x = -halfXCount; x <= halfXCount; x++)
                {
                    int tileX = x + halfXCount;
                    Tile tile = viewTiles[tileX, tileY] = CreateTile(x, y);
                    tile.transform = Matrix4x4.Rotate(Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 4) * 90f));
                }
            }
        }
        if (deltaX != 0)
        {
            int begX = deltaX > 0 ? Math.Max(-halfXCount, halfXCount - deltaX + 1) : -halfXCount;
            int endX = Math.Min(halfXCount, begX + Math.Abs(deltaX) - 1);
            for (int y = -halfYCount; y <= halfYCount; y++)
            {
                int tileY = y + halfYCount;
                for (int x = begX; x <= endX; x++)
                {
                    int tileX = x + halfXCount;
                    Tile tile = viewTiles[tileX, tileY] = CreateTile(x, y);
                    tile.transform = Matrix4x4.Rotate(Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 4) * 90f));
                }
            }
        }
        _tilemap.ClearAllTiles();
        for (int y = -halfYCount; y <= halfYCount; y++)
        {
            int tileY = y + halfYCount;
            for (int x = -halfXCount; x <= halfXCount; x++)
            {
                int tileX = x + halfXCount;
                _tilemap.SetTile(new Vector3Int(camCurrCell.x + x, camCurrCell.y + y, 0), viewTiles[tileX, tileY]);
            }
        }
        OnShiftEnd();
    }

    private void LateUpdate()
    {
        Vector3Int camPrevCell = _cameraCell;
        Vector3Int camCurrCell = _tilemap.WorldToCell(_camera.transform.position);
        camCurrCell.z = 0;
        if (camPrevCell != camCurrCell)
        {
            Shift(camPrevCell, camCurrCell);
        }
        _cameraCell = camCurrCell;
    }
}
