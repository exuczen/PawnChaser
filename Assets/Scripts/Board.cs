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

    private BoardPathfinder _pathfinder = default;
    private bool _pawnsPositionsSaved = default;
    private List<EnemyPawn> _enemyPawns = new List<EnemyPawn>();
    private List<PlayerPawn> _playerPawns = new List<PlayerPawn>();
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
        _pathfinder = GetComponent<BoardPathfinder>();
#if DUBUG_LEVEL
        OnDebugLevelLoaded();
#else
        LoadLevelFromJson(_levelIndex);
#endif
    }

    public IEnumerator MovePawnRoutine(Pawn pawn, Vector2Int destCell, Action onEnd = null)
    {
        yield return pawn.MoveRoutine(_tilemap, destCell, onEnd);
    }

    public IEnumerator MoveEnemyPawnsRoutine(Action onEnd, bool savePawnsPositionsOnHold)
    {
        int surroundedEnemiesCount = 0;
        int movedEnemiesCount = 0;
        for (int i = 0; i < _enemyPawns.Count; i++)
        {
            EnemyPawn enemyPawn = _enemyPawns[i];
            bool pathFound = _pathfinder.FindPath(enemyPawn, enemyPawn.Target, out List<Vector2Int> path,
                _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
            if (pathFound)
            {
                if (path.Count > 0)
                {
                    Vector2Int destCell = path.PickLastElement();
                    yield return MovePawnRoutine(enemyPawn, destCell);
                    movedEnemiesCount++;
                    if (path.Count <= 1)
                    {
                        yield return new WaitForSeconds(0.5f);
                        _boardScreen.ShowFailPopup();
                        onEnd?.Invoke();
                        yield break;
                    }
                }
            }
            else
            {
                bool targetSurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawn.Target, out path, _playerPawnsContainer, _enemyTargetsContainer);
                bool enemySurrounded = targetSurrounded ? !_pathfinder.FindPathToBoundsMin(enemyPawn, out path, _playerPawnsContainer) : true;
                surroundedEnemiesCount += enemySurrounded ? 1 : 0;
            }
        }
        if (movedEnemiesCount > 0 || savePawnsPositionsOnHold)
        {
            SavePawnsPositions();
        }
        if (surroundedEnemiesCount == _enemyPawns.Count)
        {
            yield return new WaitForSeconds(0.5f);
            _boardScreen.ShowSuccessPopup();
        }
        onEnd?.Invoke();
    }

    public void MoveEnemyPawns(Action onEnd, bool savePawnsPositionsOnHold)
    {
        StartCoroutine(MoveEnemyPawnsRoutine(onEnd, savePawnsPositionsOnHold));
    }

    public void SkipPlayerMove()
    {
        EventSystem currentEventSystem = EventSystem.current;
        currentEventSystem.enabled = false;
        MoveEnemyPawns(() => {
            currentEventSystem.enabled = true;
        }, false);
    }

    private void SavePawnsPositions()
    {
        foreach (PlayerPawn pawn in _playerPawns)
        {
            pawn.AddCellPositionToStack(_tilemap);
        }
        foreach (EnemyPawn pawn in _enemyPawns)
        {
            pawn.AddCellPositionToStack(_tilemap);
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
        foreach (PlayerPawn pawn in _playerPawns)
        {
            pawn.SetPreviousCellPosition(_tilemap);
        }
        foreach (EnemyPawn pawn in _enemyPawns)
        {
            pawn.SetPreviousCellPosition(_tilemap);
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
            if (EditorApplicationUtils.ApplicationIsPlaying)
            {
                _pathfinder.ClearSprites();
                _tilemap.ResetTilemap();
            }
            foreach (Vector2Int cellXY in boardLevel.PlayerPawnsXY)
            {
                _playerPawnPrefab.CreateInstance<PlayerPawn>(cellXY, _tilemap, _playerPawnsContainer);
            }
            foreach (Vector2Int cellXY in boardLevel.EnemyTargetsXY)
            {
                _enemyPawnTargetPrefab.CreateInstance<EnemyPawnTarget>(cellXY, _tilemap, _enemyTargetsContainer);
            }
            EnemyPawnData[] enemyPawnsData = boardLevel.EnemyPawnsData;
            for (int i = 0; i < enemyPawnsData.Length; i++)
            {
                _enemyPawnPrefab.CreateInstance<EnemyPawn>(enemyPawnsData[i].cell, _tilemap, _enemyPawnsContainer);
            }
            _tilemap.SetTilesContent();
            for (int i = 0; i < enemyPawnsData.Length; i++)
            {
                EnemyPawn pawn = _enemyPawnsContainer.GetChild(i).GetComponent<EnemyPawn>();
                pawn.SetTarget(enemyPawnsData[i].targetCell, _tilemap);
            }
            if (EditorApplicationUtils.ApplicationIsPlaying)
            {
                AddPawnsToLists();
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
    }

    private void AddPawnsToLists()
    {
        _playerPawns.Clear();
        foreach (Transform pawn in _playerPawnsContainer)
        {
            _playerPawns.Add(pawn.GetComponent<PlayerPawn>());
        }
        _enemyPawns.Clear();
        foreach (Transform pawn in _enemyPawnsContainer)
        {
            _enemyPawns.Add(pawn.GetComponent<EnemyPawn>());
        }
        _enemyPawns.Sort((pawnA, pawnB) => {
            bool pawnATargetIsOtherPawn = pawnA.TargetIsOtherPawn;
            bool pawnBTargetIsOtherPawn = pawnB.TargetIsOtherPawn;
            if (pawnATargetIsOtherPawn == pawnBTargetIsOtherPawn)
                return 0;
            else if (pawnATargetIsOtherPawn && !pawnBTargetIsOtherPawn)
                return 1;
            else if (pawnBTargetIsOtherPawn && !pawnATargetIsOtherPawn)
                return -1;
            else
                return 0;
        });
        //foreach (var enemyPawn in _enemyPawns)
        //{
        //    Debug.Log(GetType() + ".AddEnemyPawnsToList: " + enemyPawn.Target);
        //}
    }

    private void OnDebugLevelLoaded()
    {
        _pathfinder.ClearSprites();
        _tilemap.ResetTilemap();
        _tilemap.SetTilesContent();
        AddPawnsToLists();
        SavePawnsPositions();
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
