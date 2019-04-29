using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour
{
    private LayerMask layerMask;

    // Info about the goal...
    private bool hasGoal = false;
    private Transform goalOwner;
    private Vector3 goalLocalPosition;

    // When will we recalculate the path?
    private float nextNavUpdate;

    // The current path
    private List<Vector3> path;

    public bool randomNavUpdates = false;

    // Start is called before the first frame update
    void Start()
    {
        layerMask = 1 << LayerMask.NameToLayer("walkable");

        ScheduleNavUpdate();
    }

    public void SetGoal(bool hasGoal, Transform goalOwner, Vector3 goalLocalPosition) {
        this.hasGoal = hasGoal;
        this.goalOwner = goalOwner;
        this.goalLocalPosition = goalLocalPosition;
        path = null;
    }

    private Vector3 GetGoalPosition() {
        Vector3 position = goalLocalPosition;
        if (goalOwner!= null) {
            position = goalOwner.TransformPoint(position);
        }
        return position;
    }

    // Update is called once per frame
    void Update()
    {
        // Next update
        if (Time.time > nextNavUpdate && randomNavUpdates)
        {
            UpdatePath();
            ScheduleNavUpdate();
        }

        // Draw path
        if(path!=null && hasGoal) {
            Vector3 DRAWOFFSET = Vector3.up * .3f;
            Debug.DrawLine(transform.position, path[0] + DRAWOFFSET, Color.green);
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i] + DRAWOFFSET, path[i + 1] + DRAWOFFSET, Color.green);
            }
            Debug.DrawLine(GetGoalPosition(), path[path.Count-1] + DRAWOFFSET, Color.green);
        }
    }

    public void ScheduleNavUpdate() {
        nextNavUpdate = Time.time + Random.Range(.2f, .5f);
    }

    public void UpdatePath()
    {
        if (!hasGoal) {
            return;
        }

        path = null;
        Point p = GetEntryPoint(transform.position);
        Point goalPoint = null;
        if (p != null) {
            p.owner.AddPoint(p);

            goalPoint = GetEntryPoint(GetGoalPosition());
            goalPoint.owner.AddPoint(goalPoint);
        }


        if (p != null && goalPoint!=null)
        {
            
            // Path Finding... (Dijkstra)
            Dictionary<Point, float> distances = new Dictionary<Point, float>();
            Dictionary<Point, Point> parent = new Dictionary<Point, Point>();
            HashSet<Point> explored = new HashSet<Point>();
            distances[p] = 0;

            while (explored.Count != distances.Count)
            {
                // Find node with smallest distance...
                float min = float.PositiveInfinity;
                Point next = null;
                foreach (KeyValuePair<Point, float> node in distances)
                {
                    if (node.Value < min && !explored.Contains(node.Key))
                    {
                        min = node.Value;
                        next = node.Key;
                    }
                }
                explored.Add(next);

                if (next != null)
                {
                    float baseDistance = distances[next];

                    // Neighbours
                    // TODO: pathfinding "underneath... ?" => we currently don't know whether we can get underneath...
                    List<Point> neighbours = new List<Point>();
                    // TODO JEPE: left
                    // Checks whether we can actually reach there...
                    Point left = FindNextAccessibleNeighbour(next, -1, false);
                    Point left2 = FindNextAccessibleNeighbour(next, -1, true);
                    Point right = FindNextAccessibleNeighbour(next, 1, false);
                    Point right2 = FindNextAccessibleNeighbour(next, 1, true);
                    if (left != null) {
                        neighbours.Add(left);
                    }
                    if (right != null) {
                        neighbours.Add(right);
                    }
                    if (left2 != null)
                    {
                        neighbours.Add(left2);
                    }
                    if (right2 != null)
                    {
                        neighbours.Add(right2);
                    }


                    // Run through neighbours...
                    foreach (Point neighbour in neighbours)
                    {
                        // Go to the neighbour
                        float currentDistance = float.PositiveInfinity;
                        if (distances.ContainsKey(neighbour))
                        {
                            currentDistance = distances[neighbour];
                        }
                        if (currentDistance > baseDistance + 1)
                        {
                            distances[neighbour] = baseDistance + 1;
                            parent[neighbour] = next;

                            // Debug
                            //Vector3 A = neighbour.owner.transform.TransformPoint(neighbour.localPosition);
                            //Vector3 B = next.owner.transform.TransformPoint(next.localPosition);
                            //Debug.DrawLine(B + DRAWOFFSET, A + DRAWOFFSET, Color.gray);
                        }
                    }
                }
                else
                {
                    throw new System.InvalidOperationException("Shouldn't get here...");
                }
            }

            // Path finding done...
            
            if (distances.ContainsKey(goalPoint))
            {
                float distance = distances[goalPoint.left];

                path = new List<Vector3>();
                path.Add(goalPoint.owner.transform.TransformPoint(goalPoint.localPosition));
                Point pathPoint = goalPoint;
                path.Add(pathPoint.owner.transform.TransformPoint(pathPoint.localPosition));
                while (parent.ContainsKey(pathPoint))
                {
                    pathPoint = parent[pathPoint];
                    path.Add(pathPoint.owner.transform.TransformPoint(pathPoint.localPosition));
                }
                path.Reverse();
            }
            else
            {
                Debug.Log("Point not reachable...");
            }

            p.owner.RemovePoint(p);
            goalPoint.owner.RemovePoint(goalPoint);
        }
    }

    public List<Vector3> GetPath() {
        return path;
    }

    Point GetEntryPoint(Vector3 position)
    {
        // 1. Raycast
        RaycastHit2D hit = Physics2D.Raycast(position, Vector3.down, 6f, layerMask);
        if (hit.collider != null)
        {
            // 2. Get NavCube
            NavCube other = NavCube.FindNavCube(hit.collider);
            if (other != null)
            {
                // 3. call GetHit
                return other.GetHit(hit.point);
            }
        }
        return null;
    }

    Point FindNextAccessibleNeighbour(Point start, int dir, bool goBelowIfPossible) {
        Point result = start;
        bool valid = true;
        int remainingiter = 10;
        while (remainingiter > 0)
        {
            remainingiter--;
            Point previousLeft = result;
            if (dir == -1)
            {
                if (goBelowIfPossible && result.overruledLeft != null && result.overruleLeftCanGoBelow)
                {
                    // Go below :)
                    result = result.left;
                } else if (result.overruledLeft != null)
                {
                    result = result.overruledLeft;
                }
                else
                {
                    result = result.left;
                }
                if (Vector3.Dot(result.owner.transform.TransformVector(result.rightNormal), Vector3.up) < -0.5f)
                {
                    // We are upside down...
                    valid = false;
                    break;
                }
            }
            else {
                if (goBelowIfPossible && result.overruledRight != null && result.overruleRightCanGoBelow)
                {
                    // Go below :)
                    result = result.right;
                }
                else if (result.overruledRight != null)
                {
                    result = result.overruledRight;
                }
                else
                {
                    result = result.right;
                }
                if (Vector3.Dot(result.owner.transform.TransformVector(result.leftNormal), Vector3.up) < -0.5f)
                {
                    // We are upside down...
                    valid = false;
                    break;
                }
            }

            // Check if we did not go the opposite direction, if so, stop...
            Vector3 vectPrev = previousLeft.owner.transform.TransformPoint(previousLeft.localPosition);
            Vector3 vectNow = result.owner.transform.TransformPoint(result.localPosition);
            /*if (dir*vectPrev.x > dir*vectNow.x)
            {
                valid = false;
                break;
            }*/
            // Check if we are using an upside down path


            // Check if we did not go flat, if so, stop... (found one)
            float rico = (vectNow.y - vectPrev.y) / Mathf.Abs(vectNow.x - vectPrev.x);
            if (rico < .3f)
            {
                valid = true;
                if (start != previousLeft) {
                    result = previousLeft;
                }
                break;
            }
            // Check if we did not go too far, if so, stop...
            /*Vector3 vectOrigin = start.owner.transform.TransformPoint(start.localPosition);
            if (Vector3.Distance(vectOrigin, vectNow) > 6f)// TODO JEPE: How far can we go up-ish?
            {
                valid = false;
                break;
            }*/
            // ^Disabling above code so, it does find paths...
        }
        if (remainingiter == 0) {
            valid = false;
        }

        if (valid) {
            return result;
        }
        else
        {
            return null;
        }
    }
}
