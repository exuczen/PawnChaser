﻿using System.Collections;
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
    //[SerializeField] private Transform _marker = default;

    private Board _board = default;
    private Transform _selectedPawnTransform = default;
    private Vector2Int _selectedCell = default;
    private Coroutine _shiftPawnRoutine = default;

    private void Awake()
    {
        _board = _tilemap.GetComponentInParent<Board>();
    }

    private IEnumerator ShiftPawnRoutine(Transform pawnTransform, Vector2Int initCell, Vector2Int destCell, Action onEnd)
    {
        //EventSystem currentEventSystem = EventSystem.current;
        //currentEventSystem.enabled = false;
        SetInputEnabled(false);

        Vector3 begPos = pawnTransform.position;
        Vector3 endPos = _tilemap.GetCellCenterWorld(destCell);

        float duration = 0.3f;
        yield return CoroutineUtils.UpdateRoutine(duration, (elapsedTime, transition) => {
            float shift = Maths.GetTransition(TransitionType.COS_IN_PI_RANGE, transition);
            pawnTransform.position = Vector3.Lerp(begPos, endPos, shift);
        });
        pawnTransform.position = endPos;

        _tilemap.GetTile(initCell).Content = null;
        _tilemap.GetTile(destCell).Content = pawnTransform;

        _selectedCell = destCell;

        //EventSystem.current = currentEventSystem;
        //currentEventSystem.enabled = true;
        SetInputEnabled(true);


        _shiftPawnRoutine = null;
        onEnd?.Invoke();
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
        camera.transform.Translate(translation, Space.Self);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData, out bool pawnDragged)
    {
        pawnDragged = false;
        if (_shiftPawnRoutine != null)
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
                if (Mathf.Abs(worldPoint.x - destCellCenter.x) < cellSize.x * 0.325f &&
                    Mathf.Abs(worldPoint.y - destCellCenter.y) < cellSize.y * 0.325f)
                {
                    //Debug.Log(GetType() + ".OnDrag: " + _selectedCell + "->" + destCell + " " + _tilemap.Tilemap.cellSize + " " + _tilemap.Tilemap.cellBounds.size);
                    pawnDragged = true;
                    _shiftPawnRoutine = StartCoroutine(ShiftPawnRoutine(_selectedPawnTransform, _selectedCell, destCell, () => {
                        OnDrag(eventData, out bool pawnDragged2);
                        if (!pawnDragged2)
                        {
                            _board.FindEnemyPathToTarget();
                        }
                    }));
                }
            }
        }
        else
        {
            TranslateCamera(eventData.delta);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDrag(eventData, out _);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log(GetType() + ".OnEndDrag");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_shiftPawnRoutine == null)
        {
            Vector3 worldPoint = GetTouchRayIntersectionWithBoard(eventData.position);
            //Debug.Log(GetType() + ".OnPonterDown: " + eventData.position + Input.mousePosition + worldPoint);
            BoardTile tile = _tilemap.GetTile(worldPoint, out Vector2Int cell);
            if (tile)
            {
                _selectedPawnTransform = tile.Content;
                _selectedCell = cell;
                Debug.Log(GetType() + ".OnPonterDown: " + _selectedCell);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _selectedPawnTransform = null;
    }
}
