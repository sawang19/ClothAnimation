using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ClothSimulation : MonoBehaviour
{
    // === Public Fields ===

    // Collision Colliders
    public SphereCollider[] sphereColliders;
    public CapsuleCollider[] capsuleColliders;

    // Physics Parameters
    public float gravityStrength = 9.81f;
    public float stiffness = 0.5f;

    // Wind Parameters
    public float windStrength = 10f; // Wind strength
    public float dragCoefficient = 0.01f; 
    public float liftCoefficient = 0.2f; 
    public float verticalOffset = 0.3f; 

    // UI Elements
    public Toggle windToggle;
    public Toggle xWindToggle;        
    public Slider windStrengthSlider; 

    // Self-Collision Parameters
    public bool enableSelfCollision = true;
    public bool enableBendingSpring = true;
    public float foldingFactor = 1.0f;

    // === Private Fields ===

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Particle[] particles;
    private List<Spring> springs = new List<Spring>();

    private Vector3 windDirection;    // Wind direction
    private Vector3 initialNormal;

    private float particleRadius = 0.10f;    // Approximate radius of a particle
    private int maxParticlesPerNode = 8;      // Max particles per octree node
    private int maxOctreeDepth = 6;           // Max depth of the octree

    // === Unity Lifecycle Methods ===

    void Start()
    {
        InitializeMesh();
        InitializeParticles();
        InitializeSprings();
        CalculateInitialNormal();
        Debug.Log($"Springs: {springs.Count}");
        
        // Set initial wind direction
        windDirection = initialNormal;

        // Setup UI Listeners
        SetupUIListeners();
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        // Apply Forces
        ApplyForces(deltaTime);

        // Update Particle Positions
        UpdateParticlePositions(deltaTime);

        // Apply Constraints
        ApplyConstraints();

        // Update Mesh
        UpdateMesh();
    }

    // === Initialization Methods ===

    void InitializeMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        mesh = MeshRefinementUtility.SubdivideMesh(mesh, 0);
        GetComponent<MeshFilter>().mesh = mesh;
        if (meshRenderer != null && meshRenderer.material == null)
        {
            Debug.Log("No material!");
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Debug.Log($"Vertices: {mesh.vertexCount}");
        Debug.Log($"UVs: {mesh.uv.Length}");
        Debug.Log($"Triangles: {mesh.triangles.Length / 3}");
        originalVertices = mesh.vertices;
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

    void SetupUIListeners()
    {
        if (windToggle != null)
        {
            windToggle.onValueChanged.AddListener(OnWindToggleChanged);
        }

        if (xWindToggle != null)
        {
            xWindToggle.onValueChanged.AddListener(OnXWindToggleChanged);
        }

        if (windStrengthSlider != null)
        {
            windStrengthSlider.onValueChanged.AddListener(UpdateWindStrength);
            UpdateWindStrength(windStrengthSlider.value);
        }
    }

    // === Wind-Related Methods ===

    void OnWindToggleChanged(bool isOn)
    {
        if (isOn)
        {
            Debug.Log("Wind has been turned ON.");
            windDirection = initialNormal;
            if (xWindToggle != null && xWindToggle.isOn)
            {
                xWindToggle.isOn = false;
            }
        }
        else
        {
            Debug.Log("Wind has been turned OFF.");
            if (!xWindToggle.isOn) 
            {
                windDirection = Vector3.zero;
            }
        }
    }

    void OnXWindToggleChanged(bool isOn)
    {
        if (isOn)
        {
            Debug.Log("X-axis wind turned ON.");
            windDirection = new Vector3(1, 0, 0.5f).normalized;
            if (windToggle != null && windToggle.isOn)
            {
                windToggle.isOn = false;
            }
        }
        else
        {
            Debug.Log("X-axis wind turned OFF.");
            if (!windToggle.isOn)
            {
                windDirection = Vector3.zero;
            }
        }
    }

    void UpdateWindStrength(float value)
    {
        float minWindStrength = 0f;
        float maxWindStrength = 20f;

        windStrength = Mathf.Lerp(minWindStrength, maxWindStrength, value);
    }

    void ApplyForces(float deltaTime)
    {
        CalculateParticleNormals();

        // Apply gravity
        Vector3 gravity = new Vector3(0, -gravityStrength, 0);
        foreach (var particle in particles)
        {
            particle.AddForce(gravity);
        }

        // Apply wind forces
        if (windDirection != Vector3.zero && windStrength > 0f)
        {
            ApplyAerodynamicForces();
            ApplyBaseWindForce();
        }
    }

    void ApplyBaseWindForce()
    {
        foreach (var particle in particles)
        {
            if (!particle.isPinned) 
            {
                Vector3 baseWindForce = windDirection * windStrength;
                particle.AddForce(baseWindForce);
            }
        }
    }

    void ApplyAerodynamicForces()
    {
        foreach (var particle in particles)
        {
            Vector3 particleVelocity = particle.GetVelocity();

            Vector3 relativeWind = windDirection * windStrength - particleVelocity;

            if (relativeWind == Vector3.zero || particle.normal == Vector3.zero)
                continue;

            Vector3 relativeWindDir = relativeWind.normalized;
            Vector3 normal = particle.normal.normalized;

            Vector3 dragForce = -dragCoefficient * relativeWindDir * relativeWind.sqrMagnitude;
            Vector3 liftForce = liftCoefficient * Vector3.Cross(relativeWindDir, normal) * relativeWind.sqrMagnitude;

            Debug.Log($"Wind: {windDirection * windStrength}, Wind direction: {relativeWindDir}, Drag Force: {dragForce}, Lift Force: {liftForce}");

            particle.AddForce(dragForce + liftForce);
        }
    }

    // === Spring-Related Methods ===

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

    // === Collision-Related Methods ===

    void ApplyConstraints()
    {
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

                // Calculate friction force
                Vector3 normal = direction.normalized;
                Vector3 velocity = particle.position - particle.previousPosition;
                Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
                Vector3 frictionForce = -tangentialVelocity * friction;
                particle.AddForce(frictionForce);
            }
        }
    }

    void HandleCapsuleCollision(CapsuleCollider capsuleCollider, float elasticity = 0.05f, float friction = 0.2f)
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
        Vector3 pointA = center + offsetVector;
        Vector3 pointB = center - offsetVector;

        // Convert to world coordinates
        pointA = objTransform.TransformPoint(pointA);
        pointB = objTransform.TransformPoint(pointB);

        float deltaR = 0.02f * capsuleRadius;
        float collisionRadius = capsuleRadius + deltaR; // Prevent clipping

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

                // Calculate and apply friction force
                Vector3 normal = direction.normalized;
                Vector3 velocity = particle.position - particle.previousPosition;
                Vector3 tangentialVelocity = velocity - Vector3.Dot(velocity, normal) * normal;
                Vector3 frictionForce = -tangentialVelocity * friction;
                particle.AddForce(frictionForce);
            }
        }
    }


    void HandleSelfCollision()
    {
        // Calculate the bounds of all particles
        Bounds octreeBounds = CalculateBounds();

        // Create the spatial hash
        SpatialHash spatialHash = new SpatialHash(particleRadius * 2.5f);

        // Insert particles into the spatial hash
        foreach (var particle in particles)
        {
            spatialHash.Insert(particle);
        }

        // For each particle, query neighboring particles
        foreach (var particle in particles)
        {
            // Search radius for neighboring particles
            float searchRadius = particleRadius * 2f;

            // Get neighboring particles
            List<Particle> neighbors = spatialHash.Query(particle.position, searchRadius);

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

    // === Update Particle Positions ===

    void UpdateParticlePositions(float deltaTime)
    {
        // Update particle positions with damping
        foreach (var particle in particles)
        {
            particle.UpdatePosition(deltaTime, damping: 0.98f);
        }
    }

    // === Helper Methods ===

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

    // === Normal Calculation ===

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

    void CalculateParticleNormals()
    {
        // Initialize normals
        foreach (var particle in particles)
        {
            particle.normal = Vector3.zero;
        }

        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int indexA = triangles[i];
            int indexB = triangles[i + 1];
            int indexC = triangles[i + 2];

            Vector3 pA = particles[indexA].position;
            Vector3 pB = particles[indexB].position;
            Vector3 pC = particles[indexC].position;

            Vector3 normal = Vector3.Cross(pB - pA, pC - pA).normalized;

            particles[indexA].normal += normal;
            particles[indexB].normal += normal;
            particles[indexC].normal += normal;
        }

        // Normalize all normals
        foreach (var particle in particles)
        {
            particle.normal.Normalize();
        }
    }

    // === Mesh Update Methods ===

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

    // === Gizmos Methods ===

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
