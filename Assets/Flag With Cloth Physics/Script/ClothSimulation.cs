using UnityEngine;
using System.Collections.Generic;
using System.Collections;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ClothSimulation : MonoBehaviour
{   
    public Transform collisionSphere;
    public float sphereRadius = 0.5f;
    //public Transform collisionCapsulePointA, collisionCapsulePointB;
    public Vector3 pointA, pointB;
    public float capsuleRadius = 0.02f;
    public float gravityStrength = 9.81f;
    public float stiffness = 0.5f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Particle[] particles;
    private List<Spring> springs = new List<Spring>();

    // 风力参数
    public float windStrength = 1000f; // 风的强度
    // 保存初始法线方向
    private Vector3 initialNormal;

    void Start()
    {
        pointA = new Vector3(0, 0f, 0);
        pointB = new Vector3(0, 3f, 0);
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        InitializeParticles();
        InitializeSprings();
        CalculateInitialNormal();
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

    void CalculateInitialNormal()
    {
        // 获取网格数据
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        if (triangles.Length >= 3)
        {
            // 获取第一个三角形的三个顶点，并转换为世界坐标
            Vector3 p0 = transform.TransformPoint(vertices[triangles[0]]);
            Vector3 p1 = transform.TransformPoint(vertices[triangles[1]]);
            Vector3 p2 = transform.TransformPoint(vertices[triangles[2]]);

            // 计算法线向量
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            // 保存初始法线方向
            initialNormal = normal;

            Debug.Log("初始法线方向：" + initialNormal);
        }
        else
        {
            // 如果网格数据不足，默认使用物体的上方向
            initialNormal = transform.up;
            Debug.LogWarning("无法计算初始法线方向，使用默认值：" + initialNormal);
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
            // handleSphereCollisionSimple();
            handleSphereCollision();
        }
        handleCapsuleCollision();
        // 更新网格顶点
        UpdateMesh();
    }

    public void ApplyWindForce()
    {
        StartCoroutine(ApplyWindForDuration(2f)); // 持续施加风力 2 秒
    }

    private IEnumerator ApplyWindForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 计算风力向量
            Vector3 windForce = initialNormal.normalized * windStrength;

            // 对每个粒子施加风力
            foreach (var particle in particles)
            {
                particle.AddForce(windForce);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }


    void handleSphereCollisionSimple()
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

    void handleSphereCollision(float elasticity = 0.05f, float friction = 0.2f)
    {
        Vector3 spherePosition = collisionSphere.position;
        float deltaR = 0.1f;
        float collisionRadius = sphereRadius + deltaR;  // 避免穿模
        foreach (var particle in particles)
        {   
            
            Vector3 direction = particle.position - spherePosition;
            float distance = direction.magnitude;

            // 检查粒子是否在球体内部
            if (distance < collisionRadius)
            {
                // 将粒子放置在球体表面
                particle.position = spherePosition + direction.normalized * collisionRadius;
                Vector3 collisionForce = direction.normalized * elasticity;
                particle.AddForce(collisionForce);
            }

            // friction force
            Vector3 normal = direction.normalized;
            Vector3 velocity = particle.position - particle.previousPosition;
            Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
            Vector3 frictionForce = -tangentialVelocity.normalized * tangentialVelocity.magnitude * friction;
            particle.AddForce(frictionForce);
        }
    }

    void handleCapsuleCollision(float elasticity = 0.05f)
    {
        //Vector3 pointA = collisionCapsulePointA.position;
        //Vector3 pointB = collisionCapsulePointB.position;
        float deltaR = 0.01f;
        float collisionRadius = capsuleRadius + deltaR;  // 避免穿模

        foreach (var particle in particles)
        {
            // 计算粒子到胶囊轴线段的最近点
            Vector3 particlePosition = particle.position;
            Vector3 capsuleDirection = pointB - pointA;
            float capsuleLength = capsuleDirection.magnitude;
            capsuleDirection.Normalize();

            // 将粒子位置投影到胶囊的轴线上，找到最近点
            float t = Vector3.Dot(particlePosition - pointA, capsuleDirection);
            t = Mathf.Clamp(t, 0, capsuleLength); // 限制 t 在 [0, capsuleLength] 之间
            Vector3 closestPoint = pointA + capsuleDirection * t;

            // 检查粒子是否在胶囊体内部
            Vector3 direction = particlePosition - closestPoint;
            float distance = direction.magnitude;

            if (distance < collisionRadius)
            {
                // 将粒子放置在胶囊表面
                particle.position = closestPoint + direction.normalized * collisionRadius;

                // 计算并施加碰撞力
                Vector3 collisionForce = direction.normalized * elasticity;
                particle.AddForce(collisionForce);
                // Debug.Log("Collision:" + particlePosition.ToString());
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

        /*
        if (collisionCapsulePointA != null && collisionCapsulePointB != null)
        {
            Gizmos.color = Color.blue;
            ;

        }*/
    }

    
}
