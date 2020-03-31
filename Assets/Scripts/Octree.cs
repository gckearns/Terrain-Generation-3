using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Octree : MonoBehaviour
{
    private const int xNeg = 0;
    private const int xPos = 1;
    private const int yNeg = 0;
    private const int yPos = 1;
    private const int zNeg = 0;
    private const int zPos = 1;

    public Color[] colors = new Color[] { Color.black, Color.white, Color.red, Color.green, Color.blue };

    public bool drawWireframe;
    public Color wireframeColor;
    public int depth;
    public int maxDepth;
    public Cuboid cube;

    public Octree prefab;
    public Octree[,,] children = new Octree[2, 2, 2];

    public bool isInitialized { get => _isInitialized; }

    private bool _isInitialized;

    public void Initialize(Vector3 position, Vector3 size, int depth, int maxDepth, bool showWireframe, Color wireframeColor)
    {
        this.depth = depth;
        this.maxDepth = maxDepth;
        this.drawWireframe = showWireframe;
        this.wireframeColor = wireframeColor;
        this.cube = new Cuboid(position, size);
        gameObject.SetActive(true);
        if (depth < maxDepth) // as long as we haven't reached the depth limit, create children
        {
            CreateChildren();
        }
        _isInitialized = true;
    }

    void CreateChildren()
    {
        // calculate part of the offset to the coordinate center to use for each child. this is the same for all children, based on the parent cube (this one)
        Vector3 childSize = this.cube.size / 2;
        int childDepth = this.depth + 1;
        for (int z = 0; z < 2; z++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    Vector3 childOffset = new Vector3(childSize.x * x, childSize.y * y, childSize.z * z);
                    Vector3 childPosition = cube.min + childOffset;
                    //Debug.LogFormat("Creating child in: {0} mode.", Application.isPlaying ? "Play" : "Editor");
                    GameObject newChild = new GameObject("Octree-D:" + childDepth + "(" + x + "," + y + "," + z + ")");
                    newChild.transform.parent = transform;
                    newChild.AddComponent<Octree>();

                    this.children[x, y, z] = newChild.GetComponent<Octree>();
                    this.children[x, y, z].Initialize(childPosition, childSize, childDepth, this.maxDepth, this.drawWireframe, this.wireframeColor);

                }
            }
        }
    }

    public void Refresh(Vector3 newPosition, Vector3 newSize, bool newShowWireframe, Color newWireframeColor)
    {
        if (newPosition != this.cube.min)
        {
            Vector3 positionDelta = newPosition - this.cube.min;
            OnPositionChanged(positionDelta);
        }

        if (newSize != this.cube.size)
        {
            Vector3 sizeDelta = newSize - this.cube.size;
            OnSizeChanged(sizeDelta);
        }

        if (newShowWireframe != this.drawWireframe)
        {
            OnToggleWireframe(newShowWireframe);
        }
    }

    void OnPositionChanged(Vector3 positionDelta)
    {
        gameObject.SetActive(false);
        cube = new Cuboid(this.cube.min + positionDelta, this.cube.size);
        gameObject.SetActive(true);
        foreach (Octree child in children)
        {
            if (child != null) child.OnPositionChanged(positionDelta);
        }
    }

    void OnSizeChanged(Vector3 sizeDelta)
    {
        gameObject.SetActive(false);
        this.cube = new Cuboid(this.cube.min, this.cube.size + sizeDelta);
        gameObject.SetActive(true);

        for (int z = 0; z < 2; z++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    if (children[x,y,z] != null)
                    {
                        Vector3 offsetMult = new Vector3(x == 1 ? 1 : 0, y == 1 ? 1 : 0, z == 1 ? 1 : 0);
                        Vector3 childPositionDelta = (sizeDelta / 2);
                        childPositionDelta.Scale(offsetMult);
                        this.children[x, y, z].OnPositionChanged(childPositionDelta);
                        this.children[x, y, z].OnSizeChanged(sizeDelta * 2 / 4);
                    }
                }
            }
        }
    }

    public void OnMaxDepthChanged(int newMaxDepth)
    {
        //Debug.LogFormat("Depth- Current: {0}, Current Max: {1}, New Max: {2}", this.depth, this.maxDepth, newMaxDepth);
        if (newMaxDepth > this.maxDepth) // max depth increased, add children
        {
            //Debug.Log("New max is greater than current max");
            if (this.depth == this.maxDepth) // current depth is old max, create children at this level
            {
                //Debug.Log("Current depth is old max, create children at this level");
                this.maxDepth = newMaxDepth;
                CreateChildren();
            }
            else // children should already exist
            {
                //Debug.Log("Not yet at old max, update children with new max");
                this.maxDepth = newMaxDepth;
                foreach (Octree child in children)
                {
                    child.OnMaxDepthChanged(newMaxDepth);
                }
            }

        }
        else if (newMaxDepth < this.maxDepth) // max depth decreased, remove children
        {
            this.maxDepth = newMaxDepth;
            foreach (Octree child in children)
            {
                if (child != null)
                {
                    if (this.depth == newMaxDepth)  // this is deep as we go, remove children
                    {
                        for (int i = 0; i < transform.childCount; i++)
                        {
                            //Debug.LogFormat("Destroying child in: {0} mode.", Application.isPlaying ? "Play" : "Editor");
                            // GameObject.Destroy(transform.GetChild(i).gameObject);
                            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
                        }
                    }
                    else // update children with new max depth
                    {
                        child.OnMaxDepthChanged(newMaxDepth);
                    }
                }
            }
        }
    }

    void OnToggleWireframe(bool showWireframe)
    {
        gameObject.SetActive(false);
        this.drawWireframe = showWireframe;
        gameObject.SetActive(true);
        foreach (Octree child in children)
        {
            if (child != null) child.OnToggleWireframe(showWireframe);
        }
    }

    void OnDrawGizmos()
    {
        if (drawWireframe)
        {
            Gizmos.color = this.colors[depth];
            Gizmos.DrawWireCube(this.cube.center, this.cube.size);
        }
    }
}
