using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainChunkManager : MonoBehaviour
{
    public GameObject viewer;
    public Dictionary<Vector3, TerrainChunk> terrainChunks;
    public float viewerDeltaRequiredToUpdate;
    public float maxViewDistance;
    public float chunkSize;
    private float viewerDeltaSqrMagnitude { get => (viewerDeltaRequiredToUpdate * viewerDeltaRequiredToUpdate); }
    public Vector3 viewerLocationLastChunkUpdate;
    public Preview preview;
    public List<Vector3> visibleChunks;

    // Start is called before the first frame update
    void Start()
    {
        terrainChunks = new Dictionary<Vector3, TerrainChunk>();
        visibleChunks = new List<Vector3>();
        Debug.Log("TerrainChunks Dictionary initialized.");
        preview = GetComponentInParent<Preview>();
        viewer = preview.viewer;
        viewerDeltaRequiredToUpdate = preview.viewerDeltaRequiredToUpdate;
        viewerLocationLastChunkUpdate = viewer.transform.position;
        chunkSize = preview.chunkSize;
        maxViewDistance = preview.maxViewDistance;
        OnUpdateChunks();
    }

    // Update is called once per frame
    void Update()
    {
        // If the viewer moved far enough to bother, update chunks and viewer position
        if ((viewerLocationLastChunkUpdate - viewer.transform.position).sqrMagnitude >= viewerDeltaSqrMagnitude)
        {
            Debug.Log("Viewer moved required minimum distance to update.");
            viewerLocationLastChunkUpdate = viewer.transform.position;
            OnUpdateChunks();
        }
    }

    void OnUpdateChunks()
    {
        //Debug.Log("Updating chunks.");
        Vector3 position = new Vector3(viewer.transform.position.x, viewer.transform.position.y, viewer.transform.position.z);
        //Debug.LogFormat("Viewer is at: {0}. In chunk: {1}.", position, GetChunkAddressFromPoint(position));
        Vector3[] chunksInCubicRange = GetChunksWithinCubicRange(position, maxViewDistance);
        Vector3[] filteredChunks = FilterChunksInRange(position, maxViewDistance, chunksInCubicRange);
        //Vector3[] chunksInSquareRange = GetChunksWithinSquareRange(position, maxViewDistance);
        //Vector3[] filteredChunks = FilterChunksInRange(position, maxViewDistance, chunksInSquareRange);

        List<Vector3> chunksToRemove = new List<Vector3>();
        foreach (Vector3 chunk in visibleChunks)
        {
            if (!IsPointInRangeOfChunk(position, maxViewDistance, chunk))
            {
                chunksToRemove.Add(chunk);
            }
        }
        foreach (Vector3 chunk in chunksToRemove)
        {
            UpdateChunkVisibility(chunk, false);
            visibleChunks.Remove(chunk);
            //Debug.LogFormat("Removed visible chunk: {0}", visibleChunks[i]);
        }

        foreach (Vector3 chunk in filteredChunks)
        {
            UpdateChunkVisibility(chunk, true);
        }
    }

    void UpdateChunkVisibility(Vector3 chunkAddress, bool isVisible)
    {
        if (!terrainChunks.ContainsKey(chunkAddress))
        {
            GameObject newGameObject = new GameObject(chunkAddress.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            newGameObject.transform.parent = transform;
            newGameObject.transform.position = chunkAddress;
            TerrainChunk newTerrainChunk = newGameObject.AddComponent<TerrainChunk>();
            terrainChunks.Add(chunkAddress, newTerrainChunk);
            //Debug.LogFormat("Created chunk: {0}", chunkAddress);
        }
        terrainChunks[chunkAddress].isVisible = isVisible; // Do something else
        terrainChunks[chunkAddress].gameObject.SetActive(isVisible);
        if (!visibleChunks.Contains(chunkAddress))
        {
            visibleChunks.Add(chunkAddress);
        }
    }

    bool IsPointInRangeOfChunk(Vector3 origin, float distance, Vector3 chunk)
    {
        float sqrDistance = (distance * distance);
        Vector3 closestPoint = GetClosestPointInChunk(origin, chunk);
        return (origin - closestPoint).sqrMagnitude <= sqrDistance;
    }

    Vector3[] FilterChunksInRange(Vector3 origin, float distance, Vector3[] chunks)
    {
        float sqrDistance = (distance * distance);
        Vector3 originChunk = GetChunkAddressFromPoint(origin);
        Vector3[] filteredChunks = new Vector3[chunks.Length];
        int i = 0;
        foreach (Vector3 chunk in chunks)
        {
            Vector3 closestPoint = GetClosestPointInChunk(origin, chunk);
            if ((origin - closestPoint).sqrMagnitude <= sqrDistance)
            {
                filteredChunks[i] = chunk;
                i++;
            }
            //else
            //{
            //    Debug.LogFormat("{0} is out of view distance.", chunk);
            //}
        }
        Vector3[] returnChunks = new Vector3[i];
        System.Array.Copy(filteredChunks, returnChunks, i);
        Debug.LogFormat("{0} chunks within view distance. Removed {1} chunks from consideration.", i, chunks.Length - i);
        return returnChunks;
    }

    Vector3 GetClosestPointInChunk(Vector3 origin, Vector3 chunk)
    {
        Cuboid chunkCube = new Cuboid(chunk, new Vector3(chunkSize, chunkSize, chunkSize));
        float closestChunkX, closestChunkY, closestChunkZ;

        if (origin.x < chunkCube.xMin) closestChunkX = chunkCube.xMin;
        else if (origin.x > chunkCube.xMax) closestChunkX = chunkCube.xMax;
        else closestChunkX = origin.x;

        if (origin.y < chunkCube.yMin) closestChunkY = chunkCube.yMin;
        else if (origin.y > chunkCube.yMax) closestChunkY = chunkCube.yMax;
        else closestChunkY = origin.y;

        if (origin.z < chunkCube.zMin) closestChunkZ = chunkCube.zMin;
        else if (origin.z > chunkCube.zMax) closestChunkZ = chunkCube.zMax;
        else closestChunkZ = origin.z;

        return new Vector3(closestChunkX, closestChunkY, closestChunkZ);
    }

    Vector3[] GetChunksWithinCubicRange(Vector3 origin, float distance)
    {
        int chunkRange = Mathf.CeilToInt(distance / chunkSize);

        Vector3[] chunkCoordinates = new Vector3[(chunkRange * 2) * (chunkRange * 2) * (chunkRange * 2)];
        int i = 0;
        for (int z = -chunkRange; z < chunkRange; z++)
        {
            for (int y = -chunkRange; y < chunkRange; y++)
            {
                for (int x = -chunkRange; x < chunkRange; x++)
                {
                    Vector3 chunkCoordinate = GetChunkAddressFromPoint(new Vector3(origin.x + x * chunkSize, origin.y + y * chunkSize, origin.z + z * chunkSize));
                    chunkCoordinates[i] = chunkCoordinate;
                    i++;
                    //Debug.LogFormat("Added chunk: {0}.", chunkCoordinate);
                }
            }
        }
        Debug.LogFormat("Added {0} chunks to validate.", i);
        return chunkCoordinates;
    }

    Vector3[] GetChunksWithinSquareRange(Vector3 origin, float distance)
    {
        int chunkRange = Mathf.CeilToInt(distance / chunkSize);
        int i = 0;
        Vector3[] chunkCoordinates = new Vector3[(chunkRange * 2) * (chunkRange * 2)];
        for (int z = -chunkRange; z < chunkRange; z++)
        {
            for (int x = -chunkRange; x < chunkRange; x++)
            {
            Vector3 chunkCoordinate = GetChunkAddressFromPoint(new Vector3(origin.x + x * chunkSize, 0, origin.z + z * chunkSize));
                chunkCoordinates[i] = chunkCoordinate;
                i++;
                //Debug.LogFormat("Added chunk: {0}.", chunkCoordinate);
            }
        }
        Debug.LogFormat("Added {0} chunks to validate.", i);
        return chunkCoordinates;
    }

    Vector3 GetChunkAddressFromPoint(Vector3 coordinate)
    {
        float chunkX = Mathf.FloorToInt(coordinate.x / chunkSize) * chunkSize;
        float chunkY = Mathf.FloorToInt(coordinate.y / chunkSize) * chunkSize;
        float chunkZ = Mathf.FloorToInt(coordinate.z / chunkSize) * chunkSize;
        return new Vector3(chunkX, chunkY, chunkZ);
    }
}
