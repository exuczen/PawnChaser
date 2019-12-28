using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct Layer
{
    public static readonly int LevelPointers = LayerMask.NameToLayer("LevelPointers");
    public static readonly int LevelPointersMask = LayerMask.GetMask("LevelPointers");
}
