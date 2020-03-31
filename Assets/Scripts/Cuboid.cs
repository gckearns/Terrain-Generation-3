using System;
using UnityEngine;

/// <summary>
/// A 3D cuboid defined by X, Y, and Z position, width, height, and depth.
/// </summary>
public struct Cuboid : IEquatable<Cuboid>
{
    private Vector3 _position;
    private Vector3 _size;

    /// <summary>
    /// Creates a new <see cref="Cuboid"/> defined by its position and size as <see cref="Vector3"/>s.
    /// </summary>
    /// <param name="position">The position of the minimum corner of the cuboid.</param>
    /// <param name="size">The width, height, and depth of the cuboid.</param>
    public Cuboid(Vector3 position, Vector3 size)
    {
        this._position = position;
        this._size = size;
    }

    /// <summary>
    /// Creates a new <see cref="Cuboid"/> defined by its X, Y, and Z position, width, height, and depth.
    /// </summary>
    /// <param name="x">The X value the cuboid is measured from.</param>
    /// <param name="y">The Y value the cuboid is measured from.</param>
    /// <param name="z">The Z value the cuboid is measured from.</param>
    /// <param name="width">The width of the cuboid.</param>
    /// <param name="height">The height of the cuboid.</param>
    /// <param name="depth">The depth of the cuboid.</param>
    public Cuboid(float x, float y, float z, float width, float height, float depth)
    {
        this._position = new Vector3(x, y, z);
        this._size = new Vector3(width, height, depth);
    }

    /// <summary>The position of the center of the cuboid.</summary>
    public Vector3 center { get => new Vector3(_position.x + (_size.x / 2), _position.y + (_size.y / 2), _position.z + (_size.z / 2)); }
    /// <summary>The width, height, and depth of the cuboid.</summary>
    public Vector3 size { get => _size; }
    /// <summary>The position of the minimum corner of the cuboid.</summary>
    public Vector3 min { get => new Vector3(xMin, yMin, zMin); }
    /// <summary>The position of the maximum corner of the cuboid.</summary>
    public Vector3 max { get => new Vector3(xMax, yMax, zMax); }
    /// <summary>The minimum X coordinate of the cuboid.</summary>
    public float xMin { get => _position.x; }
    /// <summary>The maximum X coordinate of the cuboid.</summary>
    public float xMax { get => _position.x + _size.x; }
    /// <summary>The position of the center of the cuboid.</summary>
    public float xSize { get => _size.x; }
    /// <summary>The width of the cuboid.</summary>
    public float yMin { get => _position.y; }
    /// <summary>The maximum Y coordinate of the cuboid.</summary>
    public float yMax { get => _position.y + _size.y; }
    /// <summary>The height of the cuboid.</summary>
    public float ySize { get => _size.y; }
    /// <summary>The minimum Z coordinate of the cuboid.</summary>
    public float zMin { get => _position.z; }
    /// <summary>The maximum Z coordinate of the cuboid.</summary>
    public float zMax { get => _position.z + _size.z; }
    /// <summary>The depth of the cuboid.</summary>
    public float zSize { get => _size.z; }

    /// <summary>
    /// Returns <see langword="true"/> if the given cuboid is exactly equal to this cuboid.
    /// </summary>
    /// <param name="cuboid"></param>
    /// <returns></returns>
    public bool Equals(Cuboid cuboid)
    {
        return _position.Equals(cuboid._position) && _size.Equals(cuboid.size);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the given object is a <see cref="Cuboid"/> exactly equal to this cuboid.
    /// </summary>
    /// <param name="cuboid"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (!(obj is Cuboid cuboid))
            return false;
        return Equals(cuboid);
    }

    /// <summary>
    /// Returns true if the x, y, and z components of point are inside this cuboid.
    /// </summary>
    /// <param name="point">Point to test.</param>
    /// <returns></returns>
    public bool Contains(Vector3 point)
    {
        return (xMin <= point.x && point.x <= xMax) &&
            (yMin <= point.y && point.y <= yMax) &&
            (zMin <= point.z && point.z <= zMax);
    }

    public override int GetHashCode()
    {
        return ShiftAndWrap(xSize.GetHashCode(), 10) ^ ShiftAndWrap(ySize.GetHashCode(), 8) ^ ShiftAndWrap(zSize.GetHashCode(), 6) ^
            ShiftAndWrap(xMin.GetHashCode(), 4) ^ ShiftAndWrap(yMin.GetHashCode(), 2) ^ zMin.GetHashCode();
    }

    /// <summary>
    /// Returns a nicely formatted <see langword="string"/> for this cuboid.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("(x:{0:F2}, y:{1:F2}, z:{2:F2}, width:{3:F2}, height:{4:F2}, depth:{5:F2})"
            , _position.x, _position.y, _position.z, _size.x, _size.y, _size.z);
    }

    public static bool operator ==(Cuboid cuboid1, Cuboid cuboid2) => cuboid1.Equals(cuboid2);
    
    public static bool operator !=(Cuboid cuboid1, Cuboid cuboid2) => !cuboid1.Equals(cuboid2);

    private int ShiftAndWrap(int value, int positions)
    {
        positions = positions & 0x1F;

        // Save the existing bit pattern, but interpret it as an unsigned integer.
        uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
        // Preserve the bits to be discarded.
        uint wrapped = number >> (32 - positions);
        // Shift and wrap the discarded bits.
        return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
    }
}
