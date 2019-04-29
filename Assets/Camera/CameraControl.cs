using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public float boundaryPercentHeight = 0.2f;
    public float moveFactor = 4f;

    public float innerZoom = 3.65f;
    public float outerZoom = 7f;

    public float mouseKeepAwakeFactor = 0.9f;
    public float zoomInEffectSpeed = 10f;
    public float scrollZoomFactor = .5f;

    public float mouseMovevementImpact = 1f;

    private float mouseMoveInSceneSum = 0f;
    private Vector3 mouseInWorld;

    private float cameraScale = 0f;
    private float currentCameraScale = 0f;
    private float cameraScaleVelocity = 0f;

    public bool legacyCameraMovement = false;

    // Update is called once per frame
    void Update()
    {
        if (Cursor.lockState != CursorLockMode.None) {
            return;
        }

        Vector3 mouse = Input.mousePosition;

        // If we hit the boundaries, move?
        if (legacyCameraMovement)
        {
            int pixelsBoundary = Mathf.RoundToInt(Screen.height * boundaryPercentHeight);

            float right = 0;
            float up = 0;
            if (mouse.x < pixelsBoundary)
            {
                right = -1 * (1f - mouse.x / pixelsBoundary);
            }
            else if (mouse.x > Screen.width - pixelsBoundary)
            {
                right = 1 * (1f - (Screen.width - mouse.x) / pixelsBoundary);
            }

            if (mouse.y < pixelsBoundary)
            {
                up = -1 * (1f - mouse.y / pixelsBoundary);
            }
            else if (mouse.y > Screen.height - pixelsBoundary)
            {
                up = 1 * (1f - (Screen.height - mouse.y) / pixelsBoundary);
            }

            // Also zoom when scrolling...
            mouseMoveInSceneSum += (Vector3.up * up + Vector3.right * right).magnitude * pixelsBoundary / Screen.height * scrollZoomFactor;

            transform.position = transform.position + (Vector3.up * up + Vector3.right * right) * (moveFactor+ currentCameraScale*((outerZoom/ innerZoom-1f)* moveFactor)) * Time.unscaledDeltaTime;
        }

        // Forget we moved the mouse (partially)
        mouseMoveInSceneSum = mouseMoveInSceneSum * mouseKeepAwakeFactor;

        // If we move the mouse, take a running sum on this
        //Vector3 mouseInWorldBase = Camera.main.ScreenToWorldPoint(mouse);
        Vector3 mouseInWorldNew = new Vector2(mouse.x * 1f/Screen.height, mouse.y * 1f/Screen.height);// new Vector2(mouseInWorldBase.x, mouseInWorldBase.y);
        mouseMoveInSceneSum += (mouseInWorldNew- mouseInWorld).magnitude * mouseMovevementImpact;// Little movement => zoom out!
        mouseInWorld = mouseInWorldNew;

        mouseMoveInSceneSum = Mathf.Clamp(mouseMoveInSceneSum, 0, 2f);

        // If we moved the mouse, make the camera view bigger
        if (mouseMoveInSceneSum < 1) {
            cameraScale -= (1-mouseMoveInSceneSum) * Time.unscaledDeltaTime * zoomInEffectSpeed;// TODO: some constant? (how slow do we zoom in?)
        } else {
            cameraScale += (mouseMoveInSceneSum - 1) * Time.unscaledDeltaTime * zoomInEffectSpeed;// TODO: some constant? (how slow do we zoom in?)
        }

        cameraScale = Mathf.Clamp01(cameraScale);

        currentCameraScale = Mathf.SmoothDamp(currentCameraScale, cameraScale, ref cameraScaleVelocity, 1f);

        // Zoom around cursor...
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Update camera
        Camera.main.orthographicSize = innerZoom + cameraScale * (outerZoom - innerZoom);

        Vector3 worldPointNew = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Camera.main.transform.position = Camera.main.transform.position -(worldPointNew - worldPoint);
    }
}
