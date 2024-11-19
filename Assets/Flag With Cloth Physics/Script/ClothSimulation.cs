using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ClothSimulation : MonoBehaviour
{
    // Arrays to hold multiple sphere and capsule colliders
    public SphereCollider[] sphereColliders;
    public CapsuleCollider[] capsuleColliders;

    public float gravityStrength = 9.81f;
    public float stiffness = 0.5f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Particle[] particles;
    private List<Spring> springs = new List<Spring>();

    // Wind parameters
    public float windStrength = 1000f; // Wind strength
    // To save the initial normal direction
    private Vector3 windDirection;    // Wind direction (calculated from initialNormal and slider)
    private Vector3 initialNormal;
    public Slider windDirectionSlider; // Reference to the Slider

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        InitializeParticles();
        InitializeSprings();
        CalculateInitialNormal();

        // Set initial wind direction
        windDirection = initialNormal;

        // Initialize Slider value and add listener
        if (windDirectionSlider != null)
        {
            windDirectionSlider.value = 0.5f; // Middle position (no horizontal offset)
            windDirectionSlider.onValueChanged.AddListener(UpdateWindDirection);
        }
    }

    void InitializeParticles()
    {
        particles = new Particle[originalVertices.Length];
        float leftmostX = float.MaxValue;
        float rightmostX = float.MinValue;
        int leftmostIndex = -1;
        int rightmostIndex = -1;

        // Create particles and find the leftmost and rightmost vertices
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 worldPosition = transform.TransformPoint(originalVertices[i]);
            particles[i] = new Particle(worldPosition);

            // Find the leftmost and rightmost vertices
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

        // Pin the leftmost and rightmost vertices
        if (leftmostIndex != -1)
        {
            particles[leftmostIndex].isPinned = true;
            // Debug.Log("Leftmost particle pinned at: " + particles[leftmostIndex].position);
        }
        if (rightmostIndex != -1)
        {
            particles[rightmostIndex].isPinned = true;
            // Debug.Log("Rightmost particle pinned at: " + particles[rightmostIndex].position);
        }
    }

    void InitializeSprings()
    {
        int[] triangles = mesh.triangles;

        // Iterate over triangles; each triangle consists of three vertices
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
        // Get mesh data
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        if (triangles.Length >= 3)
        {
            // Get the first triangle's vertices and convert to world coordinates
            Vector3 p0 = transform.TransformPoint(vertices[triangles[0]]);
            Vector3 p1 = transform.TransformPoint(vertices[triangles[1]]);
            Vector3 p2 = transform.TransformPoint(vertices[triangles[2]]);

            // Calculate the normal vector
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;

            // Save the initial normal direction
            initialNormal = normal;

            Debug.Log("Initial normal direction: " + initialNormal);
        }
        else
        {
            // If mesh data is insufficient, use the object's up direction by default
            initialNormal = transform.up;
            Debug.LogWarning("Unable to calculate initial normal direction, using default value: " + initialNormal);
        }
    }

    void AddSpring(int indexA, int indexB)
    {
        // Ensure springs are not added twice
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
        // Debug.Log($"Update is running on GameObject: {gameObject.name}");

        // Apply gravity
        Vector3 gravity = new Vector3(0, -gravityStrength, 0);
        foreach (var particle in particles)
        {
            particle.AddForce(gravity);
        }

        // Update particle positions
        foreach (var particle in particles)
        {
            particle.UpdatePosition(deltaTime);
        }

        // Apply spring constraints
        foreach (var spring in springs)
        {
            spring.ApplyConstraint();
        }

        // Handle collisions with sphere colliders
        if (sphereColliders != null)
        {
            foreach (var sphereCollider in sphereColliders)
            {
                if (sphereCollider != null)
                {
                    handleSphereCollision(sphereCollider);
                }
            }
        }

        // Handle collisions with capsule colliders
        if (capsuleColliders != null)
        {
            foreach (var capsuleCollider in capsuleColliders)
            {
                if (capsuleCollider != null)
                {
                    handleCapsuleCollision(capsuleCollider);
                }
            }
        }

        // Update mesh vertices
        UpdateMesh();
    }



    public void ApplyWindForce()
    {
        StartCoroutine(ApplyWindForDuration(2f)); // Apply wind force for 2 seconds
    }

    private IEnumerator ApplyWindForDuration(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Calculate wind force vector
            Vector3 windForce = windDirection.normalized * windStrength;

            // Apply wind force to each particle
            foreach (var particle in particles)
            {
                particle.AddForce(windForce);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void UpdateWindDirection(float sliderValue)
{
    // Map slider value (0 to 1) to horizontal offset (-1 to 1)
    float horizontalOffset = (sliderValue - 0.5f) * 2f;

    // Combine horizontal offset with a fixed vertical component
    windDirection = new Vector3(horizontalOffset, 0, Mathf.Sqrt(1 - horizontalOffset * horizontalOffset));

    Debug.Log($"Wind Direction Updated: {windDirection}");
}


    void handleSphereCollision(SphereCollider sphereCollider, float elasticity = 0.05f, float friction = 0.2f)
    {
        if (sphereCollider == null)
        {
            Debug.LogError("SphereCollider is null! Please pass a valid SphereCollider.");
            return;
        }

        Vector3 spherePosition = sphereCollider.transform.TransformPoint(sphereCollider.center); // Convert to world coordinates
        float sphereRadius = sphereCollider.radius;

        float deltaR = 0.2f * sphereRadius;
        float collisionRadius = sphereRadius + deltaR;  // Prevent clipping
        foreach (var particle in particles)
        {
            Vector3 direction = particle.position - spherePosition;
            float distance = direction.magnitude;

            // Check if particle is inside the sphere
            if (distance < collisionRadius)
            {
                // Place particle on the sphere surface
                particle.position = spherePosition + direction.normalized * collisionRadius;

                // Calculate collision force
                Vector3 collisionForce = direction.normalized * elasticity;
                particle.AddForce(collisionForce);
            }

            // Calculate friction force
            Vector3 normal = direction.normalized;
            Vector3 velocity = particle.position - particle.previousPosition;
            Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
            Vector3 frictionForce = -tangentialVelocity.normalized * tangentialVelocity.magnitude * friction;
            particle.AddForce(frictionForce);
        }
    }

    void handleCapsuleCollision(CapsuleCollider capsuleCollider, float elasticity = 0.05f)
    {
        // Get Transform
        Transform objTransform = capsuleCollider.transform;

        // Get capsule's direction, height, and radius
        Vector3 center = capsuleCollider.center; // Capsule's center (local coordinates)
        float height = capsuleCollider.height;
        float capsuleRadius = capsuleCollider.radius;
        int directionAxis = capsuleCollider.direction; // Main axis direction: 0=X, 1=Y, 2=Z

        // Calculate offset
        float offset = (height / 2) - capsuleRadius;

        // Calculate offset vector based on direction
        Vector3 offsetVector = Vector3.zero;
        if (directionAxis == 0) offsetVector = new Vector3(offset, 0, 0); // X-axis
        else if (directionAxis == 1) offsetVector = new Vector3(0, offset, 0); // Y-axis
        else if (directionAxis == 2) offsetVector = new Vector3(0, 0, offset); // Z-axis

        // Calculate the centers of the two hemispherical ends (local coordinates)
        Vector3 pointA = center + offsetVector; // Top hemisphere center
        Vector3 pointB = center - offsetVector; // Bottom hemisphere center

        // Convert to world coordinates
        pointA = objTransform.TransformPoint(pointA);
        pointB = objTransform.TransformPoint(pointB);
        Debug.Log("Point A (Top Hemisphere Center): " + pointA);
        Debug.Log("Point B (Bottom Hemisphere Center): " + pointB);
        float deltaR = 0.2f * capsuleRadius;
        float collisionRadius = capsuleRadius + deltaR;  // Prevent clipping

        foreach (var particle in particles)
        {
            // Calculate the closest point on the capsule line segment to the particle
            Vector3 particlePosition = particle.position;
            Vector3 capsuleDirection = pointB - pointA;
            float capsuleLength = capsuleDirection.magnitude;
            capsuleDirection.Normalize();

            // Project the particle position onto the capsule's axis to find the closest point
            float t = Vector3.Dot(particlePosition - pointA, capsuleDirection);
            t = Mathf.Clamp(t, 0, capsuleLength); // Clamp t between [0, capsuleLength]
            Vector3 closestPoint = pointA + capsuleDirection * t;

            // Check if particle is inside the capsule
            Vector3 direction = particlePosition - closestPoint;
            float distance = direction.magnitude;

            if (distance < collisionRadius)
            {
                // Place particle on the capsule surface
                particle.position = closestPoint + direction.normalized * collisionRadius;

                // Calculate and apply collision force
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

        // Draw sphere colliders
        if (sphereColliders != null)
        {
            Gizmos.color = Color.red;
            foreach (var sphereCollider in sphereColliders)
            {
                if (sphereCollider != null)
                {
                    Vector3 spherePosition = sphereCollider.transform.TransformPoint(sphereCollider.center);
                    float sphereRadius = sphereCollider.radius;
                    Gizmos.DrawWireSphere(spherePosition, sphereRadius);
                }
            }
        }

        // Draw capsule colliders (optional)
        if (capsuleColliders != null)
        {
            Gizmos.color = Color.blue;
            foreach (var capsuleCollider in capsuleColliders)
            {
                if (capsuleCollider != null)
                {
                    DrawCapsuleGizmo(capsuleCollider);
                }
            }
        }
    }

    // Optional method to draw capsule colliders in Gizmos
    void DrawCapsuleGizmo(CapsuleCollider capsuleCollider)
    {
        Transform objTransform = capsuleCollider.transform;

        Vector3 center = capsuleCollider.center;
        float height = capsuleCollider.height;
        float capsuleRadius = capsuleCollider.radius;
        int directionAxis = capsuleCollider.direction;

        float offset = (height / 2) - capsuleRadius;

        Vector3 offsetVector = Vector3.zero;
        if (directionAxis == 0) offsetVector = new Vector3(offset, 0, 0); // X-axis
        else if (directionAxis == 1) offsetVector = new Vector3(0, offset, 0); // Y-axis
        else if (directionAxis == 2) offsetVector = new Vector3(0, 0, offset); // Z-axis

        Vector3 pointA = center + offsetVector;
        Vector3 pointB = center - offsetVector;

        pointA = objTransform.TransformPoint(pointA);
        pointB = objTransform.TransformPoint(pointB);

        // Draw the cylinder part
        Gizmos.DrawLine(pointA + Vector3.right * capsuleRadius, pointB + Vector3.right * capsuleRadius);
        Gizmos.DrawLine(pointA - Vector3.right * capsuleRadius, pointB - Vector3.right * capsuleRadius);
        Gizmos.DrawLine(pointA + Vector3.forward * capsuleRadius, pointB + Vector3.forward * capsuleRadius);
        Gizmos.DrawLine(pointA - Vector3.forward * capsuleRadius, pointB - Vector3.forward * capsuleRadius);

        // Draw the hemispheres
        Gizmos.DrawWireSphere(pointA, capsuleRadius);
        Gizmos.DrawWireSphere(pointB, capsuleRadius);
    }
}
