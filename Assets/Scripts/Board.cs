using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Transform _pawnsContainer = default;
    [SerializeField] private Transform _targetsContainer = default;

    private EnemyPawnTarget _enemyTarget = default;
    private EnemyPawn _enemyPawn = default;
    private BoardPathfinder _pathfinder = default;

    public Transform PawnsContainer { get => _pawnsContainer; }
    public Transform TargetsContainer { get => _targetsContainer; }
    public BoardTilemap Tilemap { get => _tilemap; set => _tilemap = value; }

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

        _tilemap.GetTile(pawnCell).Content = null;
        _tilemap.GetTile(destCell).Content = pawnTransform;

        onEnd?.Invoke();
    }
}
