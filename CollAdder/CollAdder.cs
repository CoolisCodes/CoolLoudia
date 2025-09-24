using UnityEngine;

/// <summary>
/// Adds mesh colliders recursively to all children with a MeshFilter.
/// </summary>
public class CollAdder : MonoBehaviour
{
    /// <summary>
    /// Recursively adds MeshCollider to all children (including self) that have a MeshFilter.
    /// </summary>
    /// <param name="obj">Transform to start recursion from.</param>
    void AddMeshCollidersRecursively(Transform obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf != null && obj.GetComponent<MeshCollider>() == null)
        {
            obj.gameObject.AddComponent<MeshCollider>();
        }

        foreach (Transform child in obj)
        {
            AddMeshCollidersRecursively(child);
        }
    }

    /// <summary>
    /// Public method to add mesh colliders from editor.
    /// </summary>
    public void AddCollidersFromEditor()
    {
        AddMeshCollidersRecursively(transform);
    }
}
