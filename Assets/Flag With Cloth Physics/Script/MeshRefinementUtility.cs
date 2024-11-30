=using UnityEngine;
using System.Collections.Generic;

public static class MeshRefinementUtility
{
    public static Mesh SubdivideMesh(Mesh originalMesh, int levels)
    {
        for (int i = 0; i < levels; i++)
        {
            originalMesh = Subdivide(originalMesh);
        }
        return originalMesh;
    }

    private static Mesh Subdivide(Mesh originalMesh)
    {
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector2[] uvs = originalMesh.uv; // Get original UV data

        List<Vector3> newVertices = new List<Vector3>(vertices);
        List<Vector2> newUVs = new List<Vector2>(uvs);
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            // Calculate the midpoints of the three edges
            Vector3 mid01 = (vertices[v0] + vertices[v1]) / 2f;
            Vector3 mid12 = (vertices[v1] + vertices[v2]) / 2f;
            Vector3 mid20 = (vertices[v2] + vertices[v0]) / 2f;

            // Interpolate UV data
            Vector2 uvMid01 = (uvs[v0] + uvs[v1]) / 2f;
            Vector2 uvMid12 = (uvs[v1] + uvs[v2]) / 2f;
            Vector2 uvMid20 = (uvs[v2] + uvs[v0]) / 2f;

            // Add new vertices and UV data
            int mid01Index = AddVertex(mid01, newVertices, uvMid01, newUVs);
            int mid12Index = AddVertex(mid12, newVertices, uvMid12, newUVs);
            int mid20Index = AddVertex(mid20, newVertices, uvMid20, newUVs);

            // Create new triangles
            newTriangles.Add(v0);
            newTriangles.Add(mid01Index);
            newTriangles.Add(mid20Index);

            newTriangles.Add(mid01Index);
            newTriangles.Add(v1);
            newTriangles.Add(mid12Index);

            newTriangles.Add(mid12Index);
            newTriangles.Add(v2);
            newTriangles.Add(mid20Index);

            newTriangles.Add(mid01Index);
            newTriangles.Add(mid12Index);
            newTriangles.Add(mid20Index);
        }

        // Create new Mesh
        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        newMesh.uv = newUVs.ToArray(); // Update UV data
        newMesh.RecalculateNormals();

        return newMesh;
    }

    private static int AddVertex(Vector3 vertex, List<Vector3> vertices, Vector2 uv, List<Vector2> uvs)
    {
        // Add new vertex and corresponding UV
        for (int i = 0; i < vertices.Count; i++)
        {
            if (Vector3.Distance(vertices[i], vertex) < 0.001f)
                return i;
        }

        vertices.Add(vertex);
        uvs.Add(uv); // Add UV data simultaneously
        return vertices.Count - 1;
    }

}
