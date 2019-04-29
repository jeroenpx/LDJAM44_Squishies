using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobGrow : MonoBehaviour
{
    

    public float totalTime = 2f;

    private float startTime;

    public Transform scaleOuterItem;
    private Vector3 scaleSizeStart = Vector3.one;
    public Vector3 scaleSizeEnd = 5f*Vector3.one;
    public float hairSmallerFactor = .5f;
    public float massFactor = 10f;

    public GameObject[] hideEyes;
    public GameObject[] showEyes;

    private int layerWalkable ;

    // Start is called before the first frame update
    void Start()
    {
        layerWalkable = LayerMask.NameToLayer("walkable");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Utility method...
    float GetPercent(float startPercent, float endPercent) {
        return Mathf.Clamp01((Time.time - startTime - totalTime * startPercent) / ((endPercent - startPercent) * totalTime));
    }

    float FunnyAnim(float t) {
        return (Mathf.Sin(t*Mathf.PI-Mathf.PI/2f)+1)/2f;
    }

    IEnumerator ResizeCoroutine() {
        // Initial state
        scaleSizeStart = scaleOuterItem.localScale;

        // Need to scall the joints...
        SpringJoint2D[] joints = GetComponentsInChildren<SpringJoint2D>();
        float[] jointDistance = new float[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            jointDistance[i] = joints[i].distance;
        }

        // Need to scale the hair...
        HairAnim[] hairs = GetComponentsInChildren<HairAnim>();
        Vector3[] initialHairScale = new Vector3[hairs.Length];
        for (int i = 0; i < hairs.Length; i++) {
            initialHairScale[i] = hairs[i].transform.localScale;
        }

        // Need to make the rigidbodies heavier...
        Rigidbody2D[] rigids = GetComponentsInChildren<Rigidbody2D>();
        float[] rigidsMass = new float[rigids.Length];
        for (int i = 0; i < rigids.Length; i++) {
            rigidsMass[i] = rigids[i].mass;
        }


        float percent = 0f;
        while (percent<1) {
            percent = GetPercent(0f, 1f);

            // Resize container
            scaleOuterItem.localScale = (FunnyAnim(percent) * (scaleSizeEnd - scaleSizeStart) + scaleSizeStart);

            // Update joints
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].distance = FunnyAnim(percent) * (4f /* 5 - 1 */ * jointDistance[i]) + jointDistance[i];
            }

            // Resize hair Not so much...
            for (int i = 0; i < hairs.Length; i++)
            {
                hairs[i].transform.localScale = initialHairScale[i]*(1 - percent*hairSmallerFactor);
                foreach (GameObject obj in showEyes)
                {
                    obj.transform.localScale = Vector3.one * (1 - percent * hairSmallerFactor);
                }
                foreach (GameObject obj in hideEyes)
                {
                    obj.transform.localScale = Vector3.one * (1 - percent * hairSmallerFactor);
                }
            }

            // Become heavier as well...
            for (int i = 0; i < rigids.Length; i++)
            {
                rigids[i].mass = rigidsMass[i]*(1+percent*massFactor);
            }

            yield return null;
        }

        // At the end, change my physics layer...
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D coll in colliders) {
            coll.gameObject.layer = layerWalkable;
        }

        // Enable the NavCube on myself
        NavCube cube = GetComponentInChildren<NavCube>();
        cube.enabled = true;

    }

    IEnumerator BecomeStronger()
    {
        yield return null;
        // TODO: heavier too?
        /*
        SpringJoint2D[] joints = GetComponentsInChildren<SpringJoint2D>();
        float[] originalFrequency = new float[joints.Length];
        float[] newFrequency = new float[joints.Length];

        for (int i=0;i<joints.Length;i++)
        {
            originalFrequency[i] = joints[i].frequency;
            newFrequency[i] = joints[i].frequency*200f;// FACTOR...
        }

        float percent = 0f;
        while (percent < 1)
        {
            percent = GetPercent(0f, .3f);

            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].frequency = percent*(newFrequency[i]- originalFrequency[i]) + originalFrequency[i];

            }

            yield return null;
        }*/
    }

    IEnumerator MoveIntoPosition()
    {
        float percent = 0f;
        while (percent < 1)
        {
            percent = GetPercent(0f, 1f);

            yield return null;
        }
    }

    IEnumerator ChangeColor()
    {
        float percent = 0f;
        while (percent < 1)
        {
            percent = GetPercent(0f, 1f);

            yield return null;
        }
    }

    IEnumerator ChangeOther()
    {
        yield return new WaitForSeconds(totalTime*.1f);
        foreach (GameObject obj in showEyes) {
            obj.SetActive(true);
        }
        foreach (GameObject obj in hideEyes)
        {
            obj.SetActive(false);
        }

        // TODO: Pop sound
    }

    public void Grow() {
        startTime = Time.time;
        StartCoroutine(BecomeStronger());
        StartCoroutine(ResizeCoroutine());
        StartCoroutine(MoveIntoPosition());
        StartCoroutine(ChangeColor());
        StartCoroutine(ChangeOther());
    }
}
