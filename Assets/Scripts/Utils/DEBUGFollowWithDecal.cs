using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEBUGFollowWithDecal : MonoBehaviour {

    public Transform toFollow;

    Vector3 decal;
	// Use this for initialization
	void Start () {
	    if(toFollow != null)
        {
            decal = transform.position - toFollow.transform.position;
        }
	}
	
	// Update is called once per frame
	void Update () {
        if(toFollow != null)
        {
            transform.position = toFollow.transform.position + decal;
        }
	}
}
