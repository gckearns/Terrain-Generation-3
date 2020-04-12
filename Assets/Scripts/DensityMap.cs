using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityMap
{
    private float[] _densityValues;
    private bool _isChunk;
    private bool _useNoise;
    private DensityMapMode _mode = DensityMapMode.Noise;
    private NoiseMap _noiseMap;
    private int _numEdgeVertices;
    private Vector3 _offset = Vector3.zero;
    private float _radius;
    private float _chunkSize;
    private Vector3 _sphereCenter;

    public DensityMap(int resolution)
    {
        _numEdgeVertices = resolution;
        _densityValues = new float[_numEdgeVertices * _numEdgeVertices * _numEdgeVertices];
    }

    public enum DensityMapMode
    {
        HeightMap,
        Noise,
        Sphere
    }

    public NoiseMap noiseMap { get => _noiseMap; }
    public Vector3 offset { get => _offset; }
    public Vector3Int resolution { get => new Vector3Int(_numEdgeVertices, _numEdgeVertices, _numEdgeVertices); }
    public float scale { get => _isChunk ? (_numEdgeVertices - 1) / _chunkSize : 1; }
    public float[] values { get => _densityValues; }

    public void Generate()
    {
        for (int z = 0; z < _numEdgeVertices; z++)
        {
            for (int y = 0; y < _numEdgeVertices; y++)
            {
                for (int x = 0; x < _numEdgeVertices; x++)
                {
                    ProcessCoordinate(x, y, z);
                }
            }
        }
    }

    public float GetDensityValue(int x, int y, int z) => _densityValues[x + y * _numEdgeVertices + z * _numEdgeVertices * _numEdgeVertices];

    public float ProcessCoordinate(int x, int y, int z)
    {
        switch (_mode)
        {
            case DensityMapMode.Noise:
                SetDensityValue(x, y, z, _noiseMap.GetNoiseValue(x, y, z));
                break;
            case DensityMapMode.Sphere:
                SetDensityValue(x, y, z, (EvalSphereDensity(x, y, z)));
                break;
            default:
                break;
        }
        return GetDensityValue(x, y, z);
    }

    public void SetDensityValue(int x, int y, int z, float value)
    {
        _densityValues[x + y * _numEdgeVertices + z * _numEdgeVertices * _numEdgeVertices] = value;
    }

    public void SetChunkInfo(Vector3 chunkOffset, float chunkSize)
    {
        _offset = chunkOffset;
        _chunkSize = chunkSize;
        _isChunk = true;
    }

    public void SetMode3DNoise(NoiseMap noiseMap)
    {
        _noiseMap = noiseMap;
        _mode = DensityMapMode.Noise;
        _useNoise = true;
    }

    public void SetModeSphere(float radius, Vector3 sphereCenter)
    {
        _radius = radius;
        _mode = DensityMapMode.Sphere;
        _sphereCenter = sphereCenter;
    }

    public void SetNoise(NoiseMap noiseMap)
    {
        _noiseMap = noiseMap;
        _useNoise = true;
    }

    protected float EvalSphereDensity(int x, int y, int z)
    {
        float i = x + (_offset.x - _sphereCenter.x) * scale; // remove:" - _radius" to center the sphere on 0,0,0
        float j = y + (_offset.y - _sphereCenter.y) * scale;
        float k = z + (_offset.z - _sphereCenter.z) * scale;
        float rScaled = _radius * scale;
        float noiseMod = 0;
        if (_useNoise)
        {
            Vector3 samplePoint = new Vector3(i, j, k);
            float distFromCenterSqr = (samplePoint - _sphereCenter).sqrMagnitude;
            float distFromRadiusSqr = distFromCenterSqr - (_radius * _radius);
            noiseMod = _noiseMap.GetNoiseValue(x, y, z);
            noiseMod = noiseMod * Mathf.Sqrt(distFromRadiusSqr * _radius);
            //noiseMod *= rScaled;
            //noiseMod -= (noiseMap.mod.scale / noiseMap.trueAmplitude) * rScaled;
        }
        return (i * i) + (j * j) + (k * k) - (rScaled * rScaled) - noiseMod;
    }
}
