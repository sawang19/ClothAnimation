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
    private Vector3 windDirection;    // Wind direction
    private Vector3 initialNormal;
    public Slider windDirectionSlider; // Reference to the Slider

    // Self-collision parameters
    public bool enableSelfCollision = true;
    public bool enableBendingSpring = true;
    public float foldingFactor = 1.0f;
    private float particleRadius = 0.2f;    // Approximate radius of a particle
    private int maxParticlesPerNode = 8;    // Max particles per octree node
    private int maxOctreeDepth = 6;         // Max depth of the octree

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
        }
        if (rightmostIndex != -1)
        {
            particles[rightmostIndex].isPinned = true;
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

        // Add bending springs
        if (enableBendingSpring)
        {
            AddBendingSprings();
        }

    }

    void AddBendingSprings()
    {
        // Create bending springs between particles that are two edges apart
        Dictionary<(int, int), List<int>> edgeToTriangles = new Dictionary<(int, int), List<int>>();

        int[] triangles = mesh.triangles;
        // Build a map of edges to triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int[] tri = new int[] { triangles[i], triangles[i + 1], triangles[i + 2] };

            for (int j = 0; j < 3; j++)
            {
                int a = tri[j];
                int b = tri[(j + 1) % 3];

                var edge = (Mathf.Min(a, b), Mathf.Max(a, b));
                if (!edgeToTriangles.ContainsKey(edge))
                {
                    edgeToTriangles[edge] = new List<int>();
                }
                edgeToTriangles[edge].Add(i / 3);
            }
        }

        // For each edge shared by two triangles, add a bending spring between the opposite vertices
        foreach (var kvp in edgeToTriangles)
        {
            if (kvp.Value.Count == 2)
            {
                int tri1 = kvp.Value[0];
                int tri2 = kvp.Value[1];

                int[] tri1Verts = new int[] { triangles[tri1 * 3], triangles[tri1 * 3 + 1], triangles[tri1 * 3 + 2] };
                int[] tri2Verts = new int[] { triangles[tri2 * 3], triangles[tri2 * 3 + 1], triangles[tri2 * 3 + 2] };

                int sharedA = kvp.Key.Item1;
                int sharedB = kvp.Key.Item2;

                int oppositeA = -1;
                int oppositeB = -1;

                foreach (int v in tri1Verts)
                {
                    if (v != sharedA && v != sharedB)
                    {
                        oppositeA = v;
                        break;
                    }
                }

                foreach (int v in tri2Verts)
                {
                    if (v != sharedA && v != sharedB)
                    {
                        oppositeB = v;
                        break;
                    }
                }

                if (oppositeA != -1 && oppositeB != -1)
                {
                    // Reduce bending stiffness to allow more folding
                    float bendingStiffness = stiffness * foldingFactor;
                    AddSpring(oppositeA, oppositeB, bendingStiffness); // Bending springs are usually softer
                }
            }
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
        }
        else
        {
            // If mesh data is insufficient, use the object's up direction by default
            initialNormal = transform.up;
        }
    }

    void AddSpring(int indexA, int indexB, float springStiffness = -1)
    {
        // Use default stiffness if not specified
        if (springStiffness < 0)
            springStiffness = stiffness;

        // Ensure springs are not added twice
        foreach (var spring in springs)
        {
            if ((spring.particleA == particles[indexA] && spring.particleB == particles[indexB]) ||
                (spring.particleA == particles[indexB] && spring.particleB == particles[indexA]))
            {
                return;
            }
        }
        springs.Add(new Spring(particles[indexA], particles[indexB], springStiffness));
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        // Apply gravity
        Vector3 gravity = new Vector3(0, -gravityStrength, 0);
        foreach (var particle in particles)
        {
            particle.AddForce(gravity);
        }

        // Update particle positions
        foreach (var particle in particles)
        {
            // Reduce damping to allow particles to move more freely
            particle.UpdatePosition(deltaTime, damping: 0.99f);
        }

        // Apply constraints multiple times for better stability
        int constraintIterations = 5;
        for (int iteration = 0; iteration < constraintIterations; iteration++)
        {
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
                        HandleSphereCollision(sphereCollider);
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
                        HandleCapsuleCollision(capsuleCollider);
                    }
                }
            }

            // Handle self-collision
            if (enableSelfCollision)
            {
                HandleSelfCollision();
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
    }

    void HandleSphereCollision(SphereCollider sphereCollider, float elasticity = 0.05f, float friction = 0.2f)
    {
        if (sphereCollider == null)
        {
            Debug.LogError("SphereCollider is null! Please pass a valid SphereCollider.");
            return;
        }

        Vector3 spherePosition = sphereCollider.transform.TransformPoint(sphereCollider.center); // Convert to world coordinates
        float sphereRadius = sphereCollider.radius * Mathf.Max(
            sphereCollider.transform.lossyScale.x,
            sphereCollider.transform.lossyScale.y,
            sphereCollider.transform.lossyScale.z); // Adjust for scale

        float deltaR = 0.02f * sphereRadius;
        float collisionRadius = sphereRadius + deltaR;  // Prevent clipping
        foreach (var particle in particles)
        {
            Vector3 direction = particle.position - spherePosition;
            float distance = direction.magnitude;

            // Check if particle is inside the sphere
            if (distance < collisionRadius)
            {
                // Place particle on the sphere surface
                Vector3 correctedPosition = spherePosition + direction.normalized * collisionRadius;
                particle.position = correctedPosition;

                // Calculate collision force
                Vector3 collisionForce = direction.normalized * elasticity;
                particle.AddForce(collisionForce);
            }

            // Calculate friction force
            Vector3 normal = direction.normalized;
            Vector3 velocity = particle.position - particle.previousPosition;
            Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
            Vector3 frictionForce = -tangentialVelocity * friction;
            particle.AddForce(frictionForce);
        }
    }

    void HandleCapsuleCollision(CapsuleCollider capsuleCollider, float elasticity = 0.05f)
    {
        // Get Transform
        Transform objTransform = capsuleCollider.transform;

        // Get capsule's direction, height, and radius
        Vector3 center = capsuleCollider.center; // Capsule's center (local coordinates)
        float height = capsuleCollider.height;
        float capsuleRadius = capsuleCollider.radius;
        int directionAxis = capsuleCollider.direction; // Main axis direction: 0=X, 1=Y, 2=Z

        // Adjust for scale
        Vector3 lossyScale = objTransform.lossyScale;
        capsuleRadius *= Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
        height *= lossyScale[directionAxis];

        // Calculate offset
        float offset = Mathf.Max(0, (height / 2) - capsuleRadius);

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

        float deltaR = 0.02f * capsuleRadius;
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
                Vector3 correctedPosition = closestPoint + direction.normalized * collisionRadius;
                particle.position = correctedPosition;

                // Calculate and apply collision force
                Vector3 collisionForce = direction.normalized * elasticity;
                particle.AddForce(collisionForce);
            }
        }
    }

    void HandleSelfCollision()
    {
        // Calculate the bounds of all particles
        Bounds octreeBounds = CalculateBounds();

        // Create the octree
        Octree octree = new Octree(octreeBounds, maxParticlesPerNode, maxOctreeDepth);

        // Insert particles into the octree
        foreach (var particle in particles)
        {
            octree.Insert(particle);
        }

        // For each particle, query neighboring particles
        foreach (var particle in particles)
        {
            // Search radius for neighboring particles
            float searchRadius = particleRadius * 2f;

            // Get neighboring particles
            List<Particle> neighbors = octree.Query(particle.position, searchRadius);

            foreach (var neighbor in neighbors)
            {
                if (neighbor == particle)
                    continue;

                // Ignore particles connected by springs
                if (AreParticlesConnected(particle, neighbor))
                    continue;

                // Prevent checking the same pair twice
                if (particle.id >= neighbor.id)
                    continue;

                // Check collision
                Vector3 delta = neighbor.position - particle.position;
                float distanceSquared = delta.sqrMagnitude;

                float minDistance = particleRadius * 1.5f;
                float minDistanceSquared = minDistance * minDistance;

                if (distanceSquared < minDistanceSquared)
                {
                    float distance = Mathf.Sqrt(distanceSquared);
                    // Resolve collision
                    Vector3 correction = delta.normalized * (minDistance - distance) * 0.5f;

                    if (!particle.isPinned && !neighbor.isPinned)
                    {
                        particle.position -= correction;
                        neighbor.position += correction;
                    }
                    else if (!particle.isPinned)
                    {
                        particle.position -= correction * 2f;
                    }
                    else if (!neighbor.isPinned)
                    {
                        neighbor.position += correction * 2f;
                    }
                }
            }
        }
    }

    bool AreParticlesConnected(Particle a, Particle b)
    {
        // Check if particles are connected by a spring
        foreach (var spring in springs)
        {
            if ((spring.particleA == a && spring.particleB == b) ||
                (spring.particleA == b && spring.particleB == a))
            {
                return true;
            }
        }
        return false;
    }

    Bounds CalculateBounds()
    {
        if (particles == null || particles.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds bounds = new Bounds(particles[0].position, Vector3.zero);
        foreach (var particle in particles)
        {
            bounds.Encapsulate(particle.position);
        }
        // Expand bounds slightly to ensure all particles are within
        bounds.Expand(particleRadius * 2f);
        return bounds;
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
            Gizmos.color = Color.green;
            foreach (var particle in particles)
            {
                Gizmos.DrawSphere(particle.position, particleRadius * 0.5f);
            }
        }

        if (springs != null)
        {
            Gizmos.color = Color.white;
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
                    float sphereRadius = sphereCollider.radius * Mathf.Max(
                        sphereCollider.transform.lossyScale.x,
                        sphereCollider.transform.lossyScale.y,
                        sphereCollider.transform.lossyScale.z);
                    Gizmos.DrawWireSphere(spherePosition, sphereRadius);
                }
            }
        }

        // Draw capsule colliders
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

    // Method to draw capsule colliders in Gizmos
    void DrawCapsuleGizmo(CapsuleCollider capsuleCollider)
    {
        Transform objTransform = capsuleCollider.transform;

        Vector3 center = capsuleCollider.center;
        float height = capsuleCollider.height;
        float capsuleRadius = capsuleCollider.radius;
        int directionAxis = capsuleCollider.direction;

        // Adjust for scale
        Vector3 lossyScale = objTransform.lossyScale;
        capsuleRadius *= Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
        height *= lossyScale[directionAxis];

        float offset = Mathf.Max(0, (height / 2) - capsuleRadius);

        Vector3 offsetVector = Vector3.zero;
        if (directionAxis == 0) offsetVector = new Vector3(offset, 0, 0); // X-axis
        else if (directionAxis == 1) offsetVector = new Vector3(0, offset, 0); // Y-axis
        else if (directionAxis == 2) offsetVector = new Vector3(0, 0, offset); // Z-axis

        Vector3 pointA = center + offsetVector;
        Vector3 pointB = center - offsetVector;

        pointA = objTransform.TransformPoint(pointA);
        pointB = objTransform.TransformPoint(pointB);

        // Draw the cylinder part
        Gizmos.DrawLine(pointA + objTransform.right * capsuleRadius, pointB + objTransform.right * capsuleRadius);
        Gizmos.DrawLine(pointA - objTransform.right * capsuleRadius, pointB - objTransform.right * capsuleRadius);
        Gizmos.DrawLine(pointA + objTransform.forward * capsuleRadius, pointB + objTransform.forward * capsuleRadius);
        Gizmos.DrawLine(pointA - objTransform.forward * capsuleRadius, pointB - objTransform.forward * capsuleRadius);

        // Draw the hemispheres
        Gizmos.DrawWireSphere(pointA, capsuleRadius);
        Gizmos.DrawWireSphere(pointB, capsuleRadius);
    }
}

