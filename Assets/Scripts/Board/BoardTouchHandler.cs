using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MustHave;
using MustHave.Utils;
using System;
using UnityEngine.UI;

public class BoardTouchHandler : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Board _board = default;
    //[SerializeField] private Image _raycastBlocker = default;
    [SerializeField] private Image _selectionCircle = default;
    [SerializeField] private Image _targetCircle = default;

    //[SerializeField] private Transform _marker = default;

    private BoardTilemap _tilemap = default;
    private PlayerPawn _selectedPawn = default;
    private int _selectedPawnPointerId = int.MinValue;
    private Coroutine _movePawnRoutine = default;

    public Board Board { get => _board; }

    protected override void Awake()
    {
        _tilemap = _board?.Tilemap;
    }

    protected override void Start()
    {
        SetUIComponentsSize();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        SetUIComponentsSize();
    }

    private void SetUIComponentsSize()
    {
        if (Camera.main && _tilemap && _tilemap.Tilemap)
        {
            Vector2 screenImageSize = transform.GetComponent<Image>().rectTransform.rect.size;
            float viewTilesYCount = 2f * Camera.main.orthographicSize / _tilemap.Tilemap.cellSize.y;
            float circleScreenWidth = screenImageSize.y / viewTilesYCount;
            float circleScale = 2.3f;
            _selectionCircle.rectTransform.sizeDelta = Vector2.one * circleScreenWidth * circleScale;
            _targetCircle.rectTransform.sizeDelta = _selectionCircle.rectTransform.sizeDelta;
        }
    }

    //private void SetInputEnabled(bool enabled)
    //{
    //    _raycastBlocker.gameObject.SetActive(!enabled);
    //}

    private void TranslateCamera(Vector2 screenDelta)
    {
        Camera camera = Camera.main;
        Vector3 translation = -camera.ScreenToWorldTranslation(screenDelta);
        //translation.x = 0f;
        //translation.y = 0f;
        camera.transform.Translate(1f * translation, Space.World);
    }

    private IEnumerator MovePlayerPawnRoutine(PlayerPawn pawn, Vector2Int destCell)
    {
        EventSystem currentEventSystem = EventSystem.current;
        currentEventSystem.enabled = false;
        yield return _board.MovePlayerPawnRoutine(pawn, destCell, () => {
            _movePawnRoutine = null;
            currentEventSystem.enabled = true;
        });
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_movePawnRoutine != null)
        {
            return;
        }
        if (_selectedPawn && eventData.pointerId == _selectedPawnPointerId)
        {
            //Debug.Log(GetType() + ".OnDrag: " + eventData.position);
            HideImage(_targetCircle);
            Vector3 worldPoint = _tilemap.GetTouchRayIntersection(Camera.main, eventData.position);
            Vector2Int pawnCell = _tilemap.WorldToCell(_selectedPawn.transform.position);
            Vector2Int destCell = _tilemap.WorldToCell(worldPoint);
            //Debug.Log(GetType() + ".OnDrag: " + _selectedCell + "->" + destCell + " " + _tilemap.Tilemap.cellSize + " " + _tilemap.Tilemap.cellBounds.size);
            if (destCell != pawnCell)
            {
                Vector2 ray = worldPoint - _selectedPawn.transform.position;
                float rayAngle = Vector2.SignedAngle(ray, Vector2.up);
                int raySign = Math.Sign(rayAngle);
                float absRayAngle = Mathf.Abs(rayAngle);
                float deltaAngle = 45f;
                Vector2Int[] ngbrsDeltaXY = new Vector2Int[] {
                    new Vector2Int(0, 1) , new Vector2Int(raySign, 1) , new Vector2Int(raySign, 0), new Vector2Int(raySign, -1), new Vector2Int(0, -1)
                };
                int ngbrIndex = (int)((absRayAngle + deltaAngle / 2f) / deltaAngle);
                ngbrIndex = Mathf.Min(ngbrIndex, ngbrsDeltaXY.Length - 1);
                Vector2Int deltaXY = ngbrsDeltaXY[ngbrIndex];
                destCell = pawnCell + deltaXY;
                if (!_tilemap.GetTile(destCell).Content)
                {
                    Vector3 destCellCenter = _tilemap.GetCellCenterWorld(destCell);
                    ShowImageAtPosition(_targetCircle, destCellCenter);
                }
            }
        }
        else if (!_selectedPawn)
        {
            TranslateCamera(eventData.delta);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log(GetType() + ".OnEndDrag");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_movePawnRoutine == null)
        {
            Vector3 worldPoint = _tilemap.GetTouchRayIntersection(Camera.main, eventData.position);
            //Debug.Log(GetType() + ".OnPonterDown: " + eventData.position + Input.mousePosition + worldPoint);
            BoardTile tile = _tilemap.GetTile(worldPoint, out Vector2Int cell);
            if (tile)
            {
                if (tile.Content && tile.Content is PlayerPawn)
                {
                    _selectedPawn = tile.Content as PlayerPawn;
                    _selectedPawnPointerId = eventData.pointerId;
                    HideImage(_targetCircle);
                    ShowImageAtPosition(_selectionCircle, _selectedPawn.transform.position);
                }
                Debug.Log(GetType() + ".OnPonterDown: cell:" + cell + " pointerId: " + eventData.pointerId);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_selectedPawn && _selectedPawnPointerId == eventData.pointerId)
        {
            if (_targetCircle.gameObject.activeSelf)
            {
                Vector3 destCellPos = Camera.main.ScreenToWorldPoint(_targetCircle.transform.position);
                Vector2Int destCell = _tilemap.WorldToCell(destCellPos);
                _movePawnRoutine = StartCoroutine(MovePlayerPawnRoutine(_selectedPawn, destCell));
            }
            HideImage(_targetCircle);
            HideImage(_selectionCircle);
            _selectedPawn = null;
            _selectedPawnPointerId = int.MinValue;
        }
    }

    private void HideImage(Image image)
    {
        image.gameObject.SetActive(false);
    }

    private void ShowImageAtPosition(Image image, Vector3 worldPoint)
    {
        image.gameObject.SetActive(true);
        image.transform.position = Camera.main.WorldToScreenPoint(worldPoint);
    }
}
