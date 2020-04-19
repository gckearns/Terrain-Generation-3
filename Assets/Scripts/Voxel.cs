using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Voxel
{
    private float _density;
    private Vector3 _position;

    public Voxel(Vector3 position, float density)
    {
        _position = position;
        _density = density;
    }

    public float density { get => _density; }
    public Vector3 position { get => _position; }
}
