using System.Collections.Generic;
using UnityEngine;

public class TransvoxelBlock
{
    /// <summary> Number of cells per axis. </summary>
    Vector3Int _resolution;
    /// <summary> Voxel sample data. </summary>
    float _surfaceThreshold;
    /// <summary> Voxel sample data. </summary>
    float[] _voxelValues;

    List<Vector3> _vertices;
    List<int> _triangles;
    Dictionary<int, Cell> _cells;

    public TransvoxelBlock(int x, int y, int z)
    {
        _resolution = new Vector3Int(x, y, z);
        _voxelValues = new float[x * y * z];
        _cells = new Dictionary<int, Cell>();
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
    }

    public void Generate(float iso)
    {
        _surfaceThreshold = iso;
        for (int z = 0; z < _resolution.z - 1; z++)
        {
            for (int y = 0; y < _resolution.y - 1; y++)
            {
                for (int x = 0; x < _resolution.x - 1; x++)
                {
                    float[] corners = GetCellCorners(x, y, z);

                    int caseCode = GetCaseCode(corners);

                    if ((caseCode ^ ((GetValueSign(corners[7]) >> 7) & 0xFF)) != 0) //Move a corner's sign bit to #1 significance digit
                    {
                        // Cell has a nontrivial triangulation.
                        ProcessEdges(caseCode, corners, new Vector3Int(x, y, z));
                    }
                }
            }
        }
    }

    public Vector3[] vertices { get => _vertices.ToArray(); }
    public int[] triangles { get => _triangles.ToArray(); }

    public float GetVoxelValue(int x, int y, int z)
    {
        return _voxelValues[x + y * _resolution.x + z * _resolution.x * _resolution.y];
    }

    /// <summary>
    /// Set the density value at a single coordinate.
    /// </summary>
    /// <param name="x"> The X component of the coordinate. </param>
    /// <param name="y"> The Y component of the coordinate. </param>
    /// <param name="z"> The Z component of the coordinate. </param>
    /// <param name="value"> The density value of the coordinate. </param>
    public void SetVoxelValue(int x, int y, int z, float value)
    {
        _voxelValues[x + y * _resolution.x + z * _resolution.x * _resolution.y] = value;
    }

    /// <summary>
    /// Map the entire array of density values at once.
    /// </summary>
    /// <param name="voxelValues"> Float array representing 3D density values. </param>
    public void SetVoxelValues(float[] voxelValues)
    {
        _voxelValues = voxelValues;
    }

    protected Cell GetCell(int x, int y, int z)
    {
        if (!_cells.ContainsKey(x + y * _resolution.x + z * _resolution.x * _resolution.y))
        {
            _cells.Add(x + y * _resolution.x + z * _resolution.x * _resolution.y, new Cell());
        }
        return _cells[x + y * _resolution.x + z * _resolution.x * _resolution.y];
    }

    protected void AddRegularVertexToCell(int cellX, int cellY, int cellZ, Vector3 vertex, byte cellVertexIndex)
    {
        GetCell(cellX, cellY, cellZ).MapRegularVertex(_vertices.Count, cellVertexIndex);
        _vertices.Add(vertex);
        //Debug.LogFormat("Added Vertex: {0} to cell: ({1}, {2}, {3})", cellVertexIndex, cellX, cellY, cellZ);
    }

    protected void AddExtraVertexToCell(int cellX, int cellY, int cellZ, Vector3 vertex)
    {
        GetCell(cellX, cellY, cellZ).MapExtraVertex(_vertices.Count);
        _vertices.Add(vertex);
        //Debug.LogFormat("Added extra vertex to cell: ({0}, {1}, {2})", cellX, cellY, cellZ);
    }

    protected int GetExistingVertexIndex(int cellX, int cellY, int cellZ, byte cellVertexIndex)
    {
        return GetCell(cellX, cellY, cellZ).GetBlockVertexIndexOfCorner(cellVertexIndex);
    }

    protected void MapExistingVertexToCell(int cellX, int cellY, int cellZ, int blockIndex)
    {
        GetCell(cellX, cellY, cellZ).MapExtraVertex(blockIndex);
    }

    /// <summary>
    /// Test if the vertex identified by the index exists in the cell at the given coordinates.
    /// </summary>
    /// <param name="cellX"> The X component of the cell coordinates. </param>
    /// <param name="cellY"> The Y component of the cell coordinates. </param>
    /// <param name="cellZ"> The Z component of the cell coordinates. </param>
    /// <param name="cellVertexIndex"> Cell index of the vertex to test. </param>
    /// <returns></returns>
    protected bool VertexExists(int cellX, int cellY, int cellZ, byte cellVertexIndex)
    {
        return GetCell(cellX, cellY, cellZ).VertexExists(cellVertexIndex);
    }

    /// <summary>
    /// Get the position of a corner, given the cell coordinate and the corner's index.
    /// </summary>
    /// <param name="corner"> Index of the corner to get the position of. </param>
    /// <param name="cell"> Coordinates of the cell. </param>
    /// <returns></returns>
    protected Vector3 GetCornerPosition(byte corner, Vector3Int cell)
    {
        return new Vector3(cell.x + (corner & 0x01),
                           cell.y + ((corner & 0x02) >> 1),
                           cell.z + ((corner & 0x04) >> 2));
    }

    /// <summary>
    /// Gets the value of each corner of the cell given by x, y, and z.
    /// </summary>
    /// <param name="x"> Minimum X coordinate of the cell. </param>
    /// <param name="y"> Minimum Y coordinate of the cell. </param>
    /// <param name="z"> Minimum Z coordinate of the cell. </param>
    /// <returns> Float array with each byte 0xFF (corner inside) or 0x00 (corner outside). </returns>
    protected float[] GetCellCorners(int x, int y, int z)
    {
        return new float[8]
        {
            GetVoxelValue(x    , y    , z    ), //Corner 0                    6__________ 7
            GetVoxelValue(x + 1, y    , z    ), //Corner 1                    /|        /|
            GetVoxelValue(x    , y + 1, z    ), //Corner 2                   /_|______ / |
            GetVoxelValue(x + 1, y + 1, z    ), //Corner 3                 2|  |     3|  |
            GetVoxelValue(x    , y    , z + 1), //Corner 4      y           |  |______|__|
            GetVoxelValue(x + 1, y    , z + 1), //Corner 5      |  z        | / 4     | / 5
            GetVoxelValue(x    , y + 1, z + 1), //Corner 6      | /         |/________|/
            GetVoxelValue(x + 1, y + 1, z + 1)  //Corner 7      |/_____x   0           1
        };
    }

    /// <summary>
    /// Gets a signed byte (0xFF if negative) or (0x00 if positive)
    /// </summary>
    /// <param name="value"><see cref="float"/> value </param>
    /// <returns> 0xFF if negative, or 0x00 if positive </returns>
    protected sbyte GetValueSign(float value)
    {
        return (sbyte)Mathf.Sign(value);
    }

    /// <summary>
    /// Gets an 8-bit case code for a cell where each bit represents a corner inside or at/outside the iso-surface.
    /// </summary>
    /// <param name="corners"> An array of floats containing the density values at each of a cell's corners. </param>
    /// <returns></returns>
    protected int GetCaseCode(float[] corners)
    {
        return (((GetValueSign(corners[0]) >> 7) & 0x01)      //Move sign bit to #1 significance digit (1=neg, 0=pos), 0 out all but #1 digit.
              | ((GetValueSign(corners[1]) >> 6) & 0x02)      //Move sign bit to #2 significance digit (1=neg, 0=pos), 0 out all but #2 digit, add previous digit
              | ((GetValueSign(corners[2]) >> 5) & 0x04)      //Move sign bit to #3 significance digit (1=neg, 0=pos), 0 out all but #3 digit, add previous digits
              | ((GetValueSign(corners[3]) >> 4) & 0x08)      //Move sign bit to #4 significance digit (1=neg, 0=pos), 0 out all but #4 digit, add previous digits
              | ((GetValueSign(corners[4]) >> 3) & 0x10)      //Move sign bit to #5 significance digit (1=neg, 0=pos), 0 out all but #5 digit, add previous digits
              | ((GetValueSign(corners[5]) >> 2) & 0x20)      //Move sign bit to #6 significance digit (1=neg, 0=pos), 0 out all but #6 digit, add previous digits
              | ((GetValueSign(corners[6]) >> 1) & 0x40)      //Move sign bit to #7 significance digit (1=neg, 0=pos), 0 out all but #7 digit, add previous digits
              |  (GetValueSign(corners[7])       & 0x80));    //0 out all but #8 digit, add previous digits
    }

    protected void ProcessEdges(int caseCode, float[] cornerValues, Vector3Int cellPosition)
    {
        //                    ______________   
        //                   /|     82     /|  
        //                13/ |         83/ |
        //                 /__|_________ /  |81
        //                | 11|  42     |   |
        //   y            |   |_________|___|
        //   |   z      51|   /     22  |41 / 
        //   |  /         |  /33        |  /23
        //   | /          | /           | /
        //   |/______x    |/____________|/     
        //                       62
        //
        // The high nibble of this code indicates which direction to go in order to reach the correct preceding cell (left hex digit)
        // Bit values 1, 2, and 4 in high nibble indicate that we must subtract one from the x, y, and/ or z coordinate, respectively (left hex digit)
        // Bit value 8 indicates that a new vertex is to be created for the current cell (left hex digit)
        // The low nibble of the 8-bit code gives the index of the vertex in the preceding cell that  (right hex digit)
        // should be reused or the index of the vertex in the current cell that should be created. (right hex digit)

        int cellIndex = cellPosition.x + cellPosition.y * _resolution.x + cellPosition.z * _resolution.x * _resolution.y;
        Cell thisCell = new Cell();
        _cells.Add(cellIndex, thisCell);

        byte regularCellClass = Transvoxel.regularCellClass[caseCode];  // Get the index for the equivalence class
        // Get the equivalence class data. Tells us how many triangles and vertices, and order of vertices
        RegularCellData regularCellData = Transvoxel.regularCellData[regularCellClass];

        // Get the list of edge codes for the vertex locations for this cell's case code.
        // Each value in the array is 2 hex bytes. First hex byte is an edge code of the cell.
        // Second byte is the indices of the 2 corners connected by the edge.
        // (e.g. 0x2315 means edge 23 connects corners 1 and 5)
        ushort[] regularEdgeCodes = Transvoxel.regularVertexData[caseCode];

        // Construct a byte code indicating valid preceding cells
        byte validDirections = (byte)(((cellPosition.x > 0) ? 1 : 0) +
                                      ((cellPosition.y > 0) ? 2 : 0) +
                                      ((cellPosition.z > 0) ? 4 : 0));
        //Debug.LogFormat("Current Cell: {0}; Valid Directions: (-{1}x, -{2}y, -{3}z); CaseCode: {4}; Regular Cell Class: {5}" +
        //    "\n NumEdgeCodes: {6}; NumVertices: {7}; NumTriangles: {8}",
        //    cellPosition, validDirections & 0x01, (validDirections & 0x02) >> 1, (validDirections & 0x04) >> 2, caseCode, regularCellClass,
        //    regularEdgeCodes.Length, regularCellData.VertexCount, regularCellData.TriangleCount);

        for (int i=0; i < regularEdgeCodes.Length; i++)
        {
            ushort edge = regularEdgeCodes[i];

            // v0 is the lower-numbered corner index.
            byte v0 = (byte)((edge & 0x00F0) >> 4);
            // v1 is the higher-numbered corner index.
            byte v1 = (byte)(edge & 0x000F); 

            float d0 = cornerValues[v0];
            float d1 = cornerValues[v1];

            // Interpolation parameter. Distance of new vertex from corner v1.
            // Multiply to a predictable resolution (256). 
            // t = 0-256 means 257 possible positions along the edge (including corners).
            int t = Mathf.RoundToInt((d1 * 256) / (d1 - d0));

            // Direction to go in order to reach the correct preceding cell. The 
            // bit values 1, 2, and 4 in this nibble indicate that we must subtract
            // one from the x, y, and/ or z coordinate, respectively.  8 indicates 
            // that a new vertex is to be created for the current cell.
            byte directionCode = (byte)((edge & 0xF000) >> 12);

            // index of the vertex in the preceding cell that should be reused or 
            // the index of the vertex in the current cell that should be created.
            byte vertexIndex = (byte)((edge & 0x0F00) >> 8);

            byte directionIfSurfaceCorner = (byte)(v0 ^ 7);
            
            bool precedingCellExists = (directionCode & validDirections) == directionCode;
            bool precedingCellCornerExists = (directionIfSurfaceCorner & validDirections) == directionIfSurfaceCorner;

            //Debug.LogFormat("Current Edge: {0:X};  Direction Code: (-{1}x, -{2}y, -{3}z); Cell Exists: {4}" +
            //    "\n VertexIndex: {5};  v0 = {6} ({7:F3});  v1 = {8} ({9:F3});  t = {10}" +
            //    "\n Corner Direction: (-{11}x, -{12}y, -{13}z); Cell Exists: {14}", 
            //    edge, directionCode & 0x01, (directionCode & 0x02) >> 1, (directionCode & 0x04) >> 2, precedingCellExists,
            //    vertexIndex, v0, d0, v1, d1, t,
            //    directionIfSurfaceCorner & 0x01, (directionIfSurfaceCorner & 0x02) >> 1, (directionIfSurfaceCorner & 0x04) >> 2, precedingCellCornerExists);

            // Determine if the vertex lies between or on the corners of the edge.
            if ((t & 0x00FF) != 0)
            {
                // Vertex lies in the interior of the edge.

                // Distance from corner v0
                int u = 256 - t; 

                // Convert the byte fraction to float
                float pointDelta = u / 256f;

                // Determine the axis to apply the interpolation to
                byte axis = (byte)(v0 ^ v1);

                // Get the position of the lower-numbered corner
                Vector3 p0 = GetCornerPosition(v0, cellPosition);

                Vector3 point = new Vector3(p0.x + pointDelta * (axis & 0x01), 
                                            p0.y + pointDelta * ((axis & 0x02) >> 1), 
                                            p0.z + pointDelta * ((axis & 0x04) >> 2));

                if (precedingCellExists)
                {
                    int precedingCellX = cellPosition.x - (directionCode & 0x01);
                    int precedingCellY = cellPosition.y - ((directionCode & 0x02) >> 1);
                    int precedingCellZ = cellPosition.z - ((directionCode & 0x04) >> 2);

                    if (VertexExists(precedingCellX, precedingCellY, precedingCellZ, vertexIndex))
                    {
                        int blockIndex = GetExistingVertexIndex(precedingCellX, precedingCellY, precedingCellZ, vertexIndex);
                        MapExistingVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, blockIndex);
                        //Debug.Log("Reusing edge vertex");
                    }
                    else
                    {
                        AddRegularVertexToCell(precedingCellX, precedingCellY, precedingCellZ, point, vertexIndex);
                        int blockIndex = GetExistingVertexIndex(precedingCellX, precedingCellY, precedingCellZ, vertexIndex);
                        MapExistingVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, blockIndex);
                        Debug.Log("Added vertex in preceding cell on edge.");  // This shouldn't happen. ...I think.
                    }
                }
                else if (v1 == 7)
                {
                    AddRegularVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, point, vertexIndex);
                }
                else
                {
                    AddExtraVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, point);
                }
            }
            else if(t == 0)
            {
                // Vertex lies at the higher-numbered endpoint.
                //Debug.Log("t = 0; Vertex lies at the higher-numbered endpoint.");
                if (v1 == 7)
                {
                    // This cell owns the vertex.
                    Vector3 p1 = GetCornerPosition(v1, cellPosition);
                    AddRegularVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, p1, vertexIndex);
                    //Debug.LogFormat("This cell owns the vertex.");
                }
                else
                {
                    // Try to reuse corner vertex from a preceding cell.
                    //Debug.LogFormat("v1 = {0}; Try to reuse corner vertex from a preceding cell.", v1);

                    directionIfSurfaceCorner = (byte)(v1 ^ 7);
                    precedingCellCornerExists = (directionIfSurfaceCorner & validDirections) == directionIfSurfaceCorner;

                    Vector3 p1 = GetCornerPosition(v1, cellPosition);
                    if (precedingCellCornerExists)
                    {
                        int precedingCellX = cellPosition.x - (directionIfSurfaceCorner & 0x01);
                        int precedingCellY = cellPosition.y - ((directionIfSurfaceCorner & 0x02) >> 1);
                        int precedingCellZ = cellPosition.z - ((directionIfSurfaceCorner & 0x04) >> 2);

                        if (VertexExists(precedingCellX, precedingCellY, precedingCellZ, vertexIndex))
                        {
                            int blockIndex = GetExistingVertexIndex(precedingCellX, precedingCellY, precedingCellZ, vertexIndex);
                            MapExistingVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, blockIndex);
                            //Debug.Log("Reusing greater corner vertex");
                        }
                        else
                        {
                            AddRegularVertexToCell(precedingCellX, precedingCellY, precedingCellZ, p1, vertexIndex);
                            int blockIndex = GetExistingVertexIndex(precedingCellX, precedingCellY, precedingCellZ, vertexIndex);
                            MapExistingVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, blockIndex);
                            Debug.Log("Added vertex in preceding cell at greater corner.");
                        }
                    }
                    else if (v1 == 7)
                    {
                        AddRegularVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, p1, vertexIndex);
                    }
                    else
                    {
                        AddExtraVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, p1);
                    }
                }
            }
            else //t=256
            {
                // Vertex lies at the lower-numbered endpoint.
                // Always try to reuse corner vertex from a preceding cell.
                //Debug.LogFormat("t = {0}; Vertex lies at the lower-numbered endpoint. " +
                //    "\n Always try to reuse corner vertex from a preceding cell.", t);

                directionIfSurfaceCorner = (byte)(v0 ^ 7);
                precedingCellCornerExists = (directionIfSurfaceCorner & validDirections) == directionIfSurfaceCorner;

                Vector3 p0 = GetCornerPosition(v0, cellPosition);
                if (precedingCellCornerExists)
                {
                    int precedingCellX = cellPosition.x - (directionIfSurfaceCorner & 0x01);
                    int precedingCellY = cellPosition.y - ((directionIfSurfaceCorner & 0x02) >> 1);
                    int precedingCellZ = cellPosition.z - ((directionIfSurfaceCorner & 0x04) >> 2);

                    if (VertexExists(precedingCellX, precedingCellY, precedingCellZ, vertexIndex))
                    {
                        int blockIndex = GetExistingVertexIndex(precedingCellX, precedingCellY, precedingCellZ, vertexIndex);
                        MapExistingVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, blockIndex);
                        //Debug.Log("Reusing lesser corner vertex");
                    }
                    else
                    {
                        AddRegularVertexToCell(precedingCellX, precedingCellY, precedingCellZ, p0, vertexIndex);
                        int blockIndex = GetExistingVertexIndex(precedingCellX, precedingCellY, precedingCellZ, vertexIndex);
                        MapExistingVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, blockIndex);
                        //Debug.Log("Added vertex in preceding cell at lower corner.");
                    }
                }
                else
                {
                    AddExtraVertexToCell(cellPosition.x, cellPosition.y, cellPosition.z, p0);
                }
            }
        }

        foreach (int index in regularCellData.VertexIndices)
        {
            _triangles.Add(GetCell(cellPosition.x, cellPosition.y, cellPosition.z).GetBlockIndex(index));
        }
    }

    protected class Cell
    {
        Dictionary<byte, int> _cornerIndexMap;
        List<int> _vertexIndexMap;


        public Cell()
        {
            _cornerIndexMap = new Dictionary<byte, int>(4);
            _vertexIndexMap = new List<int>(12);
        }

        public int GetBlockIndex(int cellIndex)
        {
            return _vertexIndexMap[cellIndex];
        }

        public int GetBlockVertexIndexOfCorner(byte cornerIndex)
        {
            if (_cornerIndexMap.ContainsKey(cornerIndex))
            {
                return _cornerIndexMap[cornerIndex];
            }
            else
            {
                return -1;
            }
        }

        public void MapExtraVertex(int blockIndex)
        {
            _vertexIndexMap.Add(blockIndex);
        }

        public void MapRegularVertex(int blockIndex, byte cornerIndex)
        {
            _cornerIndexMap.Add(cornerIndex, blockIndex);
            _vertexIndexMap.Add(blockIndex);
        }

        public bool VertexExists(byte cornerIndex)
        {
            return _cornerIndexMap.ContainsKey(cornerIndex);
        }
    }
}
