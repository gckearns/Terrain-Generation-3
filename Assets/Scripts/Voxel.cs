using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Voxel
{
    private float _density;

    public Voxel(float density)
    {
        _density = density;
    }

    public float density { get => _density; }
}
