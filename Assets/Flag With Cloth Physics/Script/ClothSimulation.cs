using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ClothSimulation : MonoBehaviour
{
    public Transform collisionSphere;
    public float gravityStrength = 9.81f;
    public float sphereRadius = 0.5f;
    public float stiffness = 0.5f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Particle[] particles;
    private List<Spring> springs = new List<Spring>();

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        InitializeParticles();
        InitializeSprings();
    }

    void InitializeParticles()
{
    particles = new Particle[originalVertices.Length];
    float leftmostX = float.MaxValue;
    float rightmostX = float.MinValue;
    int leftmostIndex = -1;
    int rightmostIndex = -1;

    // 创建粒子并找到左右上角的顶点
    for (int i = 0; i < originalVertices.Length; i++)
    {
        Vector3 worldPosition = transform.TransformPoint(originalVertices[i]);
        particles[i] = new Particle(worldPosition);

        // 查找最左和最右的顶点
        if (worldPosition.x < leftmostX)
        {
            leftmostX = worldPosition.x;
            leftmostIndex = i;
        }
        if (worldPosition.x > rightmostX)
        {
            rightmostX = worldPosition.x;
            rightmostIndex = i;
        }
    }

    // 固定左右上角的顶点
    if (leftmostIndex != -1)
    {
        particles[leftmostIndex].isPinned = true;
        Debug.Log("Leftmost particle pinned at: " + particles[leftmostIndex].position);
    }
    if (rightmostIndex != -1)
    {
        particles[rightmostIndex].isPinned = true;
        Debug.Log("Rightmost particle pinned at: " + particles[rightmostIndex].position);
    }
}


    void InitializeSprings()
    {
        int[] triangles = mesh.triangles;

        // 遍历三角形，每个三角形由三个顶点组成
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            AddSpring(a, b);
            AddSpring(b, c);
            AddSpring(c, a);
        }
    }

    void AddSpring(int indexA, int indexB)
    {
        // 确保不重复添加弹簧
        foreach (var spring in springs)
        {
            if ((spring.particleA == particles[indexA] && spring.particleB == particles[indexB]) ||
                (spring.particleA == particles[indexB] && spring.particleB == particles[indexA]))
            {
                return;
            }
        }
        springs.Add(new Spring(particles[indexA], particles[indexB], stiffness));
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        // 应用重力
        Vector3 gravity = new Vector3(0, -gravityStrength, 0);
        foreach (var particle in particles)
        {
            particle.AddForce(gravity);
        }

        // 更新粒子位置
        foreach (var particle in particles)
        {
            particle.UpdatePosition(deltaTime);
        }

        // 应用弹簧约束
        foreach (var spring in springs)
        {
            spring.ApplyConstraint();
        }

        // 处理与球体的碰撞
        if (collisionSphere != null)
        {
            HandleCollisions();
        }

        // 更新网格顶点
        UpdateMesh();
    }

    void HandleCollisions()
    {
        Vector3 spherePosition = collisionSphere.position;

        foreach (var particle in particles)
        {
            Vector3 direction = particle.position - spherePosition;
            float distance = direction.magnitude;

            if (distance < sphereRadius)
            {
                particle.position = spherePosition + direction.normalized * sphereRadius;
            }
        }
    }

    void UpdateMesh()
    {
        Vector3[] vertices = new Vector3[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            vertices[i] = transform.InverseTransformPoint(particles[i].position);
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void OnDrawGizmos()
    {
        if (particles != null)
        {
            foreach (var particle in particles)
            {
                Gizmos.DrawSphere(particle.position, 0.02f);
            }
        }

        if (springs != null)
        {
            foreach (var spring in springs)
            {
                Gizmos.DrawLine(spring.particleA.position, spring.particleB.position);
            }
        }

        if (collisionSphere != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(collisionSphere.position, sphereRadius);
        }
    }
}
