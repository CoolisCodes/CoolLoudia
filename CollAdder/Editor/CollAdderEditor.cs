using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for CollAdder to add mesh colliders recursively from the Unity Inspector.
/// </summary>
[CustomEditor(typeof(CollAdder))]
public class CollAdderEditor : Editor
{
    /// <summary>
    /// Draws the default inspector and adds a button to add mesh colliders recursively.
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CollAdder collAdder = (CollAdder)target;
        if (GUILayout.Button("Add Mesh Colliders Recursively"))
        {
            collAdder.AddCollidersFromEditor();
        }
    }
}
