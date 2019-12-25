using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelsMap : MonoBehaviour
{
    [SerializeField] private LevelsTilemap _tilemap = default;

    public LevelsTilemap Tilemap { get => _tilemap; }

    private void Awake()
    {
    }
}
