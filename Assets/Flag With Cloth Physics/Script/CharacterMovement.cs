using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationStateController : MonoBehaviour
{
    Animator animator;
    float velocity = 0.0f;
    public float acceleration = 0.5f;
    public float deceleration = 0.5f;
    int VelocityHash;

    public float rotationSpeed = 5.0f; // Rotation speed when changing direction
    private Quaternion targetRotation; // Target rotation for character

    public float moveSpeed = 2.0f; // Speed at which the character moves

    // Start is called before the first frame update
    void Start()
    {
        // Set reference for animator
        animator = GetComponent<Animator>();

        // Increase performance
        VelocityHash = Animator.StringToHash("Velocity");

        // Initialize target rotation as current rotation
        targetRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Get key input from player
        bool forwardPressed = Input.GetKey("w");
        bool leftPressed = Input.GetKey("a");
        bool backwardPressed = Input.GetKey("s");
        bool rightPressed = Input.GetKey("d");

        // Handle rotation and movement
        if (forwardPressed)
        {
            // Set target rotation to 0, 180, 0 (move forward)
            targetRotation = Quaternion.Euler(0, 180, 0);
            if (velocity < 1.0f)
            {
                velocity += Time.deltaTime * acceleration;
            }
        }
        else if (leftPressed)
        {
            // Set target rotation to 0, 90, 0 (move left)
            targetRotation = Quaternion.Euler(0, 90, 0);
            if (velocity < 1.0f)
            {
                velocity += Time.deltaTime * acceleration;
            }
        }
        else if (backwardPressed)
        {
            // Set target rotation to 0, 0, 0 (move backward)
            targetRotation = Quaternion.Euler(0, 0, 0);
            if (velocity < 1.0f)
            {
                velocity += Time.deltaTime * acceleration;
            }
        }
        else if (rightPressed)
        {
            // Set target rotation to 0, -90, 0 (move right)
            targetRotation = Quaternion.Euler(0, -90, 0);
            if (velocity < 1.0f)
            {
                velocity += Time.deltaTime * acceleration;
            }
        }
        else
        {
            // Decelerate if no input
            if (velocity > 0.0f)
            {
                velocity -= Time.deltaTime * deceleration;
            }
            if (velocity < 0.0f)
            {
                velocity = 0.0f;
            }
        }

        // Smoothly rotate the character towards the target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Move the character forward based on the velocity and current facing direction
        Vector3 moveDirection = transform.forward * velocity * moveSpeed * Time.deltaTime;

        // Apply the movement
        transform.Translate(moveDirection, Space.World);

        // Update the animator with velocity to trigger movement animations
        animator.SetFloat(VelocityHash, velocity);
    }
}
