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
        Transform pawn1Transform = pair.Pawn1.transform;
        Transform pawn2Transform = pair.Pawn2.transform;

        Vector2Int pawn1BegCell = _tilemap.WorldToCell(pawn1Transform.position);
        Vector2Int pawn1EndCell = path.PickLastElement();
        Vector3 pawn1BegPos = pawn1Transform.position;
        Vector3 pawn1EndPos = _tilemap.GetCellCenterWorld(pawn1EndCell);

        Vector2Int pawn2BegCell = _tilemap.WorldToCell(pawn2Transform.position);
        Vector2Int pawn2EndCell = path.Count > 0 ? path.PickFirstElement() : pawn1EndCell;
        Vector3 pawn2BegPos = pawn2Transform.position;
        Vector3 pawn2EndPos = _tilemap.GetCellCenterWorld(pawn2EndCell);

        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            pawn1Transform.position = Vector3.Lerp(pawn1BegPos, pawn1EndPos, shift);
            pawn2Transform.position = Vector3.Lerp(pawn2BegPos, pawn2EndPos, shift);
        });
        pawn1Transform.position = pawn1EndPos;
        pawn2Transform.position = pawn2EndPos;

        BoardTile tile;

        if (tile = _tilemap.GetTile(pawn1BegCell))
            tile.Content = null;
        if (tile = _tilemap.GetTile(pawn1EndCell))
            tile.Content = pawn1Transform.GetComponent<TileContent>();

        if (tile = _tilemap.GetTile(pawn2BegCell))
            tile.Content = null;
        if (tile = _tilemap.GetTile(pawn2EndCell))
            tile.Content = pawn2Transform.GetComponent<TileContent>();
    }


    public IEnumerator MoveEnemyPawnsRoutine(Action<bool> onEnd, Action onSuccess, Action onFail)
    {
        int surroundedEnemiesCount = 0;
        int movedEnemiesCount = 0;
        bool enemyReachedTarget = false;
        for (int i = 0; i < _enemySingles.Count; i++)
        {
            EnemyPawn enemyPawn = _enemySingles[i];
            bool pathFound = _pathfinder.FindPath(enemyPawn, enemyPawn.Target, out List<Vector2Int> path,
                _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
            if (pathFound)
            {
                if (path.Count > 0)
                {
                    _pathfinder.CreatePathSprites(path, 1, 1);
                    Vector2Int destCell = path.PickLastElement();
                    yield return MovePawnRoutine(enemyPawn, destCell);
                    movedEnemiesCount++;
                    if (path.Count <= 1)
                    {
                        enemyReachedTarget = true;
                        break;
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
#if DEBUG_PATHFINDING
            bool pathFound = false;
            List<Vector2Int> path = null;
            yield return _pathfinder.FindPathRoutine(enemyPawnA, enemyPawnB, (aPathFound, aPath) => {
                pathFound = aPathFound;
                path = aPath;
            }, _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
#else
            bool pathFound = _pathfinder.FindPath(enemyPawnA, enemyPawnB, out List<Vector2Int> path,
                _playerPawnsContainer, _enemyPawnsContainer, _enemyTargetsContainer);
#endif
            if (pathFound)
            {
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
                bool pawnASurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawnA, out path, _playerPawnsContainer, _enemyTargetsContainer);
                bool pawnBSurrounded = !_pathfinder.FindPathToBoundsMin(enemyPawnB, out path, _playerPawnsContainer, _enemyTargetsContainer);
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
