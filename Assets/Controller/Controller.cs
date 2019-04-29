using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public string textMoveEveryone = "Move Everyone";
    public string textMoveOne = "Move";
    public string textConstruct = "Construct";
    public string textConstructOne = "Construct";

    public string textNooneSelected = "No Squishy Selected";
    public string textSelected = "You selected ";

    public Blob selected = null;
    public Blob hovering = null;
    private bool selectNextFrame = false;
    private Blob[] allBlobs = null;

    public Text selectionText;
    public Text gotoText;
    public Text constructText;
    public Text cancelText;
    public Text speedText;
    public GameObject[] hideWhenCancel;
    public GameObject[] showWhenCancel;
    public GameObject speedButton;
    public Button constructButton;

    public float timePerSelectionArrowRoundTrip;

    public Transform flag;

    public int timeSpeed = 5;
    private bool timeSpeedOn;

    // Starting with topmost arrow
    public RectTransform canvasRect;
    public RectTransform[] arrowsClockwise;

    private bool moveOngoing = false;
    private bool constructOngoing = false;
    private int updates = 0;

    private LayerMask layerMaskTerrain;
    private LayerMask playerTriggerMask;

    private bool uiButtonClicked;

    // Dragging logic
    public float realDragDistance = .01f;
    private bool dragging = false;
    private bool realDragging = false;
    private Vector3 dragStart;
    private Vector3 dragStartMouse;

    // Start is called before the first frame update
    void Awake()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;

        layerMaskTerrain = 1 << LayerMask.NameToLayer("walkable");
        playerTriggerMask = 1 << LayerMask.NameToLayer("blobs");

        allBlobs = FindObjectsOfType<Blob>();
    }

    void Start()
    {
        UpdateUI();
        flag.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        updates++;
        if (updates > 60) {
            Cursor.lockState = CursorLockMode.None;
            updates = -100000000;
        }

        // Highlight selected person on the screen
        HighLightSelectedPerson();

        Blob wasHovering = hovering;
        hovering = null;

        // Mouse World Point
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Click Handling
        bool clicked = false;
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            // We started dragging at a certain point in the world.
            dragging = true;
            realDragging = false;
            dragStart = worldPoint;
            dragStartMouse = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        }
        if (dragging) {
            if (Vector3.Distance(Camera.main.ScreenToViewportPoint(Input.mousePosition), dragStartMouse) > realDragDistance) {
                realDragging = true;
            }
            if (realDragging) {
                Camera.main.transform.position -= worldPoint - dragStart;
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            dragging = false;
            if (realDragging)
            {
                // Don't click!
            }
            else {
                clicked = true;
            }
        }

            // Show Flag
        if (uiButtonClicked)
        {
            // Do nothing...
        } else if (moveOngoing)
        {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldPoint.x, worldPoint.y), Vector2.down, 5f, layerMaskTerrain);
            bool foundPoint = hit.collider && hit.distance > 0.02f;
            if (foundPoint)
            {
                flag.gameObject.SetActive(true);
                flag.position = hit.point;
            }
            else
            {
                flag.gameObject.SetActive(false);
            }

            if (clicked) {
                Blob[] blobsToMove; ;
                if (selected != null)
                {
                    blobsToMove = new Blob[] { selected };
                }
                else {
                    blobsToMove = allBlobs;
                }
                foreach (Blob blob in blobsToMove) {
                    Navigator nav = blob.GetComponent<Navigator>();
                    if (foundPoint)
                    {
                        nav.SetGoal(foundPoint, hit.collider.transform, hit.collider.transform.InverseTransformPoint(hit.point));
                    } else
                    {
                        nav.SetGoal(foundPoint, null, Vector3.zero);
                    }
                }
                // Deselect
                selected = null;
                // Stop Move Op
                ClickCancel();
            }
        } else {
            // Selection
            if (selectNextFrame)
            {
                selected = wasHovering;
                OnSelectionChange();
                UpdateUI();
                selectNextFrame = false;
            }


            Collider2D hitbox = Physics2D.OverlapPoint(worldPoint, playerTriggerMask);
            if (hitbox != null) {
                hovering = hitbox.GetComponent<Blob>();
                if (hovering == null && hitbox.transform.parent) {
                    hovering = hitbox.transform.parent.GetComponent<Blob>();
                }
            }

            if (clicked) {
                selectNextFrame = true;
            }
        }

        if (hovering != wasHovering)
        {
            if (wasHovering != null) { 
                wasHovering.userIsHoveringMe = false;
            }
            if (hovering!=null)
            {
                hovering.userIsHoveringMe = true;
            }
            UpdateUI();
        }

        uiButtonClicked = false;
    }

    void OnSelectionChange() {
        if (selected != null) {
            // When we select someone, let him stop doing stuff!
            Navigator nav = selected.GetComponent<Navigator>();
            nav.SetGoal(false, null, Vector3.zero);
        }
    }

    void HighLightSelectedPerson() {
        if (selected == null && hovering == null) {
            for (int i = 0; i < 4; i++)
            {
                arrowsClockwise[i].gameObject.SetActive(false);
            }
            return;
        }

        Vector3 selectedPos = hovering!=null? hovering.transform.position:selected.transform.position;


        // if none of the sides inside screen
        const float deltaSize = 0.5f;
        Vector3 delta = new Vector3(0f, 1f);
        bool someInsideScreen = false;
        for (int i = 0; i < 4; i++)
        {
            Vector3 viewPortPos = Camera.main.WorldToViewportPoint(selectedPos + delta * deltaSize);
            if (viewPortPos.x > 0 && viewPortPos.x < 1 && viewPortPos.y > 0 && viewPortPos.y < 1)
            {
                // Inside screen...
                someInsideScreen = true;
            }

            arrowsClockwise[i].gameObject.SetActive(false);

            delta = new Vector3(delta.y, -delta.x);
        }

        delta = new Vector3(0f, 1f);
        for (int i = 0; i < 4; i++)
        {
            bool showArrow = false;
            Vector3 viewPortPos = Camera.main.WorldToViewportPoint(selectedPos + delta * deltaSize);
            if (!someInsideScreen)
            {
                // Show the first arrow in a direction of screen...
                if (delta.x > 0 && viewPortPos.x < 0)
                {
                    showArrow = true;
                }
                else if (delta.x < 0 && viewPortPos.x > 1)
                {
                    showArrow = true;
                }
                else if (delta.y > 0 && viewPortPos.y < 0)
                {
                    showArrow = true;
                }
                else if (delta.y < 0 && viewPortPos.y > 1)
                {
                    showArrow = true;
                }
                if (showArrow)
                {
                    if (viewPortPos.x < 0)
                    {
                        viewPortPos.x = 0;
                    }
                    if (viewPortPos.x > 1)
                    {
                        viewPortPos.x = 1;
                    }
                    if (viewPortPos.y < 0)
                    {
                        viewPortPos.y = 0;
                    }
                    if (viewPortPos.y > 1)
                    {
                        viewPortPos.y = 1;
                    }
                }
            }
            else
            {
                // Show first arrow inside screen...
                if (viewPortPos.x > 0 && viewPortPos.x < 1 && viewPortPos.y > 0 && viewPortPos.y < 1)
                {
                    showArrow = true;
                }
            }

            if (showArrow)
            {
                Vector2 screenPoint = new Vector2(((viewPortPos.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f)),
     ((viewPortPos.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f)));
                screenPoint.x += delta.x * 15;
                screenPoint.y += delta.y * 15;
                float upDown = Mathf.Sin(Time.unscaledTime*Mathf.PI*2/timePerSelectionArrowRoundTrip);
                screenPoint.x += delta.x * 10*upDown;
                screenPoint.y += delta.y * 10 * upDown;

                arrowsClockwise[i].gameObject.SetActive(true);

                arrowsClockwise[i].anchoredPosition = screenPoint;

                break;
            }

            delta = new Vector3(delta.y, -delta.x);
        }
    }

    void UpdateUI() {
        bool showCancel = moveOngoing || constructOngoing;
        foreach (GameObject toHide in hideWhenCancel) {
            toHide.SetActive(!showCancel && !timeSpeedOn);
        }
        foreach (GameObject toShow in showWhenCancel)
        {
            toShow.SetActive(showCancel && !timeSpeedOn);
        }
        speedButton.SetActive(!showCancel);
        //selectionText.gameObject.SetActive(!timeSpeedOn);

        if (selected == null)
        {
            selectionText.text = textNooneSelected;
            gotoText.text = textMoveEveryone;
            constructText.text = textConstruct;
        }
        else
        {
            selectionText.text = textSelected + "\"" + selected.GetBlobName() + "\"";
            gotoText.text = textMoveOne;
            constructText.text = textConstructOne;
        }
        constructButton.interactable = (selected != null);

        if(hovering != null && selected != hovering)
        {
            selectionText.text = "Select Squishy \"" + hovering.GetBlobName() + "\"?";
        }

        speedText.text = "Time x " + (timeSpeedOn ? "" + timeSpeed : "1");
    }

    public void ClickSpeed()
    {
        uiButtonClicked = true;
        timeSpeedOn = !timeSpeedOn;
        Time.timeScale = timeSpeedOn?5f:1f;
        UpdateUI();
    }

    public void ClickMove() {
        uiButtonClicked = true;
        moveOngoing = true;



        UpdateUI();
    }

    public void ClickConstruct()
    {
        uiButtonClicked = true;
        constructOngoing = true;

        // TODO UI!
        selected.Construct();

        // Do not show cancel button
        ClickCancel();

        // Disable selection...
        selected = null;
        UpdateUI();
    }

    public void ClickCancel() {
        uiButtonClicked = true;
        if (moveOngoing) {
            // Cancel move (indicator)
            flag.gameObject.SetActive(false);
            moveOngoing = false;
        }

        if (constructOngoing) {
            // Cancel construction (indicator)
            // TODO
            constructOngoing = false;
        }

        UpdateUI();
    }
}
