using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Preview : MonoBehaviour
{
    [Header("Chunk Settings")]
    public bool enableChunkedMode;
    public Vector3 chunkOffset;
    [Range(1, 127)]
    public float chunkSize;
    public float maxViewDistance;
    public float viewerDeltaRequiredToUpdate;
    public float[] lodMaxDistances;
    public GameObject viewer;
    public TerrainChunkManager terrainChunkManager;

    [Header("Noise Settings")]
    public bool useNoise;
    public Vector3 noiseOffset;
    public NoiseMod noiseSettings;
    public bool showMinMax;
    public int extraIterations;
    NoiseMap noiseMap;

    [Header("Mesh Settings")]
    public PreviewMode previewMode;
    public bool useMesh;
    public Vector3 meshOffset;
    /// <summary>Diameter of sphere (without noise).</summary>
    [Range(1, 1000)]
    public float diameter = 5;
    public bool useMarchingCubes;
    public PreviewMesh previewMesh;

    /// <summary>Number of vertices per axis.</summary>
    [Range(2,128)]
    public int vertexResolution = 1;

    [Header("Render Settings")]
    public bool useTexture;
    public Material material;
    [Range(1,2048)]
    public int textureResolution;
    public bool matchMeshResolution;

    public enum PreviewMode
    {
        Noise,
        Sphere
    }

    public void GeneratePreview()
    {
        noiseMap = new NoiseMap(vertexResolution, chunkSize, useNoise ? noiseSettings : NoiseMod.flat);
        if (useMesh)
        {
            GetMesh();
            previewMesh.transform.position = chunkOffset;
        }
        previewMesh.meshRenderer.sharedMaterial = material;
        if (useNoise)
        {
            if (showMinMax)
            {
                ShowNoiseStatistics();
            }
            if (useTexture)
            {
                material = Resources.Load<Material>("Materials/NoiseMap");
                material.mainTexture = GetNoiseTexture(noiseMap);
            }
        }
    }

    void OnValidate()
    {
        textureResolution = matchMeshResolution ? vertexResolution : textureResolution;
        GeneratePreview();
    }

    void GetMesh()
    {
        Mesh mesh = previewMesh.meshFilter.sharedMesh;
        mesh = mesh == null ? new Mesh() : mesh;
        ChunkMesh chunkMesh = new ChunkMesh(vertexResolution, chunkSize, ref mesh);
        chunkMesh.SetNoise(noiseSettings, noiseOffset);
        if (useMarchingCubes)
        {
            noiseMap.SetOffset(noiseOffset);
            switch (previewMode)
            {
                case PreviewMode.Noise:
                    chunkMesh.GenerateNoiseMesh(noiseOffset, chunkOffset);
                    break;
                case PreviewMode.Sphere:
                    chunkMesh.GenerateSphereMesh(diameter / 2, chunkOffset);
                    break;
                default:
                    break;
            }
        }
        else
        {
            chunkMesh.SetNoise(useNoise ? noiseSettings : NoiseMod.flat, noiseOffset);
            chunkMesh.useNoise = useNoise;
            chunkMesh.GenerateSquareMesh(chunkOffset);
            noiseMap = chunkMesh.noiseMap;
        }
    }

    Texture2D GetNoiseTexture(NoiseMap noiseMap) //move this?
    {
        Texture2D texture = (Texture2D)material.mainTexture;
        if (texture == null || texture.width != textureResolution)
        {
            texture = new Texture2D(textureResolution, textureResolution);
        }

        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                float t = Mathf.InverseLerp(noiseMap.minValue, noiseMap.maxValue, noiseMap.GetNoiseValue(x, y));
                texture.SetPixel(x, y, Color.Lerp(Color.black, Color.white, t));
            }
        }
        //texture.filterMode = FilterMode.Trilinear;
        texture.Apply();
        return texture;
    }

    void ShowNoiseStatistics()
    {
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;
        float avgMin = 0;
        float avgMax = 0;
        noiseMap.Generate();
        avgMin += noiseMap.minValue;
        avgMax += noiseMap.maxValue;
        if (noiseMap.minValue < minVal) minVal = noiseMap.minValue;
        if (noiseMap.maxValue > maxVal) maxVal = noiseMap.maxValue;
        for (int i = 0; i < extraIterations; i++)
        {
            noiseMap.Generate(new Vector3(noiseOffset.x + i * diameter, noiseOffset.z));
            avgMin += noiseMap.minValue;
            avgMax += noiseMap.maxValue;
            if (noiseMap.minValue < minVal) minVal = noiseMap.minValue;
            if (noiseMap.maxValue > maxVal) maxVal = noiseMap.maxValue;
            noiseMap.Generate(new Vector3(noiseOffset.x, noiseOffset.z + i * diameter));
            if (noiseMap.minValue < minVal) minVal = noiseMap.minValue;
            if (noiseMap.maxValue > maxVal) maxVal = noiseMap.maxValue;
            avgMin += noiseMap.minValue;
            avgMax += noiseMap.maxValue;
        }
        Debug.LogFormat("Noise Values: Min: {0}, Max: {1}, Avg Min: {2}, Avg Max: {3}", minVal, maxVal, avgMin / (1 + extraIterations * 2), avgMax / (1 + extraIterations * 2));
        Debug.LogFormat("Noise Range: {0}", noiseMap.trueAmplitude);
    }

    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    void TimerStart()
    {
        stopwatch.Start();
    }

    void TimerStop()
    {
        stopwatch.Stop();
        Debug.LogFormat("Ran in {0} secs.\n",
        (double)(stopwatch.ElapsedMilliseconds / 1000f));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(chunkOffset + (Vector3.one * chunkSize) / 2, new Vector3(chunkSize, chunkSize, chunkSize));
    }
}
