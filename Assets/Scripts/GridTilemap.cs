//#define EDITOR_RELOAD_ON_START
//#define DEBUG_ADDED_TILES

using MustHave.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(Tilemap))]
public abstract class GridTilemap<T> : MonoBehaviour where T : Tile
{
    [SerializeField] protected Camera _camera = default;
    [SerializeField] protected T[] _tiles = default;

    protected Grid _grid = default;
    protected Tilemap _tilemap = default;
    protected readonly List<int> _tileIndexPool = new List<int>();
    protected Vector2Int _cameraCell = default;

    public Camera Camera { get => _camera; set => _camera = value; }
    public Tilemap Tilemap { get => _tilemap; set => _tilemap = value; }

    protected abstract T CreateTile(int x, int y);
    protected abstract void SetTilesContent();
    protected virtual void OnLateUpdate() { }
    protected virtual void OnAwake() { }
    protected virtual void OnStart() { }

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        _grid = _tilemap.layoutGrid;
        OnAwake();
    }

    private void Start()
    {
        if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            OnStart();
        }
#if UNITY_EDITOR && EDITOR_RELOAD_ON_START
        else
        {
            ResetTilemap();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }

    public void ResetTilemap()
    {
        Vector3 cameraPosition = new Vector3(0f, 0f, _camera.transform.position.z);
        _camera.transform.position = cameraPosition;
        _cameraCell = WorldToCell(cameraPosition);
        FillMapInView();
        SetTilesContent();
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        //return (Vector2Int)_grid.WorldToCell(worldPos);
        return (Vector2Int)_tilemap.WorldToCell(worldPos);
    }

    public Vector3 GetCellCenterWorld(Vector2Int cell)
    {
        return _tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    public T GetTile(Vector2Int cell)
    {
        return GetTile(cell.x, cell.y);
    }

    public T GetTile(int x, int y)
    {
        return GetTile(new Vector3Int(x, y, 0));
    }

    public T GetTile(Vector3Int cell)
    {
        return _tilemap.GetTile<T>(cell);
    }

    public T GetTile(Vector3 worldPoint)
    {
        return GetTile(worldPoint, out _);
    }

    public T GetTile(Vector3 worldPoint, out Vector2Int cell)
    {
        cell = WorldToCell(worldPoint);
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

    protected void GetHalfViewSizeXY(out int halfXCount, out int halfYCount)
    {
        //Debug.Log(GetType() + ".GetHalfViewSizeXY: " + Screen.width + " " + Screen.height);
        halfYCount = (int)(_camera.orthographicSize / _grid.cellSize.y);
        halfXCount = (halfYCount * Screen.width / Screen.height) + 2;
        halfYCount += 1;
    }

    private void FillMapInView()
    {
        _tilemap.ClearAllTiles();
        UpdateTilesInView((x, y) => {
            Tile tile = CreateTile(x, y);
            _tilemap.SetTile(new Vector3Int(x, y, 0), tile);
        }, true);
    }

    protected void UpdateTiles(int begY, int endY, int begX, int endX, Action<int, int> onUpdate)
    {
        for (int y = begY; y <= endY; y++)
        {
            for (int x = begX; x <= endX; x++)
            {
                onUpdate(x, y);
            }
        }
    }

    protected void UpdateTilesInView(Action<int, int> onUpdate, bool refreshTiles)
    {
        Vector2Int camCurrCell = WorldToCell(_camera.transform.position);
        GetHalfViewSizeXY(out int halfXCount, out int halfYCount);
        UpdateTiles(-halfYCount + camCurrCell.y, halfYCount + camCurrCell.y,
                    -halfXCount + camCurrCell.x, halfXCount + camCurrCell.x, (x, y) => {
                        onUpdate(x, y);
                    });
        if (refreshTiles)
            _tilemap.RefreshAllTiles();
    }

    private void Shift(Vector2Int camPrevCell, Vector2Int camCurrCell)
    {
        GetHalfViewSizeXY(out int halfXCount, out int halfYCount);
        int deltaY = camCurrCell.y - camPrevCell.y;
        int deltaX = camCurrCell.x - camPrevCell.x;
        Vector2Int viewSize = new Vector2Int(2 * halfXCount + 1, 2 * halfYCount + 1);

        //Debug.Log(GetType() + ".Shift: " + deltaX + " " + deltaY);
        if (deltaY != 0)
        {
            int begY, endY, addedRows = 0;
            if (Mathf.Abs(deltaY) > viewSize.y)
            {
                begY = deltaY > 0 ? Mathf.Max(-viewSize.y - halfYCount, -halfYCount - (deltaY - viewSize.y)) : halfYCount + 1;
                endY = begY + Mathf.Min(Mathf.Abs(deltaY) - viewSize.y - 1, viewSize.y - 1);
                UpdateTiles(begY, endY, -halfXCount, halfXCount, (x, y) => {
                    Vector3Int dstXY = new Vector3Int(x + camPrevCell.x, y + camCurrCell.y, 0);
                    Tile tile = CreateTile(x, y);
#if DEBUG_ADDED_TILES
                    tile.color = Color.white;
#endif
                    _tilemap.SetTile(dstXY, tile);
                });
                addedRows = endY - begY + 1;
            }
            begY = deltaY > 0 ? Mathf.Max(-halfYCount, halfYCount - deltaY + 1) : -halfYCount;
            endY = Mathf.Min(halfYCount, begY + Mathf.Abs(deltaY) - 1);
            int srcYOffset = camCurrCell.y - Math.Sign(deltaY) * viewSize.y;
            UpdateTiles(begY, endY, -halfXCount, halfXCount, (x, y) => {
                Vector3Int srcXY = new Vector3Int(x + camPrevCell.x, y + srcYOffset, 0);
                Vector3Int dstXY = new Vector3Int(x + camPrevCell.x, y + camCurrCell.y, 0);
                Tile tile = GetTile(srcXY);
                _tilemap.SetTile(dstXY, tile);
                _tilemap.SetTile(srcXY, null);
            });
            if (addedRows > 0)
            {
                begY = deltaY > 0 ? -halfYCount : halfYCount - addedRows;
                endY = begY + addedRows;
                UpdateTiles(begY, endY, -halfXCount, halfXCount, (x, y) => {
                    Vector3Int dstXY = new Vector3Int(x + camPrevCell.x, y + camPrevCell.y, 0);
                    _tilemap.SetTile(dstXY, null);
                });
            }
        }
        if (deltaX != 0)
        {
            int begX, endX, addedCols = 0;
            if (Mathf.Abs(deltaX) > viewSize.x)
            {
                begX = deltaX > 0 ? Mathf.Max(-viewSize.x - halfXCount, -halfXCount - (deltaX - viewSize.x)) : halfXCount + 1;
                endX = begX + Mathf.Min(Mathf.Abs(deltaX) - viewSize.x - 1, viewSize.x - 1);
                UpdateTiles(-halfYCount, halfYCount, begX, endX, (x, y) => {
                    Vector3Int dstXY = new Vector3Int(x + camCurrCell.x, y + camCurrCell.y, 0);
                    Tile tile = CreateTile(x, y);
#if DEBUG_ADDED_TILES
                    tile.color = Color.white;
#endif
                    _tilemap.SetTile(dstXY, tile);
                });
                addedCols = endX - begX + 1;
            }
            begX = deltaX > 0 ? Mathf.Max(-halfXCount, halfXCount - deltaX + 1) : -halfXCount;
            endX = Mathf.Min(halfXCount, begX + Mathf.Abs(deltaX) - 1);
            int srcXOffset = camCurrCell.x - Math.Sign(deltaX) * viewSize.x;
            UpdateTiles(-halfYCount, halfYCount, begX, endX, (x, y) => {
                Vector3Int srcXY = new Vector3Int(x + srcXOffset, y + camCurrCell.y, 0);
                Vector3Int dstXY = new Vector3Int(x + camCurrCell.x, y + camCurrCell.y, 0);
                Tile tile = GetTile(srcXY);// ?? CreateTile(x, y);
                _tilemap.SetTile(dstXY, tile);
                _tilemap.SetTile(srcXY, null);
            });
            if (addedCols > 0)
            {
                begX = deltaX > 0 ? -halfXCount : halfXCount - addedCols;
                endX = begX + addedCols;
                UpdateTiles(-halfYCount, halfYCount, begX, endX, (x, y) => {
                    Vector3Int dstXY = new Vector3Int(x + camPrevCell.x, y + camCurrCell.y, 0);
                    _tilemap.SetTile(dstXY, null);
                });
            }
        }
        SetTilesContent();
    }

    private void LateUpdate()
    {
        if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            Vector2Int camPrevCell = _cameraCell;
            Vector2Int camCurrCell = WorldToCell(_camera.transform.position);
#if DEBUG_ADDED_TILES
            //camCurrCell.x = camPrevCell.x;
            //camCurrCell.y = camPrevCell.y;
            //Vector2Int camDelta = camCurrCell - camPrevCell;
            //if (Mathf.Abs(camDelta.x) < 1 || Mathf.Abs(camDelta.y) < 1)
            //{
            //    camCurrCell = camPrevCell;
            //}
#endif
            if (camPrevCell != camCurrCell)
            {
                Shift(camPrevCell, camCurrCell);
            }
            _cameraCell = camCurrCell;

            OnLateUpdate();
        }
    }
}
