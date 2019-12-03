using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MustHave;
using MustHave.Utilities;
using System;
using UnityEngine.UI;

public class BoardTouchHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private BoardTilemap _tilemap = default;
    [SerializeField] private Image _raycastBlocker = default;
    [SerializeField] private Image _selectionCircle = default;
    [SerializeField] private Image _targetCircle = default;

    //[SerializeField] private Transform _marker = default;

    private Board _board = default;
    private Transform _selectedPawnTransform = default;
    private Vector2Int _selectedCell = default;
    private Coroutine _movePawnRoutine = default;

    private void Awake()
    {
        _board = _tilemap.GetComponentInParent<Board>();
    }

    private Vector3 GetTouchRayIntersectionWithBoard(Vector3 touchPos)
    {
        Camera camera = Camera.main;
        Vector3 worldPoint;
        if (camera.orthographic && camera.transform.rotation == Quaternion.identity)
        {
            worldPoint = camera.ScreenToWorldPoint(touchPos);
        }
        else
        {
            Maths.GetTouchRayIntersectionWithPlane(camera, touchPos, -_tilemap.transform.forward, _tilemap.transform.position, out worldPoint);
        }
        return worldPoint;
    }

    private void SetInputEnabled(bool enabled)
    {
        _raycastBlocker.gameObject.SetActive(!enabled);
    }

    private void TranslateCamera(Vector2 screenDelta)
    {
        Camera camera = Camera.main;
        Vector3 translation = -camera.ScreenToWorldTranslation(screenDelta);
        camera.transform.Translate(1f * translation, Space.Self);
    }

    private IEnumerator MovePawnRoutine(Transform pawnTransform, Vector2Int destCell)
    {
        SetInputEnabled(false);
        yield return _board.MovePawnRoutine(pawnTransform, destCell, () => {
            _selectedCell = destCell;
            _board.MoveEnemyPawn(() => {
                _movePawnRoutine = null;
                SetInputEnabled(true);
            });
        });
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        _targetCircle.gameObject.SetActive(false);
        if (_movePawnRoutine != null)
        {
            return;
        }
        if (_selectedPawnTransform)
        {
            //Debug.Log(GetType() + ".OnDrag: " + eventData.position);
            Vector3 worldPoint = GetTouchRayIntersectionWithBoard(eventData.position);
            Vector2Int destCell = _tilemap.WorldToCell(worldPoint);

            if (destCell != _selectedCell &&
                Mathf.Abs(destCell.x - _selectedCell.x) <= 1 && Mathf.Abs(destCell.y - _selectedCell.y) <= 1 &&
                !_tilemap.GetTile(destCell).Content)
            {
                Vector3 cellSize = _tilemap.Tilemap.cellSize;
                Vector3 destCellCenter = _tilemap.GetCellCenterWorld(destCell);
                if (Mathf.Abs(worldPoint.x - destCellCenter.x) < cellSize.x * 0.4f &&
                    Mathf.Abs(worldPoint.y - destCellCenter.y) < cellSize.y * 0.4f)
                {
                    //Debug.Log(GetType() + ".OnDrag: " + _selectedCell + "->" + destCell + " " + _tilemap.Tilemap.cellSize + " " + _tilemap.Tilemap.cellBounds.size);
                    ShowImageAtPosition(_targetCircle, destCellCenter);
                }
            }
        }
        else
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
            Vector3 worldPoint = GetTouchRayIntersectionWithBoard(eventData.position);
            //Debug.Log(GetType() + ".OnPonterDown: " + eventData.position + Input.mousePosition + worldPoint);
            BoardTile tile = _tilemap.GetTile(worldPoint, out Vector2Int cell);
            if (tile)
            {
                if (tile.Content && tile.Content.GetComponent<Pawn>())
                {
                    _selectedPawnTransform = tile.Content;
                    ShowImageAtPosition(_selectionCircle, _selectedPawnTransform.position);
                }
                _selectedCell = cell;
                Debug.Log(GetType() + ".OnPonterDown: " + _selectedCell);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_selectedPawnTransform && _targetCircle.gameObject.activeSelf)
        {
            Vector3 destCellPos = Camera.main.ScreenToWorldPoint(_targetCircle.transform.position);
            Vector2Int destCell = _tilemap.WorldToCell(destCellPos);
            _movePawnRoutine = StartCoroutine(MovePawnRoutine(_selectedPawnTransform, destCell));
        }
        _targetCircle.gameObject.SetActive(false);
        _selectionCircle.gameObject.SetActive(false);
        _selectedPawnTransform = null;
    }

    private void ShowImageAtPosition(Image image, Vector3 worldPoint)
    {
        image.gameObject.SetActive(true);
        image.transform.position = Camera.main.WorldToScreenPoint(worldPoint);
    }
}
