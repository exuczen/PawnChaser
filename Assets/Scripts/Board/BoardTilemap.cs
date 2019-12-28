using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardTilemap : GridTilemap<BoardTile>
{
    [SerializeField] private Transform _playerPawnsContainer = default;
    [SerializeField] private Transform _playerTargetsContainer = default;
    [SerializeField] private Transform _enemyPawnsContainer = default;
    [SerializeField] private Transform _enemyTargetsContainer = default;
    [SerializeField] private PlayerPawn _playerPawnPrefab = default;
    [SerializeField] private EnemyPawn _enemyPawnPrefab = default;
    [SerializeField] private EnemyPawnTarget _enemyPawnTargetPrefab = default;

    public Transform PlayerPawnsContainer { get => _playerPawnsContainer; }
    public Transform PlayerTargetsContainer { get => _playerTargetsContainer; }
    public Transform EnemyPawnsContainer { get => _enemyPawnsContainer; }
    public Transform EnemyTargetsContainer { get => _enemyTargetsContainer; }

    protected override void ResetCamera()
    {
        Bounds2 contentBounds = GetContentBounds();
        _camera.transform.position = new Vector3(contentBounds.Center.x, 0f, _camera.transform.position.z);
        _cameraCell = GetCameraCell();
    }

    protected override Vector2Int GetCameraCell()
    {
        return WorldToCell(_camera.transform.position);
    }

    protected override void GetHalfViewSizeXY(out int halfXCount, out int halfYCount)
    {
        //Debug.Log(GetType() + ".GetHalfViewSizeXY: " + Screen.width + " " + Screen.height);
        halfYCount = (int)(_camera.orthographicSize / _grid.cellSize.y);
        halfXCount = (halfYCount * Screen.width / Screen.height) + 2;
        halfYCount += 1;
    }

    public override void SetTilesContent()
    {
        foreach (Transform container in transform)
        {
            SetTilesContent(container);
        }
    }

    protected override BoardTile CreateTile(int x, int y)
    {
        BoardTile tile = Instantiate(_tiles[0]);
        tile.color = Color.Lerp(Color.HSVToRGB(0f, 0f, 0.7f), Color.HSVToRGB(0f, 0f, 0.8f), UnityEngine.Random.Range(0f, 1f));
        //tile.transform = Matrix4x4.Rotate(Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 4) * 90f));
        return tile;
    }

    private void SetTilesContent(Transform contentContainer)
    {
        if (contentContainer)
        {
            foreach (Transform child in contentContainer)
            {
                BoardTile boardTile = GetTile(child.position);
                if (boardTile)
                    boardTile.Content = child.GetComponent<TileContent>();
            }
        }
    }

    private Bounds2 GetContentBounds()
    {
        Bounds2 bounds = new Bounds2();
        List<Transform> containers = new List<Transform>();
        foreach (Transform container in transform)
        {
            containers.Add(container);
        }
        Transform contentContainer = containers.Find(c => c.childCount > 0);
        if (contentContainer)
        {
            Transform containerChild = contentContainer.GetChild(0);
            Vector2 min = containerChild.position;
            Vector2 max = containerChild.position;
            bounds.SetMinMax(min, max);
            foreach (Transform container in transform)
            {
                bounds = GetChildrenBounds(bounds.Min, bounds.Max, container);
            }
        }
        return bounds;
    }

    private Bounds2 GetChildrenBounds(Vector2 min, Vector2 max, Transform parent)
    {
        foreach (Transform child in parent)
        {
            min = Maths.Min(child.position, min);
            max = Maths.Max(child.position, max);
        }
        Bounds2 bounds = new Bounds2();
        bounds.SetMinMax(min, max);
        return bounds;
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

    public Bounds2Int GetTilesContentCellBounds(Vector2Int initXY, params Transform[] contentContainers)
    {
        return GetTilesContentCellBounds(initXY, initXY, contentContainers);
    }

    public Bounds2Int GetTilesContentCellBounds(Vector2Int initXY, Vector2Int destXY, params Transform[] contentContainers)
    {
        Vector2Int min = Maths.Min(initXY, destXY);
        Vector2Int max = Maths.Max(initXY, destXY);
        Bounds2Int bounds = new Bounds2Int(initXY, Vector2Int.one);
        foreach (Transform container in contentContainers)
        {
            bounds = GetChildrenCellBounds(bounds.Min, bounds.Max, container);
        }
        bounds.Min -= Vector2Int.one;
        bounds.Max += Vector2Int.one;
        return bounds;
    }

    public BoardLevel LoadLevelFromJson(int levelIndex)
    {
        BoardLevel boardLevel = BoardLevel.LoadFromJson(levelIndex);
        if (boardLevel != null)
        {
            if (EditorApplicationUtils.ApplicationIsPlaying)
            {
                foreach (Transform container in _tilemap.transform)
                {
                    container.DestroyAllChildren();
                }
            }
            else
            {
                foreach (Transform container in _tilemap.transform)
                {
                    container.DestroyAllChildrenImmediate();
                }
            }
            foreach (Vector2Int cellXY in boardLevel.PlayerPawnsXY)
            {
                _playerPawnPrefab.CreateInstance<PlayerPawn>(cellXY, this, _playerPawnsContainer);
            }
            foreach (Vector2Int cellXY in boardLevel.EnemyTargetsXY)
            {
                _enemyPawnTargetPrefab.CreateInstance<EnemyPawnTarget>(cellXY, this, _enemyTargetsContainer);
            }
            EnemyPawnData[] enemyPawnsData = boardLevel.EnemyPawnsData;
            for (int i = 0; i < enemyPawnsData.Length; i++)
            {
                _enemyPawnPrefab.CreateInstance<EnemyPawn>(enemyPawnsData[i].cell, this, _enemyPawnsContainer);
            }
            ResetCamera();
            if (EditorApplicationUtils.ApplicationIsPlaying)
            {
                ResetTilemap();
            }
            SetTilesContent();
            for (int i = 0; i < enemyPawnsData.Length; i++)
            {
                EnemyPawn pawn = _enemyPawnsContainer.GetChild(i).GetComponent<EnemyPawn>();
                pawn.SetTarget(enemyPawnsData[i].targetCell, this);
            }
        }
        return boardLevel;
    }

    //private IEnumerator UpdateViewTilesColorsRoutine()
    //{
    //    while (true)
    //    {
    //        GetHalfViewSizeXY(out int halfXCount, out int halfYCount);
    //        int viewTilesCount = (2 * halfXCount + 1) * (2 * halfYCount + 1);
    //        Color[] currColors = new Color[viewTilesCount];
    //        Color[] nextColors = new Color[viewTilesCount];
    //        int colorIndex = 0;
    //        UpdateTilesInView((x, y) => {
    //            Vector3Int cell = new Vector3Int(x, y, 0);
    //            BoardTile tile = GetTile(cell);
    //            currColors[colorIndex] = tile.color;
    //            nextColors[colorIndex] = Color.Lerp(Color.HSVToRGB(0f, 0f, 0.7f), Color.HSVToRGB(0f, 0f, 0.8f), UnityEngine.Random.Range(0f, 1f));
    //            colorIndex++;
    //        }, false);
    //        yield return CoroutineUtils.UpdateRoutine(1f, (transition, elapsedTime) => {
    //            colorIndex = 0;
    //            UpdateTilesInView((x, y) => {
    //                Vector3Int cell = new Vector3Int(x, y, 0);
    //                BoardTile tile = GetTile(cell);
    //                tile.color = Color.Lerp(currColors[colorIndex], nextColors[colorIndex], transition);
    //                colorIndex++;
    //            }, true);
    //        });
    //        colorIndex = 0;
    //        UpdateTilesInView((x, y) => {
    //            Vector3Int cell = new Vector3Int(x, y, 0);
    //            BoardTile tile = GetTile(cell);
    //            tile.color = nextColors[colorIndex++];
    //        }, true);
    //    }
    //}
}
