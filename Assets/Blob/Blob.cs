using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Blob : MonoBehaviour
{
    private enum BlobMode
    {
        IDLE, PATHFINDING, CONSTRUCT
    }

    private static string[] names = {
        "George",
        "Josh",
        "Carly",
        "Oskar",
        "Joel",
        "Arnold",
        "Eva",
        "Rudy",
        "Nell",
        "Carlo",
        "Bob",
        "Jenny",
        "Clyde",
        "Mari",
        "Khia",
        "Asha",
        "Mac",
        "Franky",
        "Michael",
        "Nada",
        "Alby",
        "Kaif"
    };
    private static int namesTaken = 0;

    private Rigidbody2D[] bodies;
    private IsTouching[] touchings;

    const float JUMPHIGH = 5f;
    const float JUMPMED = 3f;
    const float JUMPMINI = 2f;
    const float JUMPMIN = 1f;
    const float MINTIMEBETWEENJUMPS = 1f;
    const float MAXTIMEBETWEENJUMPS = 4f;

    // Whatever is configured in the settings!
    public float gravity = 9.81f;

    public float touchTimeThreshold = 0.3f;
    public float minTimeBetweenJumps = .8f;


    //
    // State of the blob
    //

    bool landedSafely = false;
    Navigator nav;


    BlobGrow grow;

    bool constructed = false;


    float nextJumpTime = 0f;
    float lastJumpTime = 0f;

    private string blobName;

    public bool userIsHoveringMe = false;

    private void Awake()
    {
        blobName = names[namesTaken];
        namesTaken++;
        namesTaken = namesTaken % names.Length;
    }

    // Start is called before the first frame update
    void Start()
    {
        bodies = new Rigidbody2D[5];
        bodies[0] = GetComponent<Rigidbody2D>();
        bodies[1] = transform.Find("TR").GetComponent<Rigidbody2D>();
        bodies[2] = transform.Find("BR").GetComponent<Rigidbody2D>();
        bodies[3] = transform.Find("BL").GetComponent<Rigidbody2D>();
        bodies[4] = transform.Find("TL").GetComponent<Rigidbody2D>();

        touchings = transform.GetComponentsInChildren<IsTouching>();

        nextJumpTime = Time.time + Random.Range(MINTIMEBETWEENJUMPS, MAXTIMEBETWEENJUMPS);

        nav = GetComponent<Navigator>();
        grow = GetComponent<BlobGrow>();
    }

    public string GetBlobName() {
        return blobName;
    }

    public void Construct() {
        constructed = true;
        grow.Grow();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if we landed ok...
        bool onGround = false;
        foreach (IsTouching touching in touchings) {
            if(touching.GetDurationTouching()>touchTimeThreshold) {
                onGround = true;
            }
        }

        // Nav path
        List<Vector3> path = nav.GetPath();
        BlobMode mode = constructed?BlobMode.CONSTRUCT:(path != null? BlobMode.PATHFINDING:BlobMode.IDLE);

        // Idle Mode...
        if (mode == BlobMode.IDLE)
        {
            nav.randomNavUpdates = true;
            if (Time.time > nextJumpTime && !userIsHoveringMe)
            {
                float height = Random.Range(JUMPMIN, JUMPMINI);
                Jump(height, 0f);
                nextJumpTime = Time.time + Random.Range(MINTIMEBETWEENJUMPS, MAXTIMEBETWEENJUMPS);
            }
        }

        // PathFinder Mode...
        if (mode == BlobMode.PATHFINDING && onGround && Time.time> lastJumpTime + minTimeBetweenJumps)
        {
            // Force a nav update
            nav.randomNavUpdates = false;
            nav.UpdatePath();
            path = nav.GetPath();
            if (path != null)
            {

                /*Vector3 DRAWOFFSET = Vector3.up * .3f;
                Debug.DrawLine(transform.position, path[0] + DRAWOFFSET, Color.green);
                for (int j = 0; j < path.Count - 1; j++)
                {
                    Debug.DrawLine(path[j] + DRAWOFFSET, path[j + 1] + DRAWOFFSET, Color.cyan, 1f);
                }*/


                nextJumpTime = Time.time + Random.Range(MINTIMEBETWEENJUMPS, MAXTIMEBETWEENJUMPS);
                const float MAXFLATJUMP = 6f;
                const float MAXWALLDISTANCEJUMP = 3f;
                const float MINDISTANCEFROMWALLUP = 1.5f;
                const float SMALLWALL = 1.5f;
                const float TYPICALJUMPDISTANCE = 1f;

                Vector3 current = transform.position;
                bool continuepath = true;
                int i = 1;
                bool facedWall = false;
                Vector3 startpoint = current;
                float flatx = current.x;// Till what x coordinate is it flat?
                int dir = path[1].x > current.x ? 1 : -1;

                //Debug.DrawLine(new Vector3(startpoint.x, transform.position.y - 1f, 0f), new Vector3(startpoint.x, transform.position.y + 1f, 0f), Color.red, 1f);
                //Debug.DrawLine(new Vector3(path[1].x, transform.position.y - 1f, 0f), new Vector3(path[1].x, transform.position.y + 1f, 0f), Color.green, 1f);

                while (continuepath && i < path.Count)
                {
                    Vector3 next = path[i];

                    // Check angle...
                    float rico = (next.y - current.y) / Mathf.Abs(next.x - current.x);
                    if (rico > .3f && (int)Mathf.Sign(next.x - current.x) == dir)
                    {
                        // Going up...
                        facedWall = true;
                        current = next;
                    }
                    else
                    {
                        // Going down or flat...
                        if (!facedWall)
                        {
                            flatx = next.x;
                            if (Mathf.Abs(flatx - startpoint.x) > MAXFLATJUMP)
                            {
                                continuepath = false;
                                // Just jump flat (max jump)
                            }
                        }
                        else
                        {
                            // We found the spot on top of the wall...
                            continuepath = false;
                        }
                    }
                    i++;

                    // Idea...  ???
                    // Option A: flat from current...
                    // Option B: downwards from current...
                    // Check how far "flat" we have
                    // If enough horizontal space, go there...
                    // If no, continue with option C
                    // Or in case we rotate, just jump to the end.

                    // Option C: upwards from current...
                    // If too close to wall... jump to point a bit away from wall...

                    // If too far from wall... jump to point a bit closer to wall...

                    // Else: jump to point on wall...
                }

                if (!facedWall)
                {
                    if (Mathf.Abs(flatx - startpoint.x) < 1f)
                    {
                        // So cloose... Stop moving :)
                        nav.SetGoal(false, null, Vector3.zero);
                        Debug.Log("Reached destination!");
                    }
                    else if (Mathf.Abs(flatx - startpoint.x) < MAXFLATJUMP)
                    {
                        // We are almost there, try moving a bit closer...
                        Jump(JUMPMIN, JUMPMIN * Mathf.Sign(flatx - startpoint.x));
                        Debug.Log("Almost there (forward)!");
                    }
                    else
                    {
                        // Simple, just jump forward...
                        Jump(JUMPMINI, JUMPMINI * Mathf.Sign(flatx - startpoint.x));
                        Debug.Log("Forward!");
                    }
                }
                else
                {
                    // Check distance to wall...
                    if (Mathf.Abs(flatx - startpoint.x) > MAXWALLDISTANCEJUMP)
                    {
                        // Jump a bit forward...
                        Jump(JUMPMIN, JUMPMIN * Mathf.Sign(flatx - startpoint.x));
                        Debug.Log("A bit forwards towards wall!");
                    }
                    else if (Mathf.Abs(flatx - startpoint.x) < MINDISTANCEFROMWALLUP)
                    {
                        if (current.y - startpoint.y < SMALLWALL)
                        {
                            // A Small wall... Jump on it...
                            Jump(4f, 1.1f * Mathf.Sign(current.x - startpoint.x));
                            Debug.Log("Small wall, jump!");
                        }
                        else
                        {
                            // Big wall... Jump a bit backwards...
                            Jump(JUMPMIN, -JUMPMIN * Mathf.Sign(flatx - startpoint.x));
                            Debug.Log("Big wall, too close, go back!");
                        }
                    }
                    else
                    {
                        // We are close enough... Try to get on it!
                        Jump(JUMPHIGH, 1.2f * Mathf.Sign(current.x - startpoint.x));
                        Debug.Log("Jump!");
                    }
                }

                // if facedwall
                // current = where we want to go (could be on top of the wall?)

            }
        }

        // FOR DEBUGGING
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump(JUMPHIGH, JUMPMIN);
        }*/
    }

    void Jump(float height, float right) {
        float halfJumpTime = Mathf.Sqrt(2 * height / gravity);
        float heightForce = (height * 2)/ (halfJumpTime * halfJumpTime);
        float rightForce = right / (halfJumpTime*2f);
        Vector3 force = new Vector3(right, height);
        for (int i = 0; i < bodies.Length; i++) {
            bodies[i].AddForce(force, ForceMode2D.Impulse);
        }
        lastJumpTime = Time.time;
    }
}
