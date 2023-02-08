using System.Linq;
using UnityEngine;

public class InvertMesh : MonoBehaviour
{
    public MeshFilter meshFilter;

    private void Awake()
    {
        if (!meshFilter) meshFilter = GetComponent<MeshFilter>();

        var mesh = meshFilter.mesh;

        // Reverse the triangles
        mesh.triangles = mesh.triangles.Reverse().ToArray();

        // also invert the normals
        mesh.normals = mesh.normals.Select(n => -n).ToArray();
    }
}
