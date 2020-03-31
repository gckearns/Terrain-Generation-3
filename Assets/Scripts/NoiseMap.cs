using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMap
{
    //sum of several coherent-noise functions of ever-increasing frequencies and ever-decreasing amplitudes. 

    //octaves are a series of coherent-noise functions that are added together. 
    //each octave has, by default, double the frequency and one-half the amplitude of the previous
    //max 30

    //amplitude is maximum value. an ampltude of n generates values between +n and -n
    //persistence is a multiplier that determines how quickly the amplitudes diminish for each successive octave (0-1) default .5

    //frequency is number of cycles per unit length. periodic cycles of length 1/f (1-16f) default start 1
    //lacunarity is a multiplier that determines how quickly the frequency increases for each successive octave. default 2

    //scale is a multiplier for the final result (changes overall amplitude)
    //bias is a flat addition or subtraction to the final result (do after scale)
    public float minValue { get => _minValue; }
    public float maxValue { get => _maxValue; }
    public float coordScale { get => _edgeSize / (_numEdgeVertices - 1); }
    public float noiseRange { get => ((1 - Mathf.Pow(0.5f, mod.octaves)) / 0.5f) * estimatedRange; }
    public float rescale { get => 0.5f / noiseRange; }
    public int numEdgeVertices { get => _numEdgeVertices; }
    public float edgeSize { get => _edgeSize; }

    protected float _minValue = float.MaxValue;
    protected float _maxValue = float.MinValue;
    protected int _numEdgeVertices;
    protected Vector3 offset = Vector3.zero;
    protected NoiseMod mod = NoiseMod.flat;

    protected OpenSimplexNoise noise = new OpenSimplexNoise();

    private float _edgeSize;
    private const float estimatedRange = 0.866f;

    public NoiseMap(int numEdgeVertices, float edgeSize, NoiseMod mod)
    {
        this._numEdgeVertices = numEdgeVertices;
        this._edgeSize = edgeSize;
        this.mod = mod;
    }

    protected NoiseMap() { }

    public virtual void Generate() { }

    public void Generate(Vector3 offset)
    {
        this.offset = offset;
        ResetMinMaxValues();
        Generate();
    }

    protected double GetNoiseValue(int x, int y, object z)
    {
        double noiseValue = 0;
        float amplitude = 1f;
        float frequency = mod.frequency;

        for (int o = 1; o <= mod.octaves; o++)
        {
            float xSample = (x * coordScale + offset.x) * frequency;
            float ySample = (y * coordScale + offset.y) * frequency;

            double eval;
            if (z is int)
            {
                float zSample = ((int)z * coordScale + offset.z) * frequency;
                eval = noise.eval(xSample, ySample, zSample);
            }
            else
            {
                eval = noise.eval(xSample, ySample);
            }
            noiseValue += (eval * amplitude);

            frequency *= mod.lacunarity;
            amplitude *= mod.persistence;
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
    [Range(1, 30)]
    public int octaves;
    [Range(0, 1)]
    public float persistence;
    [Range(0, 15)]
    public float frequency;
    [Range(1, 4)]
    public float lacunarity;
    [Range(0, 300)]
    public float scale;

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
}