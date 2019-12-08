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

    public Transform PawnsContainer { get => _pawnsContainer; }
    public Transform TargetsContainer { get => _targetsContainer; }
    public BoardTilemap Tilemap { get => _tilemap; }
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
    }

    public void ResetBoardLevel()
    {
        _pathfinder.ClearSprites();
        LoadBoardLevelFromJson(_levelIndex);
    }
}
