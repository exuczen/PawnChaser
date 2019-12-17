using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHandler : MonoBehaviour
{
    [SerializeField] private Transform _playerPawnsContainer = default;
    [SerializeField] private Transform _playerTargetsContainer = default;
    [SerializeField] private Transform _enemyPawnsContainer = default;
    [SerializeField] private Transform _enemyTargetsContainer = default;

    private BoardPathfinder _pathfinder = default;
    //private Board _board = default;
    private List<EnemyPawn> _enemyPawns = new List<EnemyPawn>();
    private List<EnemyPawn> _enemySingles = new List<EnemyPawn>();
    private List<EnemyPawnsPair> _enemyPairs = new List<EnemyPawnsPair>();

    private void Awake()
    {
        //_board = GetComponent<Board>();
        _pathfinder = GetComponent<BoardPathfinder>();
    }

    public IEnumerator MoveEnemyPawnsRoutine(Board board, Action<bool> onEnd)
    {
        int surroundedEnemiesCount = 0;
        int movedEnemiesCount = 0;

        for (int i = 0; i < _enemySingles.Count; i++)
        {
            EnemyPawn enemyPawn = _enemySingles[i];
            bool pathFound = _pathfinder.FindPath(enemyPawn, enemyPawn.Target, out List<Vector2Int> path,
                _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
            if (pathFound)
            {
                if (path.Count > 0)
                {
                    Vector2Int destCell = path.PickLastElement();
                    _pathfinder.CreatePathSprites(path, 1, 0);
                    yield return board.MovePawnRoutine(enemyPawn, destCell);
                    movedEnemiesCount++;
                    if (path.Count <= 1)
                    {
                        yield return new WaitForSeconds(0.5f);
                        board.BoardScreen.ShowFailPopup();
                        onEnd?.Invoke(movedEnemiesCount > 0);
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
        foreach (var pair in _enemyPairs)
        {
            EnemyPawn enemyPawnA = pair.Pawn1;
            EnemyPawn enemyPawnB = pair.Pawn2;
            bool pathFound = _pathfinder.FindPath(enemyPawnA, enemyPawnB, out List<Vector2Int> path,
                _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
            if (pathFound)
            {
                if (path.Count > 0)
                {
                    path.RemoveAt(0);
                    Vector2Int destCell = path.PickLastElement();
                    List<SpriteRenderer> pathSprites = _pathfinder.CreatePathSprites(path, 0, 0);
                    yield return board.MovePawnRoutine(enemyPawnA, destCell);
                    movedEnemiesCount++;
                    if (path.Count > 0)
                    {
                        Destroy(pathSprites.PickFirstElement().gameObject);
                        destCell = path.PickFirstElement();
                        yield return board.MovePawnRoutine(enemyPawnB, destCell);
                        movedEnemiesCount++;
                    }
                    if (path.Count < 1)
                    {
                        yield return new WaitForSeconds(0.5f);
                        board.BoardScreen.ShowFailPopup();
                        onEnd?.Invoke(movedEnemiesCount > 0);
                        yield break;
                    }
                }
            }
            else
            {
                bool pawnASurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawnA, out path, _playerPawnsContainer, _enemyTargetsContainer);
                bool pawnBSurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawnB, out path, _playerPawnsContainer, _enemyTargetsContainer);
                surroundedEnemiesCount += pawnASurrounded ? 1 : 0;
                surroundedEnemiesCount += pawnBSurrounded ? 1 : 0;
            }
        }
        if (surroundedEnemiesCount == _enemyPawns.Count)
        {
            yield return new WaitForSeconds(0.5f);
            board.BoardScreen.ShowSuccessPopup();
        }
        onEnd?.Invoke(movedEnemiesCount > 0);
    }

    public void AddPawnToLists(List<EnemyPawn> enemyPawns, bool sortEnemyPawns)
    {
        _enemyPawns.Clear();
        _enemyPairs.Clear();
        _enemySingles.Clear();
        List<EnemyPawn> enemyPawnsWithPawnTargets = new List<EnemyPawn>(); //_enemyPawns.FindAll(pawn => pawn.TargetIsOtherPawn);
        foreach (var pawn in enemyPawns)
        {
            if (pawn.TargetIsOtherPawn)
                enemyPawnsWithPawnTargets.Add(pawn);
            else
                _enemySingles.Add(pawn);
        }

        //Debug.Log(GetType() + "." + enemyPawnsWithPawnTargets.Count);
        for (int i = 0; i < enemyPawnsWithPawnTargets.Count; i++)
        {
            EnemyPawn pawnI = enemyPawnsWithPawnTargets[i];
            for (int j = i + 1; j < enemyPawnsWithPawnTargets.Count; j++)
            {
                EnemyPawn pawnJ = enemyPawnsWithPawnTargets[j];
                if (pawnI.Target == pawnJ.GetComponent<EnemyPawnTarget>() &&
                    pawnJ.Target == pawnI.GetComponent<EnemyPawnTarget>())
                {
                    _enemyPairs.Add(new EnemyPawnsPair(pawnI, pawnJ));
                }
            }
        }
        if (sortEnemyPawns)
        {
            //_enemyPawns.Sort((pawnA, pawnB) => {
            //    bool pawnATargetIsOtherPawn = pawnA.TargetIsOtherPawn;
            //    bool pawnBTargetIsOtherPawn = pawnB.TargetIsOtherPawn;
            //    if (pawnATargetIsOtherPawn == pawnBTargetIsOtherPawn)
            //        return 0;
            //    else if (pawnATargetIsOtherPawn && !pawnBTargetIsOtherPawn)
            //        return 1;
            //    else if (pawnBTargetIsOtherPawn && !pawnATargetIsOtherPawn)
            //        return -1;
            //    else
            //        return 0;
            //});
            enemyPawns.Clear();
            enemyPawns.AddRange(_enemySingles);
            enemyPawns.AddRange(enemyPawnsWithPawnTargets);
        }
        _enemyPawns.AddRange(enemyPawns);
    }
}
