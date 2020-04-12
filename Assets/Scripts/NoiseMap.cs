using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//sum of several coherent-noise functions of ever-increasing frequencies and ever-decreasing amplitudes. 
public class NoiseMap
{
    
    /// <summary>
    /// The human-estimated practical noise range from 0. Estimated 
    /// practical noise ranges from -n to +n with only 1 octave.
    /// </summary>
    private const float estimatedNoiseRange = 0.866f;
    /// <summary>
    /// Actual amplitude will be scaled to meet this, in attempt to clamp 
    /// final noise values between 0 and 1.
    /// </summary>
    private const float desiredAmplitude = 0.5f;
    /// <summary>
    /// Number of dimensions used in this NoiseMap. 
    /// Can be 2 or 3. (Default 3).
    /// </summary>
    protected int _numDimensions = 3;
    /// <summary>
    /// Number of sample points per axis.
    /// </summary>
    protected int _resolution;
    /// <summary>
    /// Highest value in this NoiseMap. (Values must be generated first.)
    /// </summary>
    protected float _maxValue = float.MinValue;
    /// <summary>
    /// Lowest value in this NoiseMap. (Values must be generated first.)
    /// </summary>
    protected float _minValue = float.MaxValue;
    protected NoiseMod _mod = NoiseMod.flat;
    protected OpenSimplexNoise noise = new OpenSimplexNoise();
    protected Vector3 _offset = Vector3.zero;

    private float _edgeSize;
    private float[] _noiseValues;

    /// <summary>
    /// Create a new <see cref="NoiseMap"/> with the given resolution and size, 
    /// using given noise modifiers.
    /// </summary>
    /// <param name="resolution">Number of sample points per axis.</param>
    /// <param name="edgeSize">Size per axis.</param>
    /// <param name="mod">Noise modifer parameters to use for this NoiseMap.</param>
    public NoiseMap(int resolution, float edgeSize, NoiseMod mod)
    {
        _resolution = resolution;
        _edgeSize = edgeSize;
        this._mod = mod;

        _noiseValues = new float[_resolution * _resolution * _resolution];
    }

    public float coordScale { get => _edgeSize / (_resolution - 1); }
    public float maxValue { get => _maxValue; }
    public float minValue { get => _minValue; }
    public NoiseMod mod { get => _mod; }
    /// <summary>
    /// Total practical noise range from 0 including octaves. Ranges from -n to +n.
    /// </summary>
    public float trueAmplitude { get => ((1 - Mathf.Pow(_mod.persistence, _mod.octaves)) / (1 - _mod.persistence)) * estimatedNoiseRange; }
    public Vector3 offset { get => _offset; }
    public float amplitudeScale { get => desiredAmplitude / trueAmplitude; }
    public int numEdgeVertices { get => _resolution; }
    public float edgeSize { get => _edgeSize; }

    public void Generate()
    {
        switch (_numDimensions)
        {
            case 2:
                for (int y = 0; y < _resolution; y++)
                {
                    for (int x = 0; x < _resolution; x++)
                    {
                        ProcessCoordinate(x, y);
                    }
                }
                break;
            case 3:
                for (int z = 0; z < _resolution; z++)
                {
                    for (int y = 0; y < _resolution; y++)
                    {
                        for (int x = 0; x < _resolution; x++)
                        {
                            ProcessCoordinate(x, y, z);
                        }
                    }
                }
                break;
            case 4:
                break;
            default:
                break;
        }
    }

    public void Generate(Vector3 offset)
    {
        SetOffset(offset);
        Generate();
    }

    public float GetNoiseValue(int x, int y) => GetNoiseValue(x, y, 0);

    public float GetNoiseValue(int x, int y, int z) => 
        _noiseValues[x + y * _resolution + z * _resolution * _resolution];

    public float ProcessCoordinate(int x, int y)
    {
        SetNoiseValue(x, y, Mathf.Clamp01((float)((EvaluateNoise(x, y, null) + trueAmplitude) * amplitudeScale)) * 
            _mod.scale + _mod.bias);
        UpdateMinMaxValues(GetNoiseValue(x, y));
        return GetNoiseValue(x, y);
    }

    public float ProcessCoordinate(int x, int y, int z)
    {
        SetNoiseValue(x, y, z, Mathf.Clamp01((float)(EvaluateNoise(x, y, z) * amplitudeScale) + 0.5f) * 
            _mod.scale + _mod.bias);
        UpdateMinMaxValues(GetNoiseValue(x, y, z));
        return GetNoiseValue(x, y, z);
    }

    public void SetNoiseValue(int x, int y, float value) => SetNoiseValue(x, y, 0, value);
    
    public void SetNoiseValue(int x, int y, int z, float value)
    {
        _noiseValues[x + y * _resolution + z * _resolution * _resolution] = value;
    }

    public void SetNumDimensions(int numDimensions)
    {
        this._numDimensions = numDimensions;
        ResetMinMaxValues();
    }

    public void SetOffset(Vector3 offset)
    {
        this._offset = offset;
        ResetMinMaxValues();
    }

    protected double EvaluateNoise(int x, int y, object z)
    {
        double noiseValue = 0;
        //amplitude is maximum value. an ampltude of n generates values between +n and -n
        float amplitude = 1f;
        float frequency = _mod.frequency;

        for (int o = 1; o <= _mod.octaves; o++)
        {
            float xSample = (x * coordScale + _offset.x) * frequency;
            float ySample = (y * coordScale + _offset.y) * frequency;

            double eval;
            if (z is int)
            {
                float zSample = ((int)z * coordScale + _offset.z) * frequency;
                eval = noise.eval(xSample, ySample, zSample);
            }
            else
            {
                eval = noise.eval(xSample, ySample);
            }
            noiseValue += (eval * amplitude);

            frequency *= _mod.lacunarity;
            amplitude *= _mod.persistence;
        }
        return noiseValue;
    }

    protected void ResetMinMaxValues()
    {
        _maxValue = float.MinValue;
        _minValue = float.MaxValue;
    }

    protected void UpdateMinMaxValues(float value)
    {
        if (value > _maxValue) _maxValue = value;
        if (value < _minValue) _minValue = value;
    }
}

[System.Serializable]
public struct NoiseMod
{
    /// <summary> 
    /// Octaves add a series of coherent-noise functions together. Each octave has, by 
    /// default, double the frequency and one-half the amplitude of the previous. 
    /// </summary>
    [Range(1, 30)]
    public int octaves;
    /// <summary>
    /// Multiplier that determines how quickly the amplitudes diminish for each 
    /// successive octave (0-1). (Default 0.5)
    /// </summary>
    [Range(0, 1)]
    public float persistence;
    /// <summary>
    /// Number of cycles per unit length. Periodic cycles of length 1/f (1-16) 
    /// (Default 1).
    /// </summary>
    [Range(0.0001f, 1)]
    public float frequency;
    /// <summary>
    /// Multiplier that determines how quickly the frequency increases for each successive octave. (Default 2).
    /// </summary>
    [Range(1, 4)]
    public float lacunarity;
    /// <summary>
    /// Multiplier for the final result which changes the overall amplitude.
    /// </summary>
    [Range(0, 300)]
    public float scale;
    /// <summary>
    /// Flat addition to the final value. Calculated after scale.
    /// </summary>
    public float bias;

    NoiseMod(int octaves, float persistence, float frequency, float lacunarity, float scale, float bias)
    {
        this.octaves = octaves;
        this.persistence = persistence;
        this.frequency = frequency;
        this.lacunarity = lacunarity;
        this.scale = scale;
        this.bias = bias;
    }

    public static NoiseMod flat { get => new NoiseMod(1, 0.5f, 1f, 2f, 0f, 0f); }
    public static NoiseMod basic { get => new NoiseMod(6, 0.5f, 0.2f, 2f, 1f, 0f); }
    public static NoiseMod tile { get => new NoiseMod(6, 0.5f, 0.2f, 2f, 1.153f, -0.0048f); }
    public static NoiseMod formation3D { get => new NoiseMod(3, 0.3f, 0.1f, 2f, 1f, -0.4f); }
}