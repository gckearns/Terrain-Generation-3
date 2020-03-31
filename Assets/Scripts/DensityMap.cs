using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityMap
{
    public float [,,] map { get => _map; }
    float[,,] _map;

    public NoiseMap2D noiseMap { get => _noiseMap; }
    NoiseMap2D _noiseMap;

    public DensityMap(NoiseMap noiseMap)
    {
        _noiseMap = (NoiseMap2D)noiseMap;
        _map = new float[_noiseMap.numEdgeVertices, _noiseMap.numEdgeVertices, _noiseMap.numEdgeVertices];
        for (int z = 0; z < _noiseMap.numEdgeVertices; z++)
        {
            for (int y = 0; y < _noiseMap.numEdgeVertices; y++)
            {
                for (int x = 0; x < _noiseMap.numEdgeVertices; x++)
                {
                    _map[x, y, z] = (1 - _noiseMap.values[x, z]) - ((_noiseMap.numEdgeVertices - y) / (_noiseMap.numEdgeVertices - 1));
                    //Debug.LogFormat("Coordinates: (X:{0:F2}, Z:{1:F2}) NoiseValue:{2:F5} // Y:{3:F2} Density:{4:F5}", x, z, _noiseMap.values[x, z], y, _map[x,y,z]);
                }
            }
        }
    }

    public float[,,] Get3D()
    {
        return null;
    }
}
