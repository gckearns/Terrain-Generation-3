using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewMesh : MonoBehaviour
{
    private MeshFilter _meshFilter;
    public MeshFilter meshFilter { get => _meshFilter == null ? _meshFilter = GetComponent<MeshFilter>() : _meshFilter; }

    private MeshRenderer _meshRenderer;
    public MeshRenderer meshRenderer { get => _meshRenderer == null ? _meshRenderer = GetComponent<MeshRenderer>() : _meshRenderer; }
}
