#define DUBUG_LEVEL

using MustHave.UI;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    [SerializeField, HideInInspector] private int _levelIndex = default;

    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _playerPawnsContainer = default;
    [SerializeField] private Transform _playerTargetsContainer = default;
    [SerializeField] private Transform _enemyPawnsContainer = default;
    [SerializeField] private Transform _enemyTargetsContainer = default;
    [SerializeField] private PlayerPawn _playerPawnPrefab = default;
    [SerializeField] private EnemyPawn _enemyPawnPrefab = default;
    [SerializeField] private EnemyPawnTarget _enemyPawnTargetPrefab = default;

    private EnemyPawnTarget _enemyTarget = default;
    private EnemyPawn _enemyPawn = default;
    private BoardPathfinder _pathfinder = default;
    private bool _pawnsPositionsSaved = default;
    private BoardScreen _boardScreen = default;

    public BoardTilemap Tilemap { get => _tilemap; }
    public int LevelIndex { get => _levelIndex; set => _levelIndex = value; }
    public BoardScreen BoardScreen { get => _boardScreen; set => _boardScreen = value; }
    public Transform PlayerPawnsContainer { get => _playerPawnsContainer; }
    public Transform PlayerTargetsContainer { get => _playerTargetsContainer; }
    public Transform EnemyPawnsContainer { get => _enemyPawnsContainer; }
    public Transform EnemyTargetsContainer { get => _enemyTargetsContainer; }

    private void Start()
    {
        _enemyPawn = _enemyPawnsContainer.GetComponentInChildren<EnemyPawn>();
        _enemyTarget = _enemyTargetsContainer.GetComponentInChildren<EnemyPawnTarget>();
        _pathfinder = GetComponent<BoardPathfinder>();
#if DUBUG_LEVEL
        _pathfinder.ClearSprites();
        _tilemap.ResetTilemap();
        SavePawnsPositions();
#else
        LoadBoardLevelFromJson(_levelIndex);
#endif
    }

    private IEnumerator MoveEnemyPawnRoutine(List<Vector2Int> path, Action onEnd)
    {
        if (path.Count > 0)
        {
            Vector2Int destCell = path.PickLastElement();
            yield return MovePawnRoutine(_enemyPawn, destCell);
        }
        SavePawnsPositions();
        if (path.Count <= 1)
        {
            yield return new WaitForSeconds(0.5f);
            _boardScreen.ShowFailPopup();
        }
        onEnd?.Invoke();
    }

    public void MoveEnemyPawn(Action onEnd)
    {
        bool pathFound = _pathfinder.FindPath(_enemyPawn, _enemyTarget, out List<Vector2Int> path);
        if (pathFound)
            StartCoroutine(MoveEnemyPawnRoutine(path, onEnd));
        else
        {
            SavePawnsPositions();
            bool targetSurrounded = !_pathfinder.FindPathToBoundsMin(_enemyTarget, out path);
            bool enemySurrounded = targetSurrounded ? !_pathfinder.FindPathToBoundsMin(_enemyPawn, out path) : true;
            if (enemySurrounded)
            {
                this.StartCoroutineActionAfterTime(() => {
                    _boardScreen.ShowSuccessPopup();
                    onEnd?.Invoke();
                }, 1f);
            }
            else
            {
                onEnd?.Invoke();
            }
        }
    }

    public IEnumerator MovePawnRoutine(Pawn pawn, Vector2Int destCell, Action onEnd = null)
    {
        yield return pawn.MoveRoutine(_tilemap, destCell, onEnd);
    }

    public void SkipPlayerMove()
    {
        EventSystem currentEventSystem = EventSystem.current;
        currentEventSystem.enabled = false;
        MoveEnemyPawn(() => {
            currentEventSystem.enabled = true;
        });
    }

    private void SavePawnsPositions()
    {
        foreach (Transform pawnTransform in _playerPawnsContainer)
        {
            pawnTransform.GetComponent<Pawn>().AddCellPositionToStack(_tilemap);
        }
        foreach (Transform pawnTransform in _enemyPawnsContainer)
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
        foreach (Transform pawnTransform in _playerPawnsContainer)
        {
            pawnTransform.GetComponent<Pawn>().SetPreviousCellPosition(_tilemap);
        }
        foreach (Transform pawnTransform in _enemyPawnsContainer)
        {
            pawnTransform.GetComponent<Pawn>().SetPreviousCellPosition(_tilemap);
        }
    }

    public void SaveLevelToJson()
    {
        BoardLevel boardLevel = new BoardLevel(this);
        boardLevel.SaveToJson(_levelIndex);
    }

    public void LoadLevelFromJson(int levelIndex)
    {
        BoardLevel boardLevel = BoardLevel.LoadFromJson(levelIndex);
        if (boardLevel != null)
        {
            if (EditorApplicationUtils.ApplicationIsPlaying)
            {
                _playerPawnsContainer.DestroyAllChildren();
                _playerTargetsContainer.DestroyAllChildren();
                _enemyPawnsContainer.DestroyAllChildren();
                _enemyTargetsContainer.DestroyAllChildren();
            }
            else
            {
                _playerPawnsContainer.DestroyAllChildrenImmediate();
                _playerTargetsContainer.DestroyAllChildrenImmediate();
                _enemyPawnsContainer.DestroyAllChildrenImmediate();
                _enemyTargetsContainer.DestroyAllChildrenImmediate();
            }

            foreach (Vector2Int cellXY in boardLevel.PlayerPawnsXY)
            {
                _playerPawnPrefab.CreateInstance<PlayerPawn>(cellXY, _tilemap, _playerPawnsContainer);
            }
            foreach (Vector2Int cellXY in boardLevel.EnemyPawnsXY)
            {
                _enemyPawn = _enemyPawnPrefab.CreateInstance<EnemyPawn>(cellXY, _tilemap, _enemyPawnsContainer);
            }
            foreach (Vector2Int cellXY in boardLevel.EnemyTargetsXY)
            {
                _enemyTarget = _enemyPawnTargetPrefab.CreateInstance<EnemyPawnTarget>(cellXY, _tilemap, _enemyTargetsContainer);
            }
        }
        if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            _pathfinder.ClearSprites();
            _tilemap.ResetTilemap();
            SavePawnsPositions();
        }
#if UNITY_EDITOR
        else
        {
            //Undo.RecordObject(gameObject, "LoadLevelFromJson");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }

    public void LoadNextLevel()
    {
        LoadLevelFromJson(_levelIndex = (_levelIndex + 1) % 2);
    }

    public void ResetLevel()
    {
        LoadLevelFromJson(_levelIndex);
    }
}
