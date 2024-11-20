using UnityEngine;
using System.Collections.Generic;

public class Octree
{
    private class OctreeNode
    {
        // Bounds of this node
        public Bounds bounds;
        // Particles contained in this node (if leaf node)
        private List<Particle> particles;

        // Child nodes
        private OctreeNode[] children;

        // Whether this node is a leaf
        private bool isLeaf;

        // Max particles per node before subdivision
        private int maxParticlesPerNode;

        // Max depth of the tree
        private int maxDepth;

        // Constructor
        public OctreeNode(Bounds bounds, int maxParticlesPerNode, int maxDepth)
        {
            this.bounds = bounds;
            this.maxParticlesPerNode = maxParticlesPerNode;
            this.maxDepth = maxDepth;
            particles = new List<Particle>();
            isLeaf = true;
        }

        // Insert a particle into the node
        public void Insert(Particle particle, int depth)
        {
            // If this node is a leaf node
            if (isLeaf)
            {
                particles.Add(particle);

                // If particle count exceeds max and depth is less than maxDepth, subdivide
                if (particles.Count > maxParticlesPerNode && depth < maxDepth)
                {
                    Subdivide();

                    // Re-insert particles into children
                    foreach (var p in particles)
                    {
                        InsertIntoChildren(p, depth + 1);
                    }

                    // Clear particles from this node
                    particles.Clear();
                    isLeaf = false;
                }
            }
            else
            {
                // Insert into appropriate child
                InsertIntoChildren(particle, depth + 1);
            }
        }

        private void InsertIntoChildren(Particle particle, int depth)
        {
            foreach (var child in children)
            {
                if (child.bounds.Contains(particle.position))
                {
                    child.Insert(particle, depth);
                    return;
                }
            }
            // If particle is not contained in any child (should not happen), insert into first child
            children[0].Insert(particle, depth);
        }

        // Subdivide the node into 8 children
        private void Subdivide()
        {
            children = new OctreeNode[8];

            Vector3 size = bounds.size / 2f;
            Vector3 min = bounds.min;
            Vector3 center = bounds.center;

            // Create 8 child bounds
            for (int i = 0; i < 8; i++)
            {
                Vector3 newCenter = center;
                newCenter.x += size.x * ((i & 1) == 0 ? -0.25f : 0.25f);
                newCenter.y += size.y * ((i & 2) == 0 ? -0.25f : 0.25f);
                newCenter.z += size.z * ((i & 4) == 0 ? -0.25f : 0.25f);

                Bounds childBounds = new Bounds(newCenter, size);
                children[i] = new OctreeNode(childBounds, maxParticlesPerNode, maxDepth);
            }
        }

        // Query particles within a bounds
        public void Query(Bounds queryBounds, List<Particle> result)
        {
            if (!bounds.Intersects(queryBounds))
            {
                // If bounds do not intersect, return
                return;
            }

            if (isLeaf)
            {
                // Check particles in this node
                foreach (var particle in particles)
                {
                    if (queryBounds.Contains(particle.position))
                    {
                        result.Add(particle);
                    }
                }
            }
            else
            {
                // Recurse into children
                foreach (var child in children)
                {
                    child.Query(queryBounds, result);
                }
            }
        }
    }

    // Root node
    private OctreeNode root;

    // Constructor
    public Octree(Bounds bounds, int maxParticlesPerNode, int maxDepth)
    {
        root = new OctreeNode(bounds, maxParticlesPerNode, maxDepth);
    }

    // Insert particle into the octree
    public void Insert(Particle particle)
    {
        root.Insert(particle, 0);
    }

    // Query particles within a radius
    public List<Particle> Query(Vector3 position, float radius)
    {
        List<Particle> result = new List<Particle>();
        Bounds queryBounds = new Bounds(position, new Vector3(radius * 2, radius * 2, radius * 2));
        root.Query(queryBounds, result);
        return result;
    }
}
