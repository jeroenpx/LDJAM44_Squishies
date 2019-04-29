using UnityEngine;
using System.Collections;

public class ScrollWheelZoom : MonoBehaviour
{

    public float ZoomAmount  = 0;
    float zoomSpeed = 10;

    public float innerZoom = 3.65f;
    public float outerZoom = 7f;

    private void Start()
    {
        ZoomAmount = Mathf.Clamp(ZoomAmount, innerZoom, outerZoom);
        Camera.main.orthographicSize = ZoomAmount;
    }

    void Update()
    {
        ZoomAmount -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        ZoomAmount = Mathf.Clamp(ZoomAmount, innerZoom, outerZoom);
        

        // Zoom around cursor...
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Update camera
        Camera.main.orthographicSize = ZoomAmount;

        // Zoom around cursor... (2)
        Vector3 worldPointNew = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Camera.main.transform.position = Camera.main.transform.position - (worldPointNew - worldPoint);
    }
}
