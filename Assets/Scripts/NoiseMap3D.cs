using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMap3D : NoiseMap
{
    public float[,,] values { get => _values; }
    private float[,,] _values;

    public NoiseMap3D(int numEdgeVertices, float edgeSize, NoiseMod mod) : base(numEdgeVertices, edgeSize, mod) { }

    public override void Generate()
    {
        _values = new float[_numEdgeVertices, _numEdgeVertices, _numEdgeVertices];
        for (int z = 0; z < _numEdgeVertices; z++)
        {
            for (int y = 0; y < _numEdgeVertices; y++)
            {
                for (int x = 0; x < _numEdgeVertices; x++)
                {
                    _values[x, y, z] = Mathf.Clamp01((float)(GetNoiseValue(x, y, z) * rescale) + 0.5f) * mod.scale + mod.bias;
                    UpdateMinMaxValues(_values[x, y, z]);
                }
            }
        }
    }
}
