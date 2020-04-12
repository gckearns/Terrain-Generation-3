using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMap3D : NoiseMap
{
    public float[,,] values { get => _values; }
    private float[,,] _values;
       
    public NoiseMap3D(int numEdgeVertices, float edgeSize, NoiseMod mod) : base(numEdgeVertices, edgeSize, mod)
    {
        _values = new float[_resolution, _resolution, _resolution];
    }

    //public override float ProcessCoordinate(int x, int y, int z)
    //{
    //    _values[x, y, z] = Mathf.Clamp01((float)(EvaluateNoise(x, y, z) * rescale) + 0.5f) * mod.scale + mod.bias;
    //    UpdateMinMaxValues(_values[x, y, z]);
    //    return _values[x, y, z];
    //}

    //public override void Generate()
    //{
    //    for (int z = 0; z < _numEdgeVertices; z++)
    //    {
    //        for (int y = 0; y < _numEdgeVertices; y++)
    //        {
    //            for (int x = 0; x < _numEdgeVertices; x++)
    //            {
    //                _values[x, y, z] = Mathf.Clamp01((float)(EvaluateNoise(x, y, z) * rescale) + 0.5f) * mod.scale + mod.bias;
    //                UpdateMinMaxValues(_values[x, y, z]);
    //            }
    //        }
    //    }
    //}
}
