using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Preview : MonoBehaviour
{
    public enum MarchingCubesVersion
    {
        OriginalMC,
        NewMC
    }

    [Header("Chunk Settings")]
    public bool enableChunkedMode;
    public Vector3 chunkOffset;
    [Range(1, 64)]
    public float chunkSize;
    public GameObject viewer;

    [Header("Noise Settings")]
    public bool useNoise;
    public Vector3 noiseOffset;
    public NoiseMod noiseSettings;
    public bool showMinMax;
    public int extraIterations;
    NoiseMap2D noiseMap2D;
    NoiseMap3D noiseMap3D;


    /// <summary>Distance from center of mesh to the outside along a relevant axis.</summary>
    [Header("Mesh Settings")]
    public bool useMesh;
    public Vector3 meshOffset;
    [Range(1, 100)]
    public float diameter = 5;
    public bool useMarchingCubes;
    public MarchingCubesVersion mcVersion;
    public PreviewMesh previewMesh;

    /// <summary>Quantity of vertices per axis.</summary>
    [Range(2,128)]
    public int vertexResolution = 1;

    [Header("Render Settings")]
    public bool useTexture;
    public Material material;
    [Range(1,2048)]
    public int textureResolution;
    public bool matchMeshResolution;

    public void GeneratePreview()
    {
        noiseMap2D = new NoiseMap2D(vertexResolution, diameter, useNoise ? noiseSettings : NoiseMod.flat);
        noiseMap2D.Generate(noiseOffset);
        if (useMesh)
        {
            GetMesh();
        }
        previewMesh.meshRenderer.sharedMaterial = material;
        if (useNoise)
        {
            if (showMinMax)
            {
                float minVal = float.MaxValue;
                float maxVal = float.MinValue;
                float avgMin = 0;
                float avgMax = 0;
                avgMin += noiseMap2D.minValue;
                avgMax += noiseMap2D.maxValue;
                if (noiseMap2D.minValue < minVal) minVal = noiseMap2D.minValue;
                if (noiseMap2D.maxValue > maxVal) maxVal = noiseMap2D.maxValue;
                for (int i = 0; i < extraIterations; i++)
                {
                    noiseMap2D.Generate(new Vector3(noiseOffset.x + i * diameter, noiseOffset.z));
                    avgMin += noiseMap2D.minValue;
                    avgMax += noiseMap2D.maxValue;
                    if (noiseMap2D.minValue < minVal) minVal = noiseMap2D.minValue;
                    if (noiseMap2D.maxValue > maxVal) maxVal = noiseMap2D.maxValue;
                    noiseMap2D.Generate(new Vector3(noiseOffset.x, noiseOffset.z + i * diameter));
                    if (noiseMap2D.minValue < minVal) minVal = noiseMap2D.minValue;
                    if (noiseMap2D.maxValue > maxVal) maxVal = noiseMap2D.maxValue;
                    avgMin += noiseMap2D.minValue;
                    avgMax += noiseMap2D.maxValue;
                }
                Debug.LogFormat("Noise Values- Min: {0}, Max: {1}, Avg Min: {2}, Avg Max: {3}", minVal, maxVal, avgMin / (1 + extraIterations * 2), avgMax / (1 + extraIterations * 2));
            }
            if (useTexture)
            {
                material = Resources.Load<Material>("Materials/NoiseMap");
                material.mainTexture = GetNoiseTexture(noiseMap2D.values);
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
        Vector2 size = new Vector2(diameter, diameter);
        Vector2Int vertices = new Vector2Int(vertexResolution, vertexResolution);
        Mesh mesh = previewMesh.meshFilter.sharedMesh;
        mesh = mesh == null ? new Mesh() : mesh;
        if (useNoise && useMarchingCubes)
        {
            DensityMap densityMap = new DensityMap(noiseMap2D);
            mesh = ChunkMesh.DensityMesh(ref mesh, vertexResolution, densityMap);
        }
        else if (useMarchingCubes)
        {
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();
            switch (mcVersion)
            {
                case MarchingCubesVersion.OriginalMC:
                    //mesh = ChunkMesh.OldMCMesh(ref mesh, diameter / 2, vertexResolution);
                    break;
                case MarchingCubesVersion.NewMC:
                    mesh = ChunkMesh.MyMCMesh(ref mesh, diameter / 2, vertexResolution, chunkSize, chunkOffset);
                    break;
                default:
                    break;
            }
            //stopwatch.Stop();
            //Debug.LogFormat("{1} Marching Cubes ran in {0} secs.\n",
            //(double)(stopwatch.ElapsedMilliseconds / 1000f), mcVersion.ToString());
        }
        else
        {
            ChunkMesh.GetSquareMesh(ref mesh, vertices, size, meshOffset, noiseMap2D);
        }
        
    }

    Texture2D GetNoiseTexture(float [,] noiseMap) //move this?
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
                texture.SetPixel(x, y, Color.Lerp(Color.black, Color.white, noiseMap[x, y]));
            }
        }
        texture.Apply();
        return texture;
    }




}
