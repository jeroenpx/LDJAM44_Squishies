using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HairAnim : MonoBehaviour
{
    public Transform centerPoint;

    // Initial information...
    Quaternion startRot;
    Vector3 myUpwards;
    Vector3 myRightwards;

    const float ANGLE = 70;
    const float TIME = 1f;
    const float ACCELERATIONFACTOR = 1f;
    const float HAIRKEEPANGLEFACTOR = .01f;
    const float WINDEFFECT = .2f;

    // We need the second derivative...
    Vector3 lastPosition;
    Vector3 lastSpeed;
    float angleGoal = 0;
    float hairAnglePercent = 0;
    float currentAccelerationChangeSpeed = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // Get initial rotation & "pseudo" up & side vectors (guess for hair up and side)
        startRot = transform.localRotation;
        myUpwards = transform.parent.InverseTransformDirection(transform.position - centerPoint.position).normalized;
        myRightwards = new Vector3(myUpwards.y, -myUpwards.x, 0f);

        // Track changes in speed!
        lastPosition = transform.position;
        lastSpeed = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        float inverse = transform.lossyScale.x>0?1:-1;

        // Show the "points right" ray
        //Debug.DrawRay(transform.position, transform.parent.TransformDirection(myRightwards), Color.yellow);

        // Track changes in speed...
        Vector3 position = transform.position;
        Vector3 speed = (position - lastPosition)/Time.deltaTime;
        Vector3 acceleration = speed - lastSpeed;
        lastPosition = position;
        lastSpeed = speed;

        // Hair goes back to normal
        angleGoal = 0f;// angleGoal * (1-(1- HAIRKEEPANGLEFACTOR)*Time.deltaTime);

        // Apply acceleration to hair
        //hairAnglePercent = -Vector3.Dot(transform.parent.InverseTransformDirection(speed), myRightwards);
        //Mathf.SmoothDamp(hairAnglePercent, Mathf.Clamp01(Vector3.Dot(acceleration, myRightwards) * ACCELERATIONFACTOR), ref currentAccelerationChangeSpeed, .01f);
        angleGoal += Mathf.Clamp(Vector3.Dot(transform.parent.InverseTransformVector(acceleration), myRightwards), -1f, 1f);
        angleGoal = Mathf.Clamp(angleGoal, -1f, 1f);
        hairAnglePercent = Mathf.SmoothDamp(hairAnglePercent, angleGoal*10f, ref currentAccelerationChangeSpeed, .2f);
        //Mathf.SmoothDamp(hairAnglePercent, angleGoal, ref currentAccelerationChangeSpeed, .01f);
        hairAnglePercent = Mathf.Clamp(hairAnglePercent, -1f, 1f);
        //Debug.DrawRay(transform.position, transform.parent.TransformDirection(myRightwards)* hairAnglePercent, Color.yellow);

        float flipWind = Mathf.Sign(centerPoint.position.y - position.y);

        // Update position based on squishy...
        transform.localRotation = startRot * Quaternion.AngleAxis(inverse*(hairAnglePercent+ flipWind*WINDEFFECT * Mathf.Sin((Time.time*Mathf.PI)/TIME)) * ANGLE, Vector3.forward);
    }
}
