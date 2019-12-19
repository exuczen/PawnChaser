//#define DEBUG_SHOW_DISTANCES
//#define DEBUG_DISTANCE_INT

using MustHave;
using MustHave.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathResult : Tuple<bool, List<Vector2Int>>
{
    public PathResult(bool item1, List<Vector2Int> item2) : base(item1, item2) { }

    public bool PathFound => Item1;
    public List<Vector2Int> Path => Item2;
}

public class BoardPathfinder : MonoBehaviour
{
    [SerializeField] private Transform _tileTextsContainer = default;
    [SerializeField] private TextMesh _tileTextMeshPrefab = default;
    [SerializeField] private Transform _pathSpritesContainer = default;
    [SerializeField] private SpriteRenderer _pathSpritePrefab = default;

    private readonly float SQRT2 = Mathf.Sqrt(2f);

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
        Board board = GetComponent<Board>();
        _tilemap = board.Tilemap;
    }

    public void ClearSprites()
    {
        _tileTextsContainer.transform.DestroyAllChildren();
        _pathSpritesContainer.DestroyAllChildren();
    }

    private IEnumerator FindPathRoutine(Vector2Int begXY, Vector2Int endXY, Bounds2Int bounds, Action<PathResult> onEnd,
        List<Vector2Int> lockedCells, params Transform[] contentContainers)
    {
        if (begXY == endXY)
        {
            onEnd?.Invoke(new PathResult(true, new List<Vector2Int>()));
        }
        else
        {
            begXY -= bounds.Min;
            endXY -= bounds.Min;
            var nodes = CreateCellNodes(begXY, endXY, bounds, lockedCells, contentContainers);

            _tileTextsContainer.transform.DestroyAllChildren();

            HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int>() { begXY };

            yield return UpdateCellNodesRoutine(nodes, bounds, begXY, endXY, targetReached => {
                ClearTilesMeshTexts(bounds);
                onEnd?.Invoke(new PathResult(targetReached, CreatePath(nodes, bounds, begXY, endXY)));
            });
        }
    }

    private PathResult FindPath(Vector2Int begXY, Vector2Int endXY, Bounds2Int bounds, List<Vector2Int> lockedCells, params Transform[] contentContainers)
    {
        if (begXY == endXY)
        {
            return new PathResult(true, new List<Vector2Int>());
        }
        else
        {
            begXY -= bounds.Min;
            endXY -= bounds.Min;
            //Debug.Log(GetType() + ".FindPathToTarget: bounds: " + bounds.Min + " " + bounds.Max + " " + bounds.Size + " ");
            //Debug.Log(GetType() + ".FindPathToTarget: " + begXY + "->" + endXY);
            var nodes = CreateCellNodes(begXY, endXY, bounds, lockedCells, contentContainers);

            _tileTextsContainer.transform.DestroyAllChildren();

            HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int>() { begXY };
            bool targetReached = false;
            while (nodesXY.Count > 0)
            {
                nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY, out targetReached);
            }
            ClearTilesMeshTexts(bounds);
            return new PathResult(targetReached, CreatePath(nodes, bounds, begXY, endXY));
        }
    }

    public IEnumerator FindPathRoutine(TileContent begTileContent, TileContent endTileContent, Action<PathResult> onEnd,
        List<Vector2Int> lockedCells, params Transform[] contentContainers)
    {
        Vector2Int begXY = _tilemap.WorldToCell(begTileContent.transform.position);
        Vector2Int endXY = _tilemap.WorldToCell(endTileContent.transform.position);
        Bounds2Int bounds = _tilemap.GetTilesContentCellBounds(begXY, endXY, contentContainers);
        yield return FindPathRoutine(begXY, endXY, bounds, onEnd, lockedCells, contentContainers);
    }

    public PathResult FindPath(TileContent begTileContent, TileContent endTileContent,
        List<Vector2Int> lockedCells, params Transform[] contentContainers)
    {
        Vector2Int begXY = _tilemap.WorldToCell(begTileContent.transform.position);
        Vector2Int endXY = _tilemap.WorldToCell(endTileContent.transform.position);
        Bounds2Int bounds = _tilemap.GetTilesContentCellBounds(begXY, endXY, contentContainers);
        return FindPath(begXY, endXY, bounds, lockedCells, contentContainers);
    }

    public PathResult FindPathToBoundsMin(TileContent begTileContent, params Transform[] contentContainers)
    {
        Vector2Int begXY = _tilemap.WorldToCell(begTileContent.transform.position);
        Bounds2Int bounds = _tilemap.GetTilesContentCellBounds(begXY, contentContainers);
        Vector2Int endXY = bounds.Min;
        return FindPath(begXY, endXY, bounds, null, contentContainers);
    }

    private List<Vector2Int> CreatePath(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        CellNode endNode = nodes[GetCellNodeIndex(endXY, bounds.Size)];
        if (begXY != endXY && endNode.distance < float.MaxValue / 2f)
        {
            //Debug.Log(GetType() + ".GetPath: " + begXY + "->" + endXY);
            ref CellNode begNode = ref nodes[GetCellNodeIndex(begXY, bounds.Size)];
            bool begNodeLocked = begNode.locked;
            begNode.locked = false;

            Vector2Int nodeXY = endXY;
            path.Add(nodeXY);
            while (nodeXY != begXY)
            {
                nodeXY = GetMinDistanceNgbrCellNodeXY(nodes, nodeXY, bounds);
                path.Add(nodeXY);
            }
            if (path.Count > 1)
                path.RemoveAt(path.Count - 1);
            for (int i = 0; i < path.Count; i++)
            {
                path[i] += bounds.Min;
                //Debug.Log(GetType() + ".GetPath: " + path[i]);
            }
            begNode.locked = begNodeLocked;
        }
        return path;
    }

    private CellNode[] CreateCellNodes(Vector2Int begXY, Vector2Int endXY, Bounds2Int bounds, List<Vector2Int> lockedCells, params Transform[] contentContainers)
    {
        var nodes = new CellNode[bounds.Size.x * bounds.Size.y];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i].distance = float.MaxValue;
        }
        foreach (Transform container in contentContainers)
        {
            foreach (Transform pawnTransform in container)
            {
                Vector2Int cell = _tilemap.WorldToCell(pawnTransform.position);
                Vector2Int xy = cell - bounds.Min;
                int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
                nodes[nodeIndex].locked = true;
            }
        }
        if (lockedCells != null)
        {
            foreach (var lockedCell in lockedCells)
            {
                Vector2Int xy = lockedCell - bounds.Min;
                int nodeIndex = GetCellNodeIndex(xy, bounds.Size);
                nodes[nodeIndex].locked = true;
            }
        }
        ref CellNode begNode = ref nodes[GetCellNodeIndex(begXY, bounds.Size)];
        ref CellNode endNode = ref nodes[GetCellNodeIndex(endXY, bounds.Size)];
        begNode.distance = 0f;
        begNode.locked = false;
        endNode.locked = false;
        return nodes;
    }

    private HashSet<Vector2Int> UpdateCellNodes(CellNode[] nodes, Bounds2Int bounds, HashSet<Vector2Int> nodesXY, Vector2Int targetXY, out bool targetReached)
    {
        HashSet<Vector2Int> nextNodesXY = new HashSet<Vector2Int>();
        targetReached = false;
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
                            targetReached = true;
                            nextNodesXY.Clear();
                            return nextNodesXY;
                        }
                    }
                }
            }
        }
        return nextNodesXY;
    }

    private IEnumerator UpdateCellNodesRoutine(CellNode[] nodes, Bounds2Int bounds, Vector2Int begXY, Vector2Int endXY, Action<bool> onEnd)
    {
        HashSet<Vector2Int> nodesXY = new HashSet<Vector2Int> { begXY };
        bool targetReached = false;
        while (nodesXY.Count > 0)
        {
            nodesXY = UpdateCellNodes(nodes, bounds, nodesXY, endXY, out targetReached);
            yield return new WaitForSeconds(0.05f);
        }
        onEnd(targetReached);
    }

    private Vector2Int GetMinDistanceNgbrCellNodeXY(CellNode[] nodes, Vector2Int nodeXY, Bounds2Int bounds)
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

    public List<SpriteRenderer> CreatePathSprites(List<Vector2Int> path, int begOffset, int endOffset)
    {
        List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        int beg = begOffset;
        int end = path.Count - endOffset - 1;
        for (int i = beg; i <= end; i++)
        {
            SpriteRenderer sprite = Instantiate(_pathSpritePrefab, _tilemap.GetCellCenterWorld(path[i]), Quaternion.identity, _pathSpritesContainer);
            sprites.Add(sprite);
        }
        return sprites;
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
