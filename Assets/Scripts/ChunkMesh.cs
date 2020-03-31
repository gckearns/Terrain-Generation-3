using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
    /// <summary>
    /// Get a square mesh with given arguments, centered on the offset parameter's coordinates.
    /// </summary>
    /// <param name="numVerts">Number of of vertices per axis.</param>
    /// <param name="diameter">Length of each side of the mesh.</param>
    /// <param name="offset">Position difference from the transform coordinate.</param>
    /// <returns></returns>
    public static Mesh GetSquareMesh(ref Mesh mesh, Vector2Int numVerts, Vector2 size, Vector3 offset, NoiseMap2D noise)
    {
        // Declare and initialize array variables to hold mesh data
        Vector3[] vertices = new Vector3[numVerts.x * numVerts.y];
        Vector2[] uvs = new Vector2[vertices.Length];   // UV length always equals vertices length
        Vector2Int numQuads = new Vector2Int(numVerts.x - 1, numVerts.y - 1);
        int[] triangles = new int[(numQuads.x * numQuads.y) * 6];   // Resolution represents how many quad-sides along an axis.

        // Loop through x,y coordinates to create vertices and uv coordinates
        int i = 0;
        for (int y = 0; y < numVerts.y; y++)
        {
            for (int x = 0; x < numVerts.x; x++)
            {
                vertices[i] = new Vector3(x * (size.x / numQuads.x) + offset.x, noise.values[x, y] + offset.y, y * (size.y / numQuads.y) + offset.z);
                // This probably wont matter for 3D procedural textures, but inverseLerp gives the pct of a value between the given min and max (0-1)
                uvs[i] = new Vector2(Mathf.InverseLerp(0, size.x, x * (size.x / numQuads.x)), Mathf.InverseLerp(0, size.y, y * (size.y / numQuads.y)));
                i++;
            }
        }

        // Loop through x,y coordinates once for the bottom left of each quad and save its 2 triangles. Triangles go clockwise (Left hand rule).
        i = 0;
        for (int y = 0; y < numQuads.y; y++)
        {
            for (int x = 0; x < numQuads.x; x++)
            {
                // First Triangle
                triangles[i + 0] = numVerts.x * y + x;
                triangles[i + 1] = numVerts.x * (y + 1) + x;
                triangles[i + 2] = numVerts.x * y + x + 1;
                // Second Triangle
                triangles[i + 3] = numVerts.x * y + x + 1;
                triangles[i + 4] = numVerts.x * (y + 1) + x;
                triangles[i + 5] = numVerts.x * (y + 1) + x + 1;
                i += 6;
            }
        }
        // Create the mesh, name it, assign data, and calculate missing data.
        //Mesh mesh = new Mesh();
        mesh.name = "Square";
        mesh.Clear(false);
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uvs;
        mesh.RecalculateTangents();

        return mesh;
    }

    public static Mesh MyMCMesh(ref Mesh mesh, float radius, int vertexResolution, float chunkSize, Vector3 offset)
    {
        int pad = 0;

        MarchingCubes mc = new MarchingCubes();
        mc.SetResolution(vertexResolution + pad, vertexResolution + pad, vertexResolution + pad);
        mc.InitAll();

        //float r = radius;
        float scale = (vertexResolution - 1) / chunkSize; //is this right or do i subtract 1 from resolution?

        for (int k = 0; k < mc.sizeZ; k++)
        {
            for (int j = 0; j < mc.sizeY; j++)
            {
                for (int i = 0; i < mc.sizeX; i++)
                {
                    float x = i + (offset.x - radius) * scale; // remove:" - radius * mcScale;" to center the sphere on 0,0,0
                    float y = j + (offset.y - radius) * scale;
                    float z = k + (offset.z - radius) * scale;
                    float modR = radius * scale;
                    float surface = (x * x) + (y * y) + (z * z) - (modR * modR);
                    mc.SetData(surface, i, j, k);
                }
            }
        }
        mc.Run();

        mesh.Clear();
        mesh.name = "MarchingCubes";
        mesh.vertices = GetMeshVertices(mc.vertices, mc.nverts, scale, offset);
        //TODO: change Triangle struct more useful??
        mesh.triangles = TrianglesToInt(mc.triangles, mc.ntrigs);
        mesh.normals = GetMeshNormals(mc.normals, mc.nverts);
        //mesh.uv = VerticesToUVs(mesh.vertices);
        //mesh.RecalculateTangents();
        mc.CleanTemps();
        mc.CleanAll();

        return mesh;
    }

    public static Mesh DensityMesh(ref Mesh mesh, int vertexResolution, DensityMap densityMap)
    {
        int pad = 0;

        MarchingCubes mc = new MarchingCubes();
        mc.SetResolution(vertexResolution + pad, vertexResolution + pad, vertexResolution + pad);
        mc.InitAll();

        for (int k = 0; k < mc.sizeZ; k++)
        {
            for (int j = 0; j < mc.sizeY; j++)
            {
                for (int i = 0; i < mc.sizeX; i++)
                {
                    //Debug.LogFormat("Coordinates: ({0}, {1}, {2}):{3}", i, j, k, densityMap.map[i, j, k]);
                    mc.SetData(densityMap.map[i,j,k], i, j, k);
                }
            }
        }
        mc.Run();

        mesh.Clear();
        mesh.name = "MarchingCubes";
        mesh.vertices = GetMeshVertices(mc.vertices, mc.nverts, densityMap.noiseMap.coordScale);
        //TODO: change Triangle struct more useful??
        mesh.triangles = TrianglesToInt(mc.triangles, mc.ntrigs);
        mesh.normals = GetMeshNormals(mc.normals, mc.nverts);
        //mesh.uv = VerticesToUVs(mesh.vertices);
        //mesh.RecalculateTangents();
        mc.CleanTemps();
        mc.CleanAll();

        return mesh;
    }

    //static Vector3[] McVertexToVector3(Vertex2[] vertices, int numVerts, float radius)
    //{
    //    Vector3[] vectors = new Vector3[numVerts];
    //    for (int i = 0; i < numVerts; i++)
    //    {
    //        vectors[i] = new Vector3(vertices[i].x - radius, vertices[i].y - radius, vertices[i].z - radius);
    //    }
    //    return vectors;
    //}

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

    //static Vector3[] VertexToVector3Normals(Vertex2[] vertices, int numVerts)
    //{
    //    Vector3[] normals = new Vector3[numVerts];
    //    for (int i = 0; i < numVerts; i++)
    //    {
    //        normals[i] = new Vector3(vertices[i].nx, vertices[i].ny, vertices[i].nz);
    //    }
    //    return normals;
    //}

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