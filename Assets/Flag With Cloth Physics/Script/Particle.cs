using UnityEngine;

public class Particle
{
    public Vector3 position;
    public Vector3 previousPosition;
    public Vector3 acceleration;
    public bool isPinned;
    public int id; // Unique identifier

    private static int nextId = 0; // Static variable to assign unique IDs

    public Particle(Vector3 initialPosition)
    {
        position = initialPosition;
        previousPosition = initialPosition;
        acceleration = Vector3.zero;
        isPinned = false;
        id = nextId++;
    }

    public void AddForce(Vector3 force)
    {
        if (!isPinned)
        {
            acceleration += force;
        }
    }

    public void UpdatePosition(float deltaTime, float damping = 0.98f)
    {
        if (!isPinned)
        {
            // Verlet integration for more stable cloth simulation
            Vector3 velocity = position - previousPosition;
            velocity *= damping; // Damping
            Vector3 newPosition = position + velocity + acceleration * deltaTime * deltaTime;
            previousPosition = position;
            position = newPosition;
        }

        // Reset acceleration after each frame
        acceleration = Vector3.zero;
    }
}
