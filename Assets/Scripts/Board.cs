using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    [SerializeField, HideInInspector] private int _levelIndex = default;

    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _pawnsContainer = default;
    [SerializeField] private Transform _targetsContainer = default;
    [SerializeField] private PlayerPawn _playerPawnPrefab = default;
    [SerializeField] private EnemyPawn _enemyPawnPrefab = default;
    [SerializeField] private EnemyPawnTarget _enemyPawnTargetPrefab = default;

    private EnemyPawnTarget _enemyTarget = default;
    private EnemyPawn _enemyPawn = default;
    private BoardPathfinder _pathfinder = default;
    private bool _pawnsPositionsSaved = default;

    public Transform PawnsContainer { get => _pawnsContainer; }
    public Transform TargetsContainer { get => _targetsContainer; }
    public BoardTilemap Tilemap { get => _tilemap; }
    public int LevelIndex { get => _levelIndex; set => _levelIndex = value; }

    private void Start()
    {
        _enemyPawn = _pawnsContainer.GetComponentInChildren<EnemyPawn>();
        _enemyTarget = _targetsContainer.GetComponentInChildren<EnemyPawnTarget>();
        _pathfinder = GetComponent<BoardPathfinder>();

        SavePawnsPositions();
    }

    public void MoveEnemyPawn(Action onEnd)
    {
        void onEnd2()
        {
            SavePawnsPositions();
            onEnd?.Invoke();
        }
        _pathfinder.FindPath(_enemyPawn, _enemyTarget, path => {
            if (path.Count > 0)
            {
                Vector2Int destCell = path.PickLastElement();
                StartCoroutine(MovePawnRoutine(_enemyPawn, destCell, onEnd2));
            }
            else
            {
                onEnd2();
            }
        });
    }

    public IEnumerator MovePawnRoutine(Pawn pawn, Vector2Int destCell, Action onEnd = null)
    {
        yield return pawn.MoveRoutine(_tilemap, destCell, onEnd);
    }

    private void SavePawnsPositions()
    {
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            pawnTransform.GetComponent<Pawn>().AddCellPositionToStack(_tilemap);
        }
        _pawnsPositionsSaved = true;
    }

    public void SetPawnsPreviousPositions()
    {
        if (_pawnsPositionsSaved)
        {
            _pawnsPositionsSaved = false;
            _pathfinder.ClearSprites();
        }
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            pawnTransform.GetComponent<Pawn>().SetPreviousCellPosition(_tilemap);
        }
    }

    public void SaveBoardLevelToJson()
    {
        BoardLevel boardLevel = new BoardLevel(this);
        boardLevel.SaveToJson(_levelIndex);
    }

    public void LoadBoardLevelFromJson(int levelIndex)
    {
        BoardLevel boardLevel = BoardLevel.LoadFromJson(levelIndex);
        if (boardLevel != null)
        {
            if (EditorApplicationUtils.ApplicationIsPlaying)
            {
                _pawnsContainer.DestroyAllChildren();
                _targetsContainer.DestroyAllChildren();
            }
            else
            {
                _pawnsContainer.DestroyAllChildrenImmediate();
                _targetsContainer.DestroyAllChildrenImmediate();
            }

            foreach (Vector2Int cellXY in boardLevel.PlayerPawnsXY)
            {
                _playerPawnPrefab.CreateInstance<PlayerPawn>(cellXY, _tilemap, _pawnsContainer);
            }
            foreach (Vector2Int cellXY in boardLevel.EnemyPawnsXY)
            {
                _enemyPawn = _enemyPawnPrefab.CreateInstance<EnemyPawn>(cellXY, _tilemap, _pawnsContainer);
            }
            foreach (Vector2Int cellXY in boardLevel.EnemyTargetsXY)
            {
                _enemyTarget = _enemyPawnTargetPrefab.CreateInstance<EnemyPawnTarget>(cellXY, _tilemap, _targetsContainer);
            }
        }
        _tilemap.ResetTilemap();

        SavePawnsPositions();
    }

    public void ResetBoardLevel()
    {
        _pathfinder.ClearSprites();
        LoadBoardLevelFromJson(_levelIndex);
    }
}
