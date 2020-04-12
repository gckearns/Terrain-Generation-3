using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMap2D : NoiseMap
{
    public float[,] values { get => _values; }
    private float[,] _values;

    public NoiseMap2D(int numEdgeVertices, float edgeSize, NoiseMod mod) : base(numEdgeVertices, edgeSize, mod)
    {
        _values = new float[_resolution, _resolution];
    }

    //public override float ProcessCoordinate(int x, int y)
    //{
    //    _values[x, y] = Mathf.Clamp01((float)(EvaluateNoise(x, y, null) * rescale) + 0.5f) * mod.scale + mod.bias;
    //    UpdateMinMaxValues(_values[x, y]);
    //    return _values[x, y];
    //}

    //public override void Generate()
    //{
    //    //_values = new float[_numEdgeVertices, _numEdgeVertices];
    //    for (int y = 0; y < _numEdgeVertices; y++)
    //    {
    //        for (int x = 0; x < _numEdgeVertices; x++)
    //        {
    //            _values[x, y] = Mathf.Clamp01((float)(EvaluateNoise(x, y, null) * rescale) + 0.5f) * mod.scale + mod.bias ;
    //            UpdateMinMaxValues(_values[x, y]);
    //        }
    //    }
    //}
}
