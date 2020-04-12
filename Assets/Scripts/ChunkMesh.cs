using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
    int _verticesPerEdge;
    float _edgeSize;
    bool _useNoise;
    bool _useDensity;
    Vector3 _chunkOffset; //optional?
    Vector3 _noiseOffset; //optional?

    NoiseMod _noiseMod;
    NoiseMap _noiseMap;

    Mesh _mesh;
    
    public ChunkMesh(int verticesPerEdge, float edgeSize, ref Mesh mesh)
    {
        if (verticesPerEdge < 2)
        {
            throw new System.ArgumentOutOfRangeException("verticesPerEdge", verticesPerEdge, "Must be 2 or greater.");
        }
        if (edgeSize < 0)
        {
            throw new System.ArgumentOutOfRangeException("edgeSize", edgeSize, "Must be 0 or greater.");
        }
        _verticesPerEdge = verticesPerEdge;
        _edgeSize = edgeSize;
        _mesh = mesh ?? new Mesh();
    }

    public NoiseMap noiseMap { get => _noiseMap; set => _noiseMap = value; }
    public bool useDensity { get => _useDensity; set => _useDensity = value; }
    public bool useNoise { get => _useNoise; set => _useNoise = value; }

    protected Vector3Int vertexCount { get => new Vector3Int(_verticesPerEdge, _verticesPerEdge, _verticesPerEdge); }
    protected Vector2Int quadCount { get => new Vector2Int(_verticesPerEdge - 1, _verticesPerEdge - 1); }
    protected float scale { get => (_verticesPerEdge - 1) / _edgeSize; }

    public void SetNoise(NoiseMod noiseMod, Vector3 noiseOffset)
    {
        _noiseMod = noiseMod;
        _noiseOffset = noiseOffset;
        _useNoise = true;
    }

    public void GenerateSquareMesh(Vector3 chunkOffset)
    {
        _chunkOffset = chunkOffset;

        _noiseMap = new NoiseMap(_verticesPerEdge, _edgeSize, _noiseMod);
        _noiseMap.SetNumDimensions(2);
        Vector3 trueNoiseOffset = new Vector3(_noiseOffset.x + _chunkOffset.x, _noiseOffset.y + _chunkOffset.z);
        _noiseMap.SetOffset(trueNoiseOffset);
        //noiseMap.Generate(offset);

        // Initialize array variables to hold mesh data
        Vector3[] newVertices = new Vector3[_verticesPerEdge * _verticesPerEdge];
        Vector2[] newUvs = new Vector2[newVertices.Length];   // UV length always equals vertices length
        int[] newTriangles = new int[(quadCount.x * quadCount.y) * 6];   // Resolution represents how many quad-sides along an axis.
        // Loop through x,y coordinates to create vertices and uv coordinates
        int i = 0;
        for (int y = 0; y < _verticesPerEdge; y++)
        {
            for (int x = 0; x < _verticesPerEdge; x++)
            {
                newVertices[i] = new Vector3(x * (_edgeSize / quadCount.x), _noiseMap.ProcessCoordinate(x, y), y * (_edgeSize / quadCount.y));
                // This probably wont matter for 3D procedural textures, but inverseLerp gives the pct of a value between the given min and max (0-1)
                newUvs[i] = new Vector2(Mathf.InverseLerp(0, _edgeSize, x * (_edgeSize / quadCount.x)), Mathf.InverseLerp(0, _edgeSize, y * (_edgeSize / quadCount.y)));
                i++;
            }
        }

        // Loop through x,y coordinates once for the bottom left of each quad and save its 2 triangles. Triangles go clockwise (Left hand rule).
        i = 0;
        for (int y = 0; y < quadCount.y; y++)
        {
            for (int x = 0; x < quadCount.x; x++)
            {
                // First Triangle
                newTriangles[i + 0] = vertexCount.x * y + x;
                newTriangles[i + 1] = vertexCount.x * (y + 1) + x;
                newTriangles[i + 2] = vertexCount.x * y + x + 1;
                // Second Triangle
                newTriangles[i + 3] = vertexCount.x * y + x + 1;
                newTriangles[i + 4] = vertexCount.x * (y + 1) + x;
                newTriangles[i + 5] = vertexCount.x * (y + 1) + x + 1;
                i += 6;
            }
        }
        // Create the mesh, name it, assign data, and calculate missing data.
        _mesh.name = "Square";
        _mesh.Clear(false);
        _mesh.vertices = newVertices;
        _mesh.triangles = newTriangles;
        _mesh.RecalculateNormals();
        _mesh.uv = newUvs;
        _mesh.RecalculateTangents();
    }

    public void GenerateNoiseMesh(Vector3 noiseOffset, Vector3 chunkOffset)
    {
        _chunkOffset = chunkOffset;
        _noiseOffset = noiseOffset;

        _noiseMap = new NoiseMap(_verticesPerEdge, _edgeSize, NoiseMod.formation3D);
        _noiseMap.SetNumDimensions(3);
        _noiseMap.SetOffset(_noiseOffset + chunkOffset);
        _noiseMap.Generate();

        DensityMap densityMap = new DensityMap(_verticesPerEdge);
        densityMap.SetMode3DNoise(_noiseMap);
        densityMap.SetChunkInfo(_chunkOffset, _edgeSize);
        densityMap.Generate();

        GenerateDensityMesh(densityMap);
    }

    public void GenerateSphereMesh(float radius, Vector3 chunkOffset)
    {
        _chunkOffset = chunkOffset;

        _noiseMap = new NoiseMap(_verticesPerEdge, _edgeSize, _noiseMod);
        //_noiseMap.SetOffset(_noiseOffset + chunkOffset); //old
        _noiseMap.SetOffset(_noiseOffset + chunkOffset); //new
        _noiseMap.SetNumDimensions(3);
        _noiseMap.Generate();

        DensityMap densityMap = new DensityMap(_verticesPerEdge);
        densityMap.SetModeSphere(radius, new Vector3(radius, radius, radius));
        densityMap.SetChunkInfo(_chunkOffset, _edgeSize);
        densityMap.SetNoise(noiseMap);
        densityMap.Generate();

        GenerateDensityMesh(densityMap);
    }

    public void GenerateDensityMesh(DensityMap densityMap)
    {
        MarchingCubes mc = new MarchingCubes();
        mc.SetResolution(densityMap.resolution.x, densityMap.resolution.y, densityMap.resolution.z);
        mc.InitAll();
        mc.SetExternalData(densityMap.values);
        mc.SetMethod(true); //delete this if mesh issues?
        mc.Run();

        _mesh.Clear();
        _mesh.name = "MarchingCubes";
        _mesh.vertices = GetMeshVertices(mc.vertices, mc.nverts, densityMap.scale, Vector3.zero);
        _mesh.triangles = TrianglesToInt(mc.triangles, mc.ntrigs);
        _mesh.normals = GetMeshNormals(mc.normals, mc.nverts);

        mc.CleanTemps();
        mc.CleanAll();
    }

    static Vector3[] GetMeshVertices(Vector3[] vertices, int numVerts, float scale, Vector3 offset)//need to add offsets later
    {
        Vector3[] meshVertices = new Vector3[numVerts];
        for (int i = 0; i < numVerts; i++)
        {
            meshVertices[i] = new Vector3((vertices[i].x / scale) + offset.x, (vertices[i].y / scale) + offset.y, (vertices[i].z / scale) + offset.z);
        }
        return meshVertices;
    }

    static Vector3[] GetMeshVertices(Vector3[] vertices, int numVerts, float yScale)//need to add offsets later
    {
        Vector3[] meshVertices = new Vector3[numVerts];
        for (int i = 0; i < numVerts; i++)
        {
            meshVertices[i] = new Vector3((vertices[i].x), (vertices[i].y) - 1, (vertices[i].z));
        }
        return meshVertices;
    }

    static Vector3[] GetMeshNormals(Vector3[] normals, int numVerts)
    {
        Vector3[] norms = new Vector3[numVerts];
        for (int i = 0; i < numVerts; i++)
        {
            norms[i] = new Vector3(normals[i].x, normals[i].y, normals[i].z);
        }
        return norms;
    }

    static Vector2[] VerticesToUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = (vertices[i] - Vector3.zero).normalized;
        }
        return uvs;
    }

    static int[] TrianglesToInt(Triangle[] triangles, int numTrigs)
    {
        int[] ints = new int[numTrigs * 3];
        for (int t = 0; t < numTrigs; t++)
        {
            ints[t * 3] = triangles[t].v1;
            ints[t * 3 + 1] = triangles[t].v2;
            ints[t * 3 + 2] = triangles[t].v3;
        }
        return ints;
    }
}