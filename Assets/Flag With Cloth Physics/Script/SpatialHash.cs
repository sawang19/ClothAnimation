using UnityEngine;
using System.Collections.Generic;

public class SpatialHash
{
    private Dictionary<Vector3Int, List<Particle>> cells;
    private float cellSize;

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        cells = new Dictionary<Vector3Int, List<Particle>>();
    }

    // Hash function to map a position to a cell coordinate
    private Vector3Int HashPosition(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    // Insert a particle into the spatial hash
    public void Insert(Particle particle)
    {
        Vector3Int cell = HashPosition(particle.position);

        if (!cells.ContainsKey(cell))
        {
            cells[cell] = new List<Particle>();
        }
        cells[cell].Add(particle);
    }

    // Query neighboring particles within a certain radius
    public List<Particle> QueryNeighbors(Particle particle, float searchRadius)
    {
        List<Particle> neighbors = new List<Particle>();
        int searchRange = Mathf.CeilToInt(searchRadius / cellSize);

        Vector3Int cell = HashPosition(particle.position);

        // Iterate over neighboring cells
        for (int x = -searchRange; x <= searchRange; x++)
        {
            for (int y = -searchRange; y <= searchRange; y++)
            {
                for (int z = -searchRange; z <= searchRange; z++)
                {
                    Vector3Int neighborCell = new Vector3Int(cell.x + x, cell.y + y, cell.z + z);
                    if (cells.ContainsKey(neighborCell))
                    {
                        foreach (var neighborParticle in cells[neighborCell])
                        {
                            if (neighborParticle != particle)
                            {
                                neighbors.Add(neighborParticle);
                            }
                        }
                    }
                }
            }
        }

        return neighbors;
    }

    // Clear the spatial hash for the next frame
    public void Clear()
    {
        cells.Clear();
    }
}
