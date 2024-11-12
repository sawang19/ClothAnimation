using UnityEngine;

public class Particle
{
    public Vector3 position;
    public Vector3 previousPosition;
    public Vector3 acceleration;
    public bool isPinned;

    public Particle(Vector3 initialPosition)
    {
        position = initialPosition;
        previousPosition = initialPosition;
        acceleration = Vector3.zero;
        isPinned = false;
    }

    public void AddForce(Vector3 force)
    {
        if (!isPinned)
        {
            acceleration += force;
        }
    }

    public void UpdatePosition(float deltaTime)
    {
        if (isPinned) return;

        // Verlet integration for more stable cloth simulation
        Vector3 newPosition = position + (position - previousPosition) + acceleration * deltaTime * deltaTime;
        previousPosition = position;
        position = newPosition;

        // Reset acceleration after each frame
        acceleration = Vector3.zero;
    }
}
