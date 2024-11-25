using UnityEngine;
using System.Collections.Generic;

public class SpatialHash
{
    private readonly float cellSize;
    private readonly Dictionary<Int3, List<Particle>> grid;

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        this.grid = new Dictionary<Int3, List<Particle>>();
    }

    // 3D integer coordinate structure
    private struct Int3
    {
        public int x, y, z;

        public Int3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Int3)) return false;
            Int3 other = (Int3)obj;
            return x == other.x && y == other.y && z == other.z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                hash = hash * 23 + z.GetHashCode();
                return hash;
            }
        }
    }

    // Get the grid cell coordinates where the particle is located
    private Int3 GetCell(Vector3 position)
    {
        return new Int3(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    // Insert a particle
    public void Insert(Particle particle)
    {
        Int3 cell = GetCell(particle.position);
        if (!grid.TryGetValue(cell, out var cellList))
        {
            cellList = new List<Particle>();
            grid[cell] = cellList;
        }
        cellList.Add(particle);
    }

    // Query neighboring particles
    public List<Particle> Query(Vector3 position, float radius)
    {
        List<Particle> result = new List<Particle>();
        float radiusSquared = radius * radius;

        // Calculate the query range
        Int3 minCell = GetCell(position - new Vector3(radius, radius, radius));
        Int3 maxCell = GetCell(position + new Vector3(radius, radius, radius));

        // Iterate through all possible grids
        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    Int3 cell = new Int3(x, y, z);
                    if (grid.TryGetValue(cell, out var cellParticles))
                    {
                        foreach (var particle in cellParticles)
                        {
                            float distanceSquared = (particle.position - position).sqrMagnitude;
                            if (distanceSquared <= radiusSquared)
                            {
                                result.Add(particle);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // Clear the grid
    public void Clear()
    {
        grid.Clear();
    }
}
