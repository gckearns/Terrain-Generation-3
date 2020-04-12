using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Octree
{
    private float _minVoxelSize;
    private Cuboid _cube;

    private Octree[,,] _children = new Octree[2, 2, 2];

    public Octree(Vector3 center, float minSize, float minVoxelSize)
    {
        // Could do some error checking here in case of negative sizes.
        _minVoxelSize = minVoxelSize;

        float testSize = _minVoxelSize;

        bool keepLooping = testSize < minSize;

        while (keepLooping)
        {
            testSize *= 2;
            keepLooping = testSize < minSize;
        }

        _cube = new Cuboid(new Vector3(center.x - testSize / 2, center.y - testSize / 2, center.z - testSize / 2), new Vector3(testSize, testSize, testSize));
        if (_cube.size.x > _minVoxelSize) // as long as we haven't reached the depth limit, create children
        {
            CreateChildren();
        }
    }

    public Octree(Vector3 position, Vector3 size, float minVoxelSize)
    {
        _minVoxelSize = minVoxelSize;

        _cube = new Cuboid(position, size);
        if (_cube.size.x > _minVoxelSize) // as long as we haven't reached the depth limit, create children
        {
            CreateChildren();
        }
    }

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
