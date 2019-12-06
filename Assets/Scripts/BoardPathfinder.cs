//#define DEBUG_PATHFINDING
#define DEBUG_SHOW_PATH
//#define DEBUG_SHOW_DISTANCES
//#define DEBUG_DISTANCE_INT

using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardPathfinder : MonoBehaviour
{
    [SerializeField] private Transform _tileTextsContainer = default;
    [SerializeField] private TextMesh _tileTextMeshPrefab = default;
    [SerializeField] private Transform _pathSpritesContainer = default;
    [SerializeField] private SpriteRenderer _pathSpritePrefab = default;

    private readonly float SQRT2 = Mathf.Sqrt(2f);

#if DEBUG_PATHFINDING
    private Coroutine _updateCellNodesRoutine = default;
#endif
    private Transform _pawnsContainer = default;
    private BoardTilemap _tilemap = default;

    private readonly Vector2Int[] _cellNgbrsDeltaXY = new Vector2Int[]
    {
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(-1, 0), new Vector2Int(1, 0),
        new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
    };

    private struct CellNode
    {
        public bool locked;
        public bool @checked;
        public float distance;
    }

    private void Awake()
    {
        Board board = GetComponentInParent<Board>();
        _pawnsContainer = board.PawnsContainer;
        _tilemap = board.Tilemap;
    }

    public void FindPath(Pawn pawn, PawnTarget target, Action<List<Vector2Int>> onEnd)
    {
        Bounds2Int bounds = _tilemap.GetPawnsCellBounds();
        var nodes = CreateCellNodes(bounds);
        Vector2Int begXY = _tilemap.WorldToCell(pawn.transform.position) - bounds.Min;
        Vector2Int endXY = _tilemap.WorldToCell(target.transform.position) - bounds.Min;

        //Debug.Log(GetType() + ".FindPathToTarget: bounds: " + bounds.Min + " " + bounds.Max + " " + bounds.Size + " ");
        Debug.Log(GetType() + ".FindPathToTarget: " + begXY + "->" + endXY);

        ref CellNode begNode = ref nodes[GetCellNodeIndex(begXY, bounds.Size)];
        begNode.distance = 0f;

        _tileTextsContainer.transform.DestroyAllChildren();
        _pathSpritesContainer.DestroyAllChildren();

#if DEBUG_PATHFINDING
        if (_updateCellNodesRoutine != null)
        {
            StopCoroutine(_updateCellNodesRoutine);
            ClearTilesMeshTexts(bounds);
        }
        _updateCellNodesRoutine = StartCoroutine(UpdateCellNodesRoutine(nodes, bounds, begXY, endXY, () => {
            ClearTilesMeshTexts(bounds);
            onEnd(CreatePath(nodes, bounds, begXY, endXY));
        }));
#else
        HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int>() { begXY };
        while (nodesXY.Count > 0)
        {
            nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY);
        }
        ClearTilesMeshTexts(bounds);
        onEnd(CreatePath(nodes, bounds, begXY, endXY));
#endif
    }

    private List<Vector2Int> CreatePath(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        CellNode endNode = nodes[GetCellNodeIndex(endXY, bounds.Size)];
        if (endNode.distance < float.MaxValue / 2f)
        {
            path = GetPath(nodes, bounds, begXY, endXY);
#if DEBUG_SHOW_PATH
            CreatePathSprites(path);
#endif
        }
        return path;
    }

    private CellNode[] CreateCellNodes(Bounds2Int bounds)
    {
        var nodes = new CellNode[bounds.Size.x * bounds.Size.y];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].distance = float.MaxValue;
        }
        foreach (Transform pawnTransform in _pawnsContainer)
        {
            Vector2Int cell = _tilemap.WorldToCell(pawnTransform.position);
            Vector2Int xy = cell - bounds.Min;
            int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
            nodes[nodeIndex].locked = true;
        }
        return nodes;
    }

    private HashSet<Vector2Int> UpdateCellNodes(CellNode[] nodes, Bounds2Int bounds, HashSet<Vector2Int> nodesXY, Vector2Int targetXY)
    {
        HashSet<Vector2Int> nextNodesXY = new HashSet<Vector2Int>();

        foreach (var nodeXY in nodesXY)
        {
            int nodeIndex = GetCellNodeIndex(nodeXY, bounds.Size);
            ref CellNode node = ref nodes[nodeIndex];
            node.@checked = true;
            nextNodesXY.Remove(nodeXY);
            foreach (var ngbrDeltaXY in _cellNgbrsDeltaXY)
            {
                Vector2Int ngbrXY = nodeXY + ngbrDeltaXY;
                if (CellNodeInBounds(ngbrXY, bounds.Size))
                {
                    int ngbrNodeIndex = GetCellNodeIndex(ngbrXY, bounds.Size);
                    ref CellNode ngbrNode = ref nodes[ngbrNodeIndex];
                    if (!ngbrNode.locked && !ngbrNode.@checked)
                    {
                        if (!nextNodesXY.Contains(ngbrXY))
                        {
                            nextNodesXY.Add(ngbrXY);
                        }
#if DEBUG_DISTANCE_INT
                        float delta = 1f;
#else
                        float delta = ngbrDeltaXY.x == 0 || ngbrDeltaXY.y == 0 ? 1f : SQRT2;
#endif
                        ngbrNode.distance = Mathf.Min(ngbrNode.distance, node.distance + delta);
#if DEBUG_SHOW_DISTANCES
                        SetTileMeshText(ngbrXY + bounds.Min, ngbrNode.distance.ToString("F1"));
#endif
                        if (ngbrXY == targetXY)
                        {
                            nextNodesXY.Clear();
                            return nextNodesXY;
                        }
                    }
                }
            }
        }
        return nextNodesXY;
    }

#if DEBUG_PATHFINDING
    private IEnumerator UpdateCellNodesRoutine(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY, Action onEnd)
    {
        HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int> { begXY };
        while (nodesXY.Count > 0)
        {
            nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY);
            yield return new WaitForSeconds(0.05f);
        }
        _updateCellNodesRoutine = null;
        onEnd();
    }
#endif

    private List<Vector2Int> GetPath(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (begXY != endXY)
        {
            ref CellNode begNode = ref nodes[GetCellNodeIndex(begXY, bounds.Size)];
            begNode.locked = false;

            Vector2Int nodeXY = endXY;
            while (nodeXY != begXY)
            {
                nodeXY = GetMinDistanceCellNodeNgbrXY(nodes, nodeXY, bounds);
                path.Add(nodeXY);
            }
            if (path.Count > 0)
            {
                path.RemoveAt(path.Count - 1);
                for (int i = 0; i < path.Count; i++)
                {
                    path[i] += bounds.Min;
                    //Debug.Log(GetType() + ".GetPath: " + path[i]);
                }
                begNode.locked = true;
            }
        }
        return path;
    }

    private Vector2Int GetMinDistanceCellNodeNgbrXY(CellNode[] nodes, Vector2Int nodeXY, Bounds2Int bounds)
    {
        float minDistance = float.MaxValue;
        Vector2Int minDistNgbrXY = default;
        foreach (var ngbrDeltaXY in _cellNgbrsDeltaXY)
        {
            Vector2Int ngbrXY = nodeXY + ngbrDeltaXY;
            if (CellNodeInBounds(ngbrXY, bounds.Size))
            {
                int ngbrNodeIndex = GetCellNodeIndex(ngbrXY, bounds.Size);
                CellNode ngbrNode = nodes[ngbrNodeIndex];
                if (!ngbrNode.locked && ngbrNode.distance < minDistance)
                {
                    minDistance = ngbrNode.distance;
                    minDistNgbrXY = ngbrXY;
                }
            }
        }
        //Debug.Log(GetType() + ".GetMinDistanceCellNodeNgbrXY: " + minDistNgbrXY);
        return minDistNgbrXY;
    }

    private void CreatePathSprites(List<Vector2Int> path)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            Instantiate(_pathSpritePrefab, _tilemap.GetCellCenterWorld(path[i]), Quaternion.identity, _pathSpritesContainer);
        }
    }

    private void SetTileMeshText(Vector2Int cell, string text)
    {
        BoardTile tile = _tilemap.GetTile(cell);
        Vector3 tilePos = _tilemap.GetCellCenterWorld(cell) - Vector3.forward;
        if (tile)
        {
            if (!tile.TextMesh)
            {
                tile.TextMesh = Instantiate(_tileTextMeshPrefab, tilePos, Quaternion.identity, _tileTextsContainer);
            }
            tile.TextMesh.text = text;
        }
    }

    private void ClearTilesMeshTexts(Bounds2Int bounds)
    {
        for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
        {
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            {
                BoardTile tile = _tilemap.GetTile(new Vector2Int(x, y));
                if (tile)
                    tile.TextMesh = null;
            }
        }
    }

    //private ref CellNode GetCollNodeRef(CellNode[] nodes, Vector2Int xy, Bounds2Int bounds)
    //{
    //    int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
    //    return ref nodes[nodeIndex];
    //}

    private int GetCellNodeIndex(int x, int y, int columns)
    {
        return x + y * columns;
    }

    private int GetCellNodeIndex(Vector2Int xy, Vector2Int size)
    {
        return xy.x + xy.y * size.x;
    }

    private bool CellNodeInBounds(Vector2Int xy, Vector2Int size)
    {
        return xy.x >= 0 && xy.x < size.x && xy.y >= 0 && xy.y < size.y;
    }
}
