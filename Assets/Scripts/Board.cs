﻿using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    [SerializeField, HideInInspector] private string _levelFileNamePrefix = default;
    [SerializeField, HideInInspector] private int _levelIndex = default;

    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _pawnsContainer = default;
    [SerializeField] private Transform _targetsContainer = default;

    private EnemyPawnTarget _enemyTarget = default;
    private EnemyPawn _enemyPawn = default;
    private BoardPathfinder _pathfinder = default;

    public Transform PawnsContainer { get => _pawnsContainer; }
    public Transform TargetsContainer { get => _targetsContainer; }
    public BoardTilemap Tilemap { get => _tilemap; set => _tilemap = value; }
    public string LevelFileNamePrefix { get => _levelFileNamePrefix; set => _levelFileNamePrefix = value; }
    public int LevelIndex { get => _levelIndex; set => _levelIndex = value; }

    private void Start()
    {
        _enemyPawn = _pawnsContainer.GetComponentInChildren<EnemyPawn>();
        _enemyTarget = _targetsContainer.GetComponentInChildren<EnemyPawnTarget>();
        _pathfinder = GetComponent<BoardPathfinder>();
    }

    public void MoveEnemyPawn(Action onEnd)
    {
        _pathfinder.FindPath(_enemyPawn, _enemyTarget, path => {
            if (path.Count > 0)
            {
                Vector2Int destCell = path.PickLastElement();
                StartCoroutine(MovePawnRoutine(_enemyPawn.transform, destCell, onEnd));
            }
            else
            {
                onEnd?.Invoke();
            }
        });
    }

    public IEnumerator MovePawnRoutine(Transform pawnTransform, Vector2Int destCell, Action onEnd = null)
    {
        Vector2Int pawnCell = _tilemap.WorldToCell(pawnTransform.position);
        Vector3 begPos = pawnTransform.position;
        Vector3 endPos = _tilemap.GetCellCenterWorld(destCell);

        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            pawnTransform.position = Vector3.Lerp(begPos, endPos, shift);
        });
        pawnTransform.position = endPos;
        BoardTile tile;
        if (tile = _tilemap.GetTile(pawnCell))
            tile.Content = null;
        if (tile = _tilemap.GetTile(destCell))
            tile.Content = pawnTransform;

        onEnd?.Invoke();
    }

    public void SaveBoardLevelToJson()
    {
        BoardLevel boardLevel = new BoardLevel(this);
        boardLevel.SaveToJson(_levelFileNamePrefix + _levelIndex + ".dat");
    }
}
