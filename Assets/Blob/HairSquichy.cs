using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HairSquichy : MonoBehaviour
{
    public Transform[] cornersClockwise;

    private Transform container;
    private HairAnim[] hairParticles;
    private Vector2[] initialPositionInBlob;
    private Vector3[] initialPositionInBlob2;

    // Start is called before the first frame update
    void Start()
    {
        container = transform.Find("HairContainer");
        hairParticles = transform.GetComponentsInChildren<HairAnim>();
        initialPositionInBlob = new Vector2[hairParticles.Length];
        initialPositionInBlob2 = new Vector3[hairParticles.Length];
        for (int i = 0;i<hairParticles.Length;i++)
        {
            HairAnim particle = hairParticles[i];
            initialPositionInBlob[i] = InverseTransformSquich(particle.transform.position);
            initialPositionInBlob2[i] = container.InverseTransformPoint(particle.transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Assume the container is rotated correctly (see Eyes script) => TODO RENAME!

        // Move all things to the correct place in the container...
        for (int i = 0; i < hairParticles.Length; i++)
        {
            HairAnim particle = hairParticles[i];
            Vector2 bary = initialPositionInBlob[i];
            particle.transform.position = TransformSquich(initialPositionInBlob[i]);
            //Debug.DrawLine(container.TransformPoint(initialPositionInBlob2[i]), particle.transform.position, Color.green);
        }
    }

    //
    // Implement actual transformations...
    //
    Vector2 InverseTransformSquich(Vector3 position)
    {
        // 1: transform to blob-local coordinates...
        position = container.InverseTransformPoint(position);

        // 2: subtract the center point from everything (potentially different to local zero)
        // TODO? Probably does not make much difference?
        Vector3 origin = Vector3.zero;

        // 3: figure out in which triangle we are
        Vector3[] corners = new Vector3[cornersClockwise.Length];
        float[] signs = new float[cornersClockwise.Length];
        float[] xtransformed = new float[cornersClockwise.Length];
        float[] ytransformed = new float[cornersClockwise.Length];
        float x = 1; float y = 1;
        for (int i = 0; i < cornersClockwise.Length; i++) {
            corners[i] = container.InverseTransformPoint(cornersClockwise[i].position);
            signs[i] = sign(position, origin, corners[i]);

            // Coordinates in the destination system
            xtransformed[i] = x;
            ytransformed[i] = y;
            float temp = x;
            x = y;
            y = -temp;
        }

        int appliedTriangle = 0;
        for (int i = 0; i < cornersClockwise.Length; i++)
        {
            if (signs[i] <= 0 && signs[(i + 1) % cornersClockwise.Length] >= 0) {
                appliedTriangle = i;
            }
        }

        // Show which ones are in which triangle...
        //Debug.DrawLine(container.TransformPoint(position), container.TransformPoint(.5f*(corners[appliedTriangle]+corners[(appliedTriangle + 1) % cornersClockwise.Length])), Color.blue, 5f);

        // 4: express the coordinates along the axes to the corner points...
        Vector3 bary = Barycentric(position, corners[appliedTriangle], corners[(appliedTriangle + 1) % cornersClockwise.Length], origin);
        float xcorner = bary.x;
        float ycorner = bary.y;
        return new Vector2(xtransformed[appliedTriangle]* xcorner, ytransformed[appliedTriangle]* ycorner);
    }

    Vector3 TransformSquich(Vector2 position)
    {
        // 4: invert the coordinates along the axes to the corner points...
        Vector3[] corners = new Vector3[cornersClockwise.Length];
        float x = 1; float y = 1;
        int appliedTriangle = 0;
        for (int i = 0; i < cornersClockwise.Length; i++)
        {
            corners[i] = container.InverseTransformPoint(cornersClockwise[i].position);

            if (Mathf.Sign(position.x) == Mathf.Sign(x) && Mathf.Sign(position.y) == Mathf.Sign(y)) {
                appliedTriangle = i;
            }

            // Coordinates in the destination system
            float temp = x;
            x = y;
            y = -temp;
        }

        // Apply the inverse transformation...
        Vector3 wrappedPosition = corners[appliedTriangle] * Mathf.Abs(position.x) + corners[(appliedTriangle + 1) % cornersClockwise.Length] * Mathf.Abs(position.y);

        // 2. add the center point to everything (potentially different to local zero)
        // TODO?

        // 1. transform back from blob-local coordinates...
        return container.TransformPoint(wrappedPosition);
    }

    float sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    Vector3 Barycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        return new Vector3(u, v, w);
    }
}
