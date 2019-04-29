using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsTouching : MonoBehaviour
{
    private int isTouching;
    private float timeStartedTouching;

    // Start is called before the first frame update
    void Start()
    {
        isTouching = 0;
    }

    public float GetDurationTouching() {
        if (isTouching > 0)
        {
            return Time.time - timeStartedTouching;
        }
        else {
            return -1;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isTouching++;

        if (isTouching == 1) {
            timeStartedTouching = Time.time;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isTouching--;
    }
}
