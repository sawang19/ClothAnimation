using UnityEngine;

public class Spring
{
    public Particle particleA;
    public Particle particleB;
    public float restLength;
    public float stiffness;

    public Spring(Particle a, Particle b, float stiffness)
    {
        particleA = a;
        particleB = b;
        this.stiffness = stiffness;
        restLength = Vector3.Distance(a.position, b.position);
    }

    public void ApplyConstraint(float dampingFactor = 0.02f)
    {
        Vector3 delta = particleB.position - particleA.position;
        float currentLength = delta.magnitude;
        float difference = (currentLength - restLength) / currentLength;
        Vector3 offset = delta * (stiffness * difference);

        if (!particleA.isPinned)
        {
            particleA.position += offset;
        }

        if (!particleB.isPinned)
        {
            particleB.position -= offset;
        }

        // Calculate relative velocity and apply damping force
        Vector3 relativeVelocity = (particleB.position - particleB.previousPosition) -
                                   (particleA.position - particleA.previousPosition);
        // Calculate damping force, opposite to relative velocity
        Vector3 dampingForce = -relativeVelocity * dampingFactor;
        // Apply damping force to particle acceleration
        if (!particleA.isPinned)
        {
            particleA.AddForce(dampingForce * -0.5f);
        }
        if (!particleB.isPinned)
        {
            particleB.AddForce(dampingForce * 0.5f);
        }
    }
}
