using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Octree
{
    private Cuboid _cube;
    //private bool _isLeaf = false;
    private float _minVoxelSize;
    private Voxel _voxel;

    private Octree[,,] _children = new Octree[2, 2, 2];

    /// <summary>
    /// Create an <see cref="Octree"/> root given it's center, minimum size, and smallest voxel size.
    /// </summary>
    /// <param name="center">The coordinate of the center of the Octree.</param>
    /// <param name="minSize">The minimum size the Octree must be.</param>
    /// <param name="leafSize">The size of each voxel.</param>
    public Octree(Vector3 center, float minSize, float leafSize)
    {
        // Could do some error checking here in case of negative sizes.
        _minVoxelSize = leafSize;

        float testSize = _minVoxelSize;
        bool keepLooping = testSize < minSize;

        while (keepLooping)
        {
            testSize *= 2;
            keepLooping = testSize < minSize;
        }
        _cube = new Cuboid(new Vector3(center.x - testSize / 2, center.y - testSize / 2, center.z - testSize / 2), new Vector3(testSize, testSize, testSize));
        LeafTest();
    }

    /// <summary>
    /// Create an <see cref="Octree"/> given it's minimum position, size, and the smallest voxel size in the tree.
    /// </summary>
    /// <param name="position">The coordinate of the minimum position.</param>
    /// <param name="size">The size of this Octree.</param>
    /// <param name="leafSize">The size of each voxel</param>
    public Octree(Vector3 position, Vector3 size, float leafSize)
    {
        _minVoxelSize = leafSize;
        _cube = new Cuboid(position, size);
        LeafTest();
    }

    /// <summary> The Cuboid representing the volume of this Octree. </summary>
    public Cuboid cube { get => _cube; }
    /// <summary> The Voxel representing a leaf on the Octree. </summary>
    public Voxel voxel { get => _voxel; set => _voxel = value; }

    void CreateChildren()
    {
        // calculate part of the offset to the coordinate center to use for each child. this is the same for all children, based on the parent cube (this one)
        Vector3 childSize = this._cube.size / 2;
        for (int z = 0; z < 2; z++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    Vector3 childOffset = new Vector3(childSize.x * x, childSize.y * y, childSize.z * z);
                    Vector3 childPosition = _cube.min + childOffset;
                    //Debug.LogFormat("Creating child in: {0} mode.", Application.isPlaying ? "Play" : "Editor");

                    _children[x, y, z] = new Octree(childPosition, childSize, _minVoxelSize);
                }
            }
        }
    }

    void LeafTest()
    {
        if (_cube.size.x > _minVoxelSize * 2) // as long as we haven't reached voxel size, create children
        {
            CreateChildren();
        }
        else if (_cube.size.x == _minVoxelSize)
        {
            //_isLeaf = true;
        }
    }

    void OnPositionChanged(Vector3 positionDelta)
    {
        _cube = new Cuboid(this._cube.min + positionDelta, this._cube.size);

        foreach (Octree child in _children)
        {
            if (child != null) child.OnPositionChanged(positionDelta);
        }
    }

    void OnSizeChanged(Vector3 sizeDelta)
    {
        this._cube = new Cuboid(this._cube.min, this._cube.size + sizeDelta);

        for (int z = 0; z < 2; z++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    if (_children[x,y,z] != null)
                    {
                        Vector3 offsetMult = new Vector3(x == 1 ? 1 : 0, y == 1 ? 1 : 0, z == 1 ? 1 : 0);
                        Vector3 childPositionDelta = (sizeDelta / 2);
                        childPositionDelta.Scale(offsetMult);
                        this._children[x, y, z].OnPositionChanged(childPositionDelta);
                        this._children[x, y, z].OnSizeChanged(sizeDelta * 2 / 4);
                    }
                }
            }
        }
    }
}
