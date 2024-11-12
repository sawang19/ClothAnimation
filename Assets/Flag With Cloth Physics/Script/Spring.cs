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

    public void ApplyConstraint()
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
    }
}
