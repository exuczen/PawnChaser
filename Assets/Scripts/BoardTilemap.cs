﻿using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardTilemap : GridTilemap<BoardTile>
{
    private Transform _playerPawnsContainer = default;
    private Transform _enemyPawnsContainer = default;
    private Transform _enemyTargetsContainer = default;

    protected override void OnAwake()
    {
        Board board = GetComponentInParent<Board>();
        _playerPawnsContainer = board.PlayerPawnsContainer;
        _enemyPawnsContainer = board.EnemyPawnsContainer;
        _enemyTargetsContainer = board.EnemyTargetsContainer;
    }

    protected override void OnStart()
    {
    }

    protected override void SetTilesContent()
    {
        SetTilesContent(_playerPawnsContainer);
        SetTilesContent(_enemyPawnsContainer);
        SetTilesContent(_enemyTargetsContainer);
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
                    boardTile.Content = child;
            }
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

    public Bounds2Int GetTilesContentCellBounds(Vector2Int initXY, bool playerPawns, bool enemyPawns)
    {
        Vector2Int min = initXY;
        Vector2Int max = initXY;
        Bounds2Int bounds = new Bounds2Int(initXY, Vector2Int.one);
        if (playerPawns)
            bounds = GetChildrenCellBounds(bounds.Min, bounds.Max, _playerPawnsContainer);
        if (enemyPawns)
            bounds = GetChildrenCellBounds(bounds.Min, bounds.Max, _enemyPawnsContainer);
        bounds = GetChildrenCellBounds(bounds.Min, bounds.Max, _enemyTargetsContainer);
        bounds.Min -= Vector2Int.one;
        bounds.Max += Vector2Int.one;
        return bounds;
    }

    private IEnumerator UpdateViewTilesColorsRoutine()
    {
        while (true)
        {
            GetHalfViewSizeXY(out int halfXCount, out int halfYCount);
            int viewTilesCount = (2 * halfXCount + 1) * (2 * halfYCount + 1);
            Color[] currColors = new Color[viewTilesCount];
            Color[] nextColors = new Color[viewTilesCount];
            int colorIndex = 0;
            UpdateTilesInView((x, y) => {
                Vector3Int cell = new Vector3Int(x, y, 0);
                BoardTile tile = GetTile(cell);
                currColors[colorIndex] = tile.color;
                nextColors[colorIndex] = Color.Lerp(Color.HSVToRGB(0f, 0f, 0.7f), Color.HSVToRGB(0f, 0f, 0.8f), UnityEngine.Random.Range(0f, 1f));
                colorIndex++;
            }, false);
            yield return CoroutineUtils.UpdateRoutine(1f, (transition, elapsedTime) => {
                colorIndex = 0;
                UpdateTilesInView((x, y) => {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    BoardTile tile = GetTile(cell);
                    tile.color = Color.Lerp(currColors[colorIndex], nextColors[colorIndex], transition);
                    colorIndex++;
                }, true);
            });
            colorIndex = 0;
            UpdateTilesInView((x, y) => {
                Vector3Int cell = new Vector3Int(x, y, 0);
                BoardTile tile = GetTile(cell);
                tile.color = nextColors[colorIndex++];
            }, true);
        }
    }
}
