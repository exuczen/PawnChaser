using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelPointer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite = default;
    [SerializeField] private TextMesh _levelTextMesh = default;

    private Collider _pointerCollider = default;

    public SpriteRenderer Sprite { get => _sprite; }
    public TextMesh LevelTextMesh { get => _levelTextMesh; }
    public Collider Collider { get => _pointerCollider ?? (_pointerCollider = _sprite.GetComponent<Collider>()); }
    public int Level { get => int.TryParse(_levelTextMesh.text, out int level) ? level : 0; }
}
