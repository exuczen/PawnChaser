//#define DEBUG_PATHFINDING

using MustHave;
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
    private BoardTilemap _tilemap = default;
    private List<EnemyPawn> _enemyPawns = new List<EnemyPawn>();
    private List<EnemyPawn> _enemySingles = new List<EnemyPawn>();
    private List<EnemyPawnsPair> _enemyPairs = new List<EnemyPawnsPair>();

    private void Awake()
    {
        Board board = GetComponent<Board>();
        _tilemap = board.Tilemap;
        _pathfinder = GetComponent<BoardPathfinder>();
    }

    private IEnumerator MovePawnRoutine(Pawn pawn, Vector2Int destCell)
    {
        yield return pawn.MoveRoutine(_tilemap, destCell);
    }

    private IEnumerator MoveEnemyPawnsPairRoutine(EnemyPawnsPair pair, List<Vector2Int> path)
    {
        if (path.Count < 1)
        {
            yield break;
        }
        Vector2Int pawn1EndCell;
        PawnTransition pawn1Transition = new PawnTransition(pair.Pawn1, _tilemap, pawn1EndCell = path.PickLastElement());
        PawnTransition pawn2Transition = new PawnTransition(pair.Pawn2, _tilemap, path.Count > 0 ? path.PickFirstElement() : pawn1EndCell);
        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            pawn1Transition.Update(shift);
            pawn2Transition.Update(shift);
        });
        pawn1Transition.Finish(_tilemap);
        pawn2Transition.Finish(_tilemap);
    }

    private IEnumerator MoveEnemyPawnsRoutine(List<PawnTransition> pawnTransitions)
    {
        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            foreach (var pawnTransition in pawnTransitions)
            {
                pawnTransition.Update(shift);
            }
        });
        foreach (var pawnTransition in pawnTransitions)
        {
            pawnTransition.Finish(_tilemap);
        }
    }

    public IEnumerator MoveEnemyPawnsRoutine(Action<bool> onEnd, Action onSuccess, Action onFail)
    {
        int surroundedEnemiesCount = 0;
        int movedEnemiesCount = 0;
        bool enemyReachedTarget = false;
        _pathfinder.ClearSprites();

        List<PathResult> pathResults = new List<PathResult>();
        List<PawnTransition> pawnTransitions = new List<PawnTransition>();
        List<Vector2Int> lockedCells = new List<Vector2Int>();
        foreach (var enemyPawn in _enemySingles)
        {
            PathResult pathResult = null;
#if DEBUG_PATHFINDING
            yield return _pathfinder.FindPathRoutine(enemyPawn, enemyPawn.Target, aPathResult => {
                pathResult = aPathResult;
            }, pawnTransitions, _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
#else
            pathResult = _pathfinder.FindPath(enemyPawn, enemyPawn.Target, pawnTransitions,
                _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
#endif
            pathResults.Add(pathResult);
            var path = pathResult.Path;
            if (pathResult.PathFound && path.Count > 0)
            {
                _pathfinder.CreatePathSprites(path, 1, 1);
                PawnTransition pawnTransition = new PawnTransition(enemyPawn, _tilemap, path.PickLastElement());
                pawnTransitions.Add(pawnTransition);
                lockedCells.Add(pawnTransition.EndCell);
            }
        }
        if (pawnTransitions.Count > 0)
        {
            yield return MoveEnemyPawnsRoutine(pawnTransitions);
            movedEnemiesCount += pawnTransitions.Count;
        }
        for (int i = 0; i < pathResults.Count; i++)
        {
            EnemyPawn enemyPawn = _enemyPawns[i];
            PathResult pathResult = pathResults[i];
            var path = pathResult.Path;
            if (pathResult.PathFound)
            {
                if (path.Count <= 1)
                {
                    enemyReachedTarget = true;
                    break;
                }
            }
            else
            {
                bool targetSurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawn.Target, _playerPawnsContainer, _enemyTargetsContainer).PathFound;
                bool enemySurrounded = targetSurrounded ? !_pathfinder.FindPathToBoundsMin(enemyPawn, _playerPawnsContainer).PathFound : true;
                surroundedEnemiesCount += enemySurrounded ? 1 : 0;
            }
        }
        foreach (var pair in _enemyPairs)
        {
            EnemyPawn enemyPawnA = pair.Pawn1;
            EnemyPawn enemyPawnB = pair.Pawn2;
            PathResult pathResult = null;
#if DEBUG_PATHFINDING
            yield return _pathfinder.FindPathRoutine(enemyPawnA, enemyPawnB, aPathResult => {
                pathResult = aPathResult;
            }, null, _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
#else
            pathResult = _pathfinder.FindPath(enemyPawnA, enemyPawnB, null, _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
#endif
            if (pathResult.PathFound)
            {
                var path = pathResult.Path;
                if (path.Count > 0)
                {
                    path.RemoveAt(0);
                    if (path.Count > 0)
                    {
                        List<SpriteRenderer> pathSprites = _pathfinder.CreatePathSprites(path, 1, 1);
                        yield return MoveEnemyPawnsPairRoutine(pair, path);
                        movedEnemiesCount += 2;
                    }
                    if (path.Count < 1)
                    {
                        enemyReachedTarget = true;
                        break;
                    }
                }
            }
            else
            {
                bool pawnASurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawnA, _playerPawnsContainer, _enemyTargetsContainer).PathFound;
                bool pawnBSurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawnB, _playerPawnsContainer, _enemyTargetsContainer).PathFound;
                surroundedEnemiesCount += pawnASurrounded ? 1 : 0;
                surroundedEnemiesCount += pawnBSurrounded ? 1 : 0;
            }
        }
        if (enemyReachedTarget)
        {
            yield return new WaitForSeconds(0.5f);
            onFail?.Invoke();
        }
        else if (surroundedEnemiesCount == _enemyPawns.Count)
        {
            yield return new WaitForSeconds(0.5f);
            onSuccess?.Invoke();
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
