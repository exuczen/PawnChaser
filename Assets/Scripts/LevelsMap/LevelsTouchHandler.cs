using MustHave;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelsTouchHandler : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private LevelsMap _levelsMap = default;

    private LevelsTilemap _tilemap = default;
    private Camera _camera = default;
    private SpriteRenderer _selectedLevelPointerSprite = default;

    protected override void Awake()
    {
        _tilemap = _levelsMap.Tilemap;
        _camera = _tilemap.Camera;
    }

    private void TranslateCamera(PointerEventData eventData)
    {
        Camera camera = _camera;
        //if (camera.GetRayIntersectionWithPlane(_tilemap.transform.forward, _tilemap.transform.position, out _, out float distance))
        //{
        //    Vector3 translation = camera.ScreenToWorldTranslation(eventData.delta, distance);
        //    //Debug.Log(GetType() + ".TranslateCamera: " + translation.ToString("F2"));
        //    float translationX = translation.x;
        //    translation.x = 0f;
        //    float translationY = Mathf.Sign(Vector3.Dot(translation, camera.transform.up)) * translation.magnitude;
        //    translation = new Vector3(translationX, translationY, 0f);
        //    camera.transform.Translate(translation, Space.World);
        //}
        if (
            Maths.GetTouchRayIntersectionWithPlane(camera, eventData.position, _tilemap.transform.forward, _tilemap.transform.position, out Vector3 currIsecPt) &&
            Maths.GetTouchRayIntersectionWithPlane(camera, eventData.position - eventData.delta, _tilemap.transform.forward, _tilemap.transform.position, out Vector3 prevIsecPt)
            )
        {
            Vector3 translation = prevIsecPt - currIsecPt;
            translation.y = 0f;
            camera.transform.Translate(translation, Space.World);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        TranslateCamera(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Ray touchRay = _camera.ScreenPointToRay(eventData.position);
        //Debug.Log(GetType() + ".OnPointerDown");
        //RaycastHit2D hit = Physics2D.GetRayIntersection(touchRay, 100f, Layer.LevelPointersMask);
        if (Physics.Raycast(touchRay, out RaycastHit hit, 1000f, Layer.LevelPointersMask))
        {
            if (_selectedLevelPointerSprite)
            {
                _selectedLevelPointerSprite.color = Color.white;
            }
            _selectedLevelPointerSprite = hit.transform.GetComponent<SpriteRenderer>();
            _selectedLevelPointerSprite.color = Color.black;
            //Debug.Log(GetType() + ".OnPointerDown: " + _selectedLevelPointerSprite);
        }
        //Debug.DrawRay(touchRay.origin, 100f * touchRay.direction, Color.white, 5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_selectedLevelPointerSprite)
        {
            _selectedLevelPointerSprite.color = Color.white;
        }
        _selectedLevelPointerSprite = null;
    }
}
