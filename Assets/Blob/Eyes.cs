using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eyes : MonoBehaviour
{
    public Transform leftUp;
    public Transform rightUp;
    public bool flip = true;

    private Vector3 lastUpDirection = Vector3.zero;
    private Vector3 upDirectionVelocity;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Get the up direction by looking both at leftUp and rightUp (and counting both equally much)
        Vector3 upDirection = ((leftUp.position- transform.position).normalized + (rightUp.position - transform.position).normalized);

        // Experiment: eyes always up...
        if (flip)
        {
            Vector3 maxUpDirection = upDirection;
            float maxUpDirectionWeight = 0f;
            for (int i = 0; i < 4; i++)
            {
                float weight = Vector3.Dot(upDirection, Vector3.up);
                if (weight > maxUpDirectionWeight)
                {
                    maxUpDirectionWeight = weight;
                    maxUpDirection = upDirection;
                }
                upDirection = new Vector3(upDirection.y, -upDirection.x, 0f);
            }
            upDirection = maxUpDirection;


            // SmoothFlip...
            if (lastUpDirection == Vector3.zero)
            {
                lastUpDirection = upDirection;
            }

            lastUpDirection = Vector3.SmoothDamp(lastUpDirection, upDirection, ref upDirectionVelocity, 0.1f);
        } else
        {
            lastUpDirection = upDirection;
        }

        // Get the rotation
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, lastUpDirection);
        

        // Set the rotation
        transform.rotation = rot;
    }
}
