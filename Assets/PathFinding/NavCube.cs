using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Point
{
    public NavCube owner;
    public Vector3 localPosition;
    public int quadrant;

    public Point left;
    public Point right;
    public Point overruledLeft;
    public bool overruleLeftCanGoBelow;
    public Point overruledRight;
    public bool overruleRightCanGoBelow;
    public float order;

    public Vector3 rightNormal;
    public Vector3 leftNormal;
}

public class NavCube : MonoBehaviour
{

    // Max angle we can go up?
    const float maxAngle = 33;

    public BoxCollider2D box;
    public float altBoxSize;
    private LayerMask layerMask;


    // Clockwise array of corners...
    private Vector3[] corners;
    private Point[] cornerPoints;

    // Quadrants with their list of points
    private List<Point>[] quadrants;

    // Points we registered on other cubes
    private List<Point> registeredPoints;


    /**
     * Make a new point closest to position
     * 
     * Either forget after use or add it (AddPoint)
     */
    public Point GetHit(Vector3 position) {
        // PARTIALLY DUPLICATE CODE, see "HairSquichy"

        // 1: transform to local coordinates...
        position = transform.InverseTransformPoint(position);
        
        // 3: figure out in which triangle we are
        float[] signs = new float[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            signs[i] = sign(position, Vector3.zero, corners[i]);
        }

        int appliedTriangle = 0;
        for (int i = 0; i < corners.Length; i++)
        {
            if (signs[i] <= 0 && signs[(i + 1) % corners.Length] >= 0)
            {
                appliedTriangle = i;
            }
        }

        // Calculate "order"
        float order = Vector3.Dot(position - corners[appliedTriangle], corners[(appliedTriangle + 1) % corners.Length] - corners[appliedTriangle]);

        // In normal situations this does not happen, but it can happen with a "blob" "nav cube"...
        if (order < 0)
        {
            order = 0;
        }
        if(order > 1) {
            order = 1;
        }

        // Create a new point
        Point p = new Point();
        p.owner = this;
        p.order = order;
        p.quadrant = appliedTriangle;
        p.localPosition = (corners[(appliedTriangle + 1) % corners.Length] - corners[appliedTriangle]) * order + corners[appliedTriangle];

        // Find out what is left and right of this one...
        Point left = quadrants[appliedTriangle][0];
        float leftOrder = 0;
        Point right = quadrants[appliedTriangle][1];
        float rightOrder = 1.1f;

        for(int i=2;i<quadrants[appliedTriangle].Count;i++)
        {
            Point other = quadrants[appliedTriangle][i];
            //Debug.Log("order: " + other.order+" vs "+leftOrder+"; "+order+"; "+rightOrder);
            if (other.order >= leftOrder && other.order <= order) {
                leftOrder = other.order;
                left = other;
            }
            if (other.order < rightOrder && other.order > order)
            {
                rightOrder = other.order;
                right = other;
            }
        }

        p.left = left;
        p.right = right;

        // Fill in normals as well...
        p.leftNormal = p.left.rightNormal;
        p.rightNormal = p.right.leftNormal;

        if (p.left != p.right.left || p.right != p.left.right)
        {
            Debug.Log(p.left.quadrant);
            Debug.Log(p.right.quadrant);
            Debug.Log(p.left.right.quadrant);
            Debug.Log(p.right.left.quadrant);
            Debug.Log(p.left != p.right.left);
            throw new System.InvalidOperationException("Created an invalid point...");
        }

        return p;
    }

    /**
     * You can add the point you just got via GetHit
     */
    public void AddPoint(Point p) {
        if (p.owner != this) {
            throw new System.InvalidOperationException("Shouldn't happen - AddPoint of point we don't own...");
        }
        if (quadrants[p.quadrant].Contains(p)) {
            throw new System.InvalidOperationException("Shouldn't happen - AddPoint of point we already have...");
        }

        quadrants[p.quadrant].Add(p);

        if (p.left != p.right.left || p.right != p.left.right)
        {
            throw new System.InvalidOperationException("Shouldn't happen - Point outdated...");
        }

        p.left.right = p;
        p.right.left = p;

        // Can go below?
        // If there are more points to the right, return true...
        // 
        float PADDING = .2f;
        if (p.overruledRight != null) {
            // Do a raycast...
            Vector3 from = p.owner.transform.TransformPoint(p.localPosition);
            Vector3 to = p.right.owner.transform.TransformPoint(p.right.localPosition);
            RaycastHit2D hit = Physics2D.Raycast(from + p.rightNormal * PADDING, to - from, 12f, layerMask);
            Debug.DrawRay(from + p.rightNormal * PADDING, (to - from).normalized*12f, Color.yellow);
            if (hit.collider != null)
            {
                // Something there... Is it the owner of overruledRight?
                p.overruleRightCanGoBelow = FindNavCube(hit.collider) != p.overruledRight.owner;
            }
            else
            {
                // Nothing there...
                p.overruleRightCanGoBelow = true;
            }
        }
        if (p.overruledLeft != null)
        {
            // Do a raycast...
            Vector3 from = p.owner.transform.TransformPoint(p.localPosition);
            Vector3 to = p.left.owner.transform.TransformPoint(p.left.localPosition);
            RaycastHit2D hit = Physics2D.Raycast(from + p.leftNormal * PADDING, to - from, 12f, layerMask);
            Debug.DrawRay(from + p.leftNormal * PADDING, (to - from).normalized * 12f, Color.yellow);
            if (hit.collider != null)
            {
                // Something there... Is it the owner of overruledRight?
                p.overruleLeftCanGoBelow = FindNavCube(hit.collider) != p.overruledLeft.owner;
            }
            else
            {
                // Nothing there...
                p.overruleLeftCanGoBelow = true;
            }
        }

    }

    /**
     * You can also remove a point you got via GetHit
     */
    public void RemovePoint(Point p) {
        if (p.owner != this) {
            throw new System.InvalidOperationException("Shouldn't happen - RemovePoint of point we don't own...");
        }
        if (!quadrants[p.quadrant].Contains(p))
        {
            throw new System.InvalidOperationException("Shouldn't happen - RemovePoint of point we no longer have...");
        }

        quadrants[p.quadrant].Remove(p);

        if (p.left.right != p || p.right.left != p) {
            throw new System.InvalidOperationException("Shouldn't happen - Point has invalid link...");
        }

        p.left.right = p.right;
        p.right.left = p.left;
    }

    // Start is called before the first frame update
    void Start()
    {
        registeredPoints = new List<Point>();

        layerMask = 1 << LayerMask.NameToLayer("walkable");

        corners = new Vector3[4];
        Vector2 offset = box != null ? box.offset:Vector2.zero;
        Vector2 size = box!=null?0.5f *box.size: altBoxSize * Vector2.one;
        corners[0] = new Vector3(offset.x + size.x, offset.y + size.y);
        corners[1] = new Vector3(offset.x + size.x, offset.y - size.y);
        corners[2] = new Vector3(offset.x - size.x, offset.y - size.y);
        corners[3] = new Vector3(offset.x - size.x, offset.y + size.y);

        quadrants = new List<Point>[4];
        for (int i = 0; i < quadrants.Length; i++)
        {
            quadrants[i] = new List<Point>();
        }

        cornerPoints = new Point[4];
        Vector3 leftNormal = Vector3.up;
        for (int i = 0; i < cornerPoints.Length; i++)
        {
            Point p = new Point();
            p.owner = this;
            p.quadrant = -1;
            p.localPosition = corners[i];
            p.order = -1;
            p.leftNormal = leftNormal;
            leftNormal = new Vector3(leftNormal.y, -leftNormal.x);
            p.rightNormal = leftNormal;
            cornerPoints[i] = p;
        }
        for (int i = 0; i < cornerPoints.Length; i++)
        {
            Point a = cornerPoints[i];
            Point b = cornerPoints[(i+1)% cornerPoints.Length];
            a.right = b;
            b.left = a;
            quadrants[i].Add(a);
            quadrants[i].Add(b);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Debugdraw
        const float DRAWSIZE = .1f;
        for (int i = 0; i < quadrants.Length; i++) {
            for (int j = 0; j < quadrants[i].Count; j++) {
                Point p = quadrants[i][j];
                Vector3 pos = transform.TransformPoint(p.localPosition);
                Debug.DrawLine(pos + (Vector3.up + Vector3.right) * DRAWSIZE, pos - (Vector3.up + Vector3.right) * DRAWSIZE, Color.cyan);
                Debug.DrawLine(pos + (Vector3.up + Vector3.left) * DRAWSIZE, pos - (Vector3.up + Vector3.left) * DRAWSIZE, Color.cyan);

                Vector3 posLeft = p.left.owner.transform.TransformPoint(p.left.localPosition);
                Debug.DrawLine(posLeft, pos, Color.blue);

                Vector3 posRight = p.right.owner.transform.TransformPoint(p.right.localPosition);
                Debug.DrawLine(posRight, pos, Color.blue);

                if (p.overruledLeft != null)
                {
                    Vector3 posLeft2 = p.overruledLeft.owner.transform.TransformPoint(p.overruledLeft.localPosition);
                    Debug.DrawLine(posLeft2, pos, Color.blue);
                    if(p.overruleLeftCanGoBelow)
                    {
                        Debug.DrawLine(pos + (Vector3.up) * DRAWSIZE, pos - (Vector3.up) * DRAWSIZE, Color.green);
                        Debug.DrawLine(pos + (Vector3.left) * DRAWSIZE, pos - (Vector3.left) * DRAWSIZE, Color.green);
                    }
                }

                if (p.overruledRight != null)
                {
                    Vector3 posRight2 = p.overruledRight.owner.transform.TransformPoint(p.overruledRight.localPosition);
                    Debug.DrawLine(posRight2, pos, Color.blue);
                    if (p.overruleRightCanGoBelow)
                    {
                        Debug.DrawLine(pos + (Vector3.up) * DRAWSIZE, pos - (Vector3.up) * DRAWSIZE, Color.green);
                        Debug.DrawLine(pos + (Vector3.left) * DRAWSIZE, pos - (Vector3.left) * DRAWSIZE, Color.green);
                    }
                }
            }
        }

        // 1. Clean up previously registered points
        foreach(Point p in registeredPoints)
        {
            p.owner.RemovePoint(p);
        }
        registeredPoints.Clear();

        // 2. Clean up all overruled left/right in the corners
        foreach (Point p in cornerPoints)
        {
            p.overruledLeft = null;
            p.overruledRight = null;
        }

        // 2. Raycast again...
        const float PADDING = .3f;
        for (int i = 0; i < corners.Length; i++) {
            // If our left point is below us and it is not really flat to get there... then raycast down...
            Vector3 left = transform.TransformPoint(corners[(i + corners.Length - 1) % corners.Length]);
            Vector3 current = transform.TransformPoint(corners[i]);
            Vector3 right = transform.TransformPoint(corners[(i+1) % corners.Length]);
            Point origin = cornerPoints[i];

            for (int j = -1; j < 2; j+=2)
            {
                Vector3 side = j == -1 ? left:right;

                if (side.y < current.y && Mathf.Abs(current.x - side.x) / (current.y - side.y) < 1)
                {
                    // 1. Trace Left Up
                    Vector3 startPoint = current + (Vector3.up + Vector3.right*j) * 0.05f;
                    Vector3 paddingPoint = current + (Vector3.up + Vector3.right * j) * PADDING;
                    bool hitSomething = Trace(startPoint, paddingPoint, j, origin);

                    if (!hitSomething)
                    {
                        // 2. Trace Down (or parallel with cube...)
                        Vector3 traceDir = Vector3.down;
                        if (side.x*j > current.x*j)
                        {
                            traceDir = (side - current).normalized;
                        }
                        const float traceLength = 20f;// To long is not a problem if we know we can go underneath...

                        Vector3 downPoint = paddingPoint + traceDir * traceLength;
                        Trace(paddingPoint, downPoint, j, origin);
                    }
                }
            }
        }
        
    }

    // Do Raytracing down on the outer edges & update NavMesh links
    bool Trace(Vector3 from, Vector3 to, int dir, Point origin) {
        // 1. Raycast
        RaycastHit2D hit = Physics2D.Raycast(from, to - from, (to - from).magnitude, layerMask);
        Debug.DrawLine(from, to, Color.magenta);
        if (hit.collider != null) {
            Debug.DrawLine(from, to, Color.red);
            // 2. Get NavCube
            NavCube other = NavCube.FindNavCube(hit.collider); 
            if (other != null) {
                // 3. call GetHit
                Point hitPoint = other.GetHit(hit.point);


                // Check distance between point and boundary... (to remove false positives (when hit stats inside the collider)
                Vector3 boundaryPoint = hitPoint.owner.transform.TransformPoint(hitPoint.localPosition);
                if (Vector3.Distance(hit.point, boundaryPoint) > 0.3f) {
                    // Don't continue...
                    return true;
                }


                // Update hitPoint
                if (dir == -1)
                {
                    hitPoint.overruledRight = origin;
                    origin.overruledLeft = hitPoint;
                }
                else {
                    hitPoint.overruledLeft = origin;
                    origin.overruledRight = hitPoint;
                }

                // 4. AddPoint
                registeredPoints.Add(hitPoint);
                other.AddPoint(hitPoint);
            }


            return true;
        }
        return false;
    }

    // DUPLICATE CODE - see HairSquichy
    float sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }


    public static NavCube FindNavCube(Collider2D collider) {
        NavCube cube = collider.GetComponent<NavCube>();
        if (cube == null) {
            NavCubeRef navref = collider.GetComponent<NavCubeRef>();
            if (navref!=null) {
                cube = navref.cube;
            }
        }
        return cube;
    }
}
