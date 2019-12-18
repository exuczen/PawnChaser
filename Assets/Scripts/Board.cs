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
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _playerPawnsContainer = default;
    [SerializeField] private Transform _enemyPawnsContainer = default;

    [Header("BOARD LEVEL")]
    [SerializeField] private int _levelIndex = default;
    [SerializeField] private int _playerMovesInTurn = default;

    private BoardPathfinder _pathfinder = default;
    private EnemyHandler _enemyHandler = default;
    private bool _pawnsPositionsSaved = default;
    private List<EnemyPawn> _enemyPawns = new List<EnemyPawn>();
    private List<PlayerPawn> _playerPawns = new List<PlayerPawn>();
    private BoardScreen _boardScreen = default;
    private int _playerMovesLeft = default;

    public BoardTilemap Tilemap { get => _tilemap; }
    public int LevelIndex { get => _levelIndex; set => _levelIndex = value; }
    public int PlayerMovesInTurn { get => _playerMovesInTurn; }
    public BoardScreen BoardScreen { get => _boardScreen; set => _boardScreen = value; }

    private void Awake()
    {
        _pathfinder = GetComponent<BoardPathfinder>();
        _enemyHandler = GetComponent<EnemyHandler>();
    }

    private void Start()
    {
#if DUBUG_LEVEL
        OnLevelLoaded(null);
#else
        LoadLevelFromJson(_levelIndex);
#endif
    }

    public IEnumerator MovePlayerPawnRoutine(PlayerPawn pawn, Vector2Int destCell, Action onEnd = null)
    {
        yield return pawn.MoveRoutine(_tilemap, destCell);
        _playerMovesLeft = Mathf.Max(_playerMovesLeft - 1, 0);
        if (_playerMovesLeft > 0)
        {
            SavePawnsPositions();
            onEnd?.Invoke();
        }
        else
        {
            MoveEnemyPawns(enemiesMoved => {
                _playerMovesLeft = enemiesMoved ? _playerMovesInTurn : 1;
                SavePawnsPositions();
                onEnd?.Invoke();
            });
        }
    }

    public void MoveEnemyPawns(Action<bool> onEnd)
    {
        StartCoroutine(_enemyHandler.MoveEnemyPawnsRoutine(onEnd, _boardScreen.ShowSuccessPopup, _boardScreen.ShowFailPopup));
    }

    public void SkipPlayerMove()
    {
        EventSystem currentEventSystem = EventSystem.current;
        currentEventSystem.enabled = false;
        MoveEnemyPawns(enemiesMoved => {
            _playerMovesLeft = _playerMovesInTurn;
            if (enemiesMoved)
                SavePawnsPositions();
            currentEventSystem.enabled = true;
        });
    }

    public void SavePawnsPositions()
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
        int movedPlayerPawnsCount = 0;
        foreach (PlayerPawn pawn in _playerPawns)
        {
            movedPlayerPawnsCount += pawn.SetPreviousCellPosition(_tilemap) ? 1 : 0;
        }
        foreach (EnemyPawn pawn in _enemyPawns)
        {
            pawn.SetPreviousCellPosition(_tilemap);
        }
        //Debug.Log(GetType() + ".SetPawnsPreviousPositions:" + _playerMovesLeft);
        if (movedPlayerPawnsCount > 0)
        {
            if (_playerMovesLeft < _playerMovesInTurn)
                _playerMovesLeft += movedPlayerPawnsCount;
            else
                _playerMovesLeft = movedPlayerPawnsCount;
            _playerMovesLeft = Mathf.Min(_playerMovesLeft, _playerMovesInTurn);
        }
        else
        {
            _playerMovesLeft = _playerMovesInTurn;
        }
        //Debug.Log(GetType() + ".SetPawnsPreviousPositions:" + _playerMovesLeft);
    }

    public void SaveLevelToJson()
    {
        BoardLevel boardLevel = new BoardLevel(this, _tilemap);
        boardLevel.SaveToJson(_levelIndex);
    }

    public void LoadLevelFromJson(int levelIndex)
    {
        //BoardLevel boardLevel = BoardLevel.LoadFromJson(levelIndex);
        BoardLevel boardLevel = _tilemap.LoadLevelFromJson(levelIndex);
        if (boardLevel != null)
        {
            OnLevelLoaded(boardLevel);
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
        _enemyHandler.AddPawnToLists(_enemyPawns, true);

        //foreach (var enemyPawn in _enemyPawns)
        //{
        //    Debug.Log(GetType() + ".AddEnemyPawnsToList: " + enemyPawn.Target);
        //}
    }

    private void OnLevelLoaded(BoardLevel boardLevel)
    {
        if (boardLevel != null)
        {
            _playerMovesInTurn = boardLevel.PlayerMovesInTurn;
        }
        else if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            _tilemap.ResetTilemap();
            _tilemap.SetTilesContent();
        }
        if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            _playerMovesLeft = _playerMovesInTurn;
            _pathfinder.ClearSprites();
            AddPawnsToLists();
            SavePawnsPositions();
        }
#if UNITY_EDITOR
        else
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }

    public void LoadNextLevel()
    {
        LoadLevelFromJson(_levelIndex = (_levelIndex + 1) % 4);
    }

    public void ResetLevel()
    {
        LoadLevelFromJson(_levelIndex);
    }
}
