using MustHave;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelsTilemap : GridTilemap<GridTile>
{
    public override void SetTilesContent()
    {
    }

    protected override GridTile CreateTile(int x, int y)
    {
        GridTile tile = Instantiate(_tiles[0]);
        tile.color = Color.Lerp(Color.HSVToRGB(0f, 0f, 0.7f), Color.HSVToRGB(0f, 0f, 0.8f), UnityEngine.Random.Range(0f, 1f));
        return tile;
    }

    protected override Vector2Int GetCameraCell()
    {
        if (_camera.GetRayIntersectionWithPlane(-transform.forward, transform.position, out Vector3 worldPoint, out _))
        {
            return WorldToCell(worldPoint);
        }
        else
        {
            return Vector2Int.zero;
        }
    }

    protected override void ResetCamera()
    {
    }
}
