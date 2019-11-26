using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MustHave.Utilities;
using System;

public class BoardTouchHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private BoardTilemap _tilemap = default;

    private Transform _selectedPawnTransform = default;
    private Vector3Int _selectedCell = default;
    private Coroutine _shiftPawnRoutine = default;
    private void Awake()
    {
    }


    private IEnumerator ShiftPawnRoutine(Transform pawnTransform, Vector3Int initCell, Vector3Int destCell, Action onEnd)
    {
        //EventSystem currentEventSystem = EventSystem.current;
        //currentEventSystem.enabled = false;

        _tilemap.GetTile(initCell).Content = null;

        Vector3 begPos = pawnTransform.position;
        Vector3 endPos = _tilemap.Tilemap.GetCellCenterWorld(destCell);

        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            pawnTransform.position = Vector3.Lerp(begPos, endPos, shift);
        });
        pawnTransform.position = endPos;

        _tilemap.GetTile(destCell).Content = pawnTransform;

        _selectedCell = destCell;

        //EventSystem.current = currentEventSystem;
        //currentEventSystem.enabled = true;

        _shiftPawnRoutine = null;
        onEnd?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_selectedPawnTransform && _shiftPawnRoutine == null)
        {
            //Debug.Log(GetType() + ".OnDrag");
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
            Vector3Int destCell = _tilemap.Tilemap.WorldToCell(worldPoint);

            if (destCell != _selectedCell &&
                Mathf.Abs(destCell.x - _selectedCell.x) <= 1 && Mathf.Abs(destCell.y - _selectedCell.y) <= 1 &&
                !_tilemap.GetTile(destCell).Content)
            {
                Vector3 cellSize = _tilemap.Tilemap.cellSize;
                Vector3 destCellCenter = _tilemap.Tilemap.GetCellCenterWorld(destCell);
                if (Mathf.Abs(worldPoint.x - destCellCenter.x) < cellSize.x * 0.325f &&
                    Mathf.Abs(worldPoint.y - destCellCenter.y) < cellSize.y * 0.325f)
                {
                    Debug.Log(GetType() + ".OnDrag: " + _selectedCell + "->" + destCell + " " + (_tilemap.Tilemap.cellSize + _tilemap.Tilemap.cellGap) + " " + _tilemap.Tilemap.cellBounds.size);
                    _shiftPawnRoutine = StartCoroutine(ShiftPawnRoutine(_selectedPawnTransform, _selectedCell, destCell, () => {
                        OnDrag(eventData);
                    }));
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log(GetType() + ".OnEndDrag");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_shiftPawnRoutine == null)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(eventData.position);
            //Debug.Log(GetType() + ".OnPonterDown: " + eventData.position + Input.mousePosition + worldPoint);
            BoardTile tile = _tilemap.GetTile(worldPoint, out Vector3Int cell);
            if (tile)
            {
                _selectedPawnTransform = tile.Content;
                _selectedCell = cell;
                Debug.Log(GetType() + ".OnPonterDown: tile: " + _selectedCell);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _selectedPawnTransform = null;
    }
}
