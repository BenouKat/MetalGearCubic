using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour {

    public Vector3 axis;
	
	// Just rotating an object trought time
	void Update () {
        transform.Rotate(axis * Time.deltaTime, Space.Self);
	}
}
