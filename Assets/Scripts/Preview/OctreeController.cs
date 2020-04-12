using System.Collections;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEngine;

public class OctreeController : MonoBehaviour
{
    [Range(1,4)]
    public int maxOctreeDepth = 1;
    [Range(4,16)]
    public float octreeSize = 4;
    public Vector3 offset = Vector3.zero;
    public bool showWireframe = false;
    public Color wireframeColor = Color.white;
    public Octree octreePrefab;

    public Octree octree { get
        {
            //_octree = _octree ?? GetComponentInChildren<Octree>();
            if (_octree == null)
            {
                //_octree = (new GameObject("Octree-D:1", typeof(Octree))).GetComponentInChildren<Octree>();
                //_octree.transform.parent = transform;
            }
            return _octree;
        }
    }

    private Octree _octree;

    // on script loaded or inspector value changed. editor only
    void OnValidate()
    {
        if (_octree != null)
            Generate();
        else
            Debug.LogWarning("Cannot generate the octree in OnValidate().");
    }

    public void Generate()
    {
        //if (octree.isInitialized)
        //    octree.Refresh(offset, new Vector3(octreeSize, octreeSize, octreeSize), showWireframe, wireframeColor);
        //else
        //    InitializeOctree();
    }

    public void UpdateDepth()
    {
        //if (!octree.isInitialized)
        //    InitializeOctree();
        //if (octree.minVoxelSize != this.maxOctreeDepth)
        //    octree.OnMaxDepthChanged(this.maxOctreeDepth);
    }

    void InitializeOctree()
    {
        //octree.Initialize(offset, new Vector3(octreeSize, octreeSize, octreeSize), 1, maxOctreeDepth, showWireframe, wireframeColor);
    }
}
