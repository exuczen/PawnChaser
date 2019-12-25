using MustHave;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelsTouchHandler : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private LevelsMap _levelsMap = default;

    private LevelsTilemap _tilemap;

    protected override void Awake()
    {
        _tilemap = _levelsMap.Tilemap;
    }

    private void TranslateCamera(Vector2 screenDelta)
    {
        Camera camera = Camera.main;
        if (camera.GetRayIntersectionWithPlane(_tilemap.transform.forward, _tilemap.transform.position, out _, out float distance))
        {
            Vector3 translation = camera.ScreenToWorldTranslation(screenDelta, distance);
            //Debug.Log(GetType() + ".TranslateCamera: " + translation.ToString("F2"));
            float translationX = translation.x;
            translation.x = 0f;
            float translationY = Mathf.Sign(Vector3.Dot(translation, camera.transform.up)) * translation.magnitude;
            translation = new Vector3(translationX, translationY, 0f);
            camera.transform.Translate(translation, Space.World);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        TranslateCamera(eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }
}
