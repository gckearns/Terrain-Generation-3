using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OctreeController))]
public class OctreeControllerEditor : Editor
{
    // Creates a custom Label on the inspector for all the scripts named ScriptName
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            ((OctreeController)target).Generate();
        }
        if (GUILayout.Button("Update Depth"))
        {
            ((OctreeController)target).UpdateDepth();
        }
    }
}
