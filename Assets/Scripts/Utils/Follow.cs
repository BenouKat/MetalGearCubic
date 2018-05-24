using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour {

    public Vector3 smoothAxes;
    public Transform target;
    Transform myTransform;

    private void Start()
    {
        myTransform = transform;
    }

    Vector3 tempVector;
	// Update is called once per frame
	void Update () {
        if(target == null)
        {
            enabled = false;
            return;
        }

        tempVector.x = Mathf.Lerp(myTransform.position.x, target.position.x, smoothAxes.x);
        tempVector.y = Mathf.Lerp(myTransform.position.y, target.position.y, smoothAxes.y);
        tempVector.z = Mathf.Lerp(myTransform.position.z, target.position.z, smoothAxes.z);
        myTransform.position = tempVector;
    }
}
