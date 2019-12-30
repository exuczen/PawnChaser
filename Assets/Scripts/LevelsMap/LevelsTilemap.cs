using MustHave;
using MustHave.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelsTilemap : GridTilemap<GridTile>
{
    [SerializeField] Transform _levelsContainer = default;

    private float _planeHalfHeightInView = default;

    protected override void OnStart()
    {
        Ray viewTopRay = _camera.ViewportPointToRay(new Vector3(0.5f, 1f, 0f));
        Ray viewBtmRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
        if (
            Maths.GetRayIntersectionWithPlane(viewTopRay, -transform.forward, transform.position, out _, out float a) &&
            Maths.GetRayIntersectionWithPlane(viewBtmRay, -transform.forward, transform.position, out _, out float b))
        {
            _planeHalfHeightInView = Mathf.Sqrt(a * a + b * b - 2 * a * b * Mathf.Cos(Mathf.Deg2Rad * _camera.fieldOfView)) / 2f;
        }
        if (EditorApplicationUtils.ApplicationIsPlaying)
        {
            ResetTilemap();
        }
        Quaternion lookAtCameraRotation = Quaternion.LookRotation(_camera.transform.forward, _camera.transform.up);
        foreach (Transform levelPointer in _levelsContainer)
        {
            levelPointer.rotation = lookAtCameraRotation;
        }
    }

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
        Ray viewTopRay = _camera.ViewportPointToRay(new Vector3(0.5f, 1f, 0f));
        Ray viewBtmRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0f, 0f));
        if (
            Maths.GetRayIntersectionWithPlane(viewTopRay, -transform.forward, transform.position, out Vector3 farIsecPoint) &&
            Maths.GetRayIntersectionWithPlane(viewBtmRay, -transform.forward, transform.position, out Vector3 closeIsecPoint))
        {
            Vector3 worldPoint = (farIsecPoint + closeIsecPoint) / 2f;
            return WorldToCell(worldPoint);
        }
        else
        {
            return Vector2Int.zero;
        }
        //if (_camera.GetRayIntersectionWithPlane(-transform.forward, transform.position, out Vector3 worldPoint, out _))
        //{
        //    return WorldToCell(worldPoint);
        //}
    }

    protected override void GetHalfViewSizeXY(out int halfXCount, out int halfYCount)
    {
        //Debug.Log(GetType() + ".GetHalfViewSizeXY: " + Screen.width + " " + Screen.height);
        halfYCount = (int)(_planeHalfHeightInView / _grid.cellSize.y);
        halfXCount = ((halfYCount * Screen.width / Screen.height) + 2) / 2;
        halfYCount += 1;
    }


    protected override void ResetCamera()
    {
    }
}
