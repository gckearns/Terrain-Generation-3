using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    public ChunkMesh chunkMesh;
    public bool isVisible = true;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Vector3 offset;
    public Mesh mesh;

    public Preview preview { get => GetComponentInParent<Preview>(); }

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        offset = transform.position;
        meshFilter.sharedMesh = mesh ?? new Mesh();
        mesh = meshFilter.sharedMesh;
        chunkMesh = new ChunkMesh(preview.vertexResolution, preview.chunkSize, ref mesh);
        chunkMesh.SetNoise(preview.noiseSettings, preview.noiseOffset);
        //chunkMesh.GenerateSquareMesh(offset);
        chunkMesh.GenerateSphereMesh(preview.diameter / 2, offset);

        meshRenderer.material.mainTexture = GetNoiseTexture(chunkMesh.noiseMap);
    }

    // Update is called once per frame
    void Update()
    {

    }

    Texture2D GetNoiseTexture(NoiseMap noiseMap) //move this?
    {
        Texture2D texture = (Texture2D)meshRenderer.material.mainTexture;
        if (texture == null || texture.width != preview.textureResolution)
        {
            texture = new Texture2D(preview.textureResolution, preview.textureResolution);
        }

        for (int y = 0; y < preview.textureResolution; y++)
        {
            for (int x = 0; x < preview.textureResolution; x++)
            {
                float t = Mathf.InverseLerp(0 + noiseMap.mod.bias, noiseMap.mod.scale + noiseMap.mod.bias, noiseMap.GetNoiseValue(x, y));
                //texture.SetPixel(x, y, Color.Lerp(Color.black, Color.white, t));
                texture.SetPixel(x, y, Color.gray);
            }
        }
        //texture.filterMode = FilterMode.Trilinear;
        texture.Apply();
        return texture;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(offset + (Vector3.one * preview.chunkSize) / 2, new Vector3(preview.chunkSize, preview.chunkSize, preview.chunkSize));
    }
}
