using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodObject : MonoBehaviour {
    
    float timeAppeared;
    float timeBeforeDisappear;
    float timeDisappear;
    float timePast;
    MeshRenderer render;

    bool initialized = false;
    Vector3 startScale;

    //Initialize the blood (or the body part)
    public void Init(float timeDisappear, float timeBeforeDisappear, Material bloodMaterial)
    {
        render = GetComponent<MeshRenderer>();
        if(bloodMaterial != null)
        {
            render.sharedMaterial = bloodMaterial;
        }
        timeAppeared = Time.time;
        this.timeDisappear = timeDisappear;
        //We set +1 to prevent the initialization occurs in the same frame where the blood is separate from the body.
        this.timeBeforeDisappear = timeBeforeDisappear + 1f;
    }

    // Update is called once per frame
    void Update () {
        
        //If it's off camera and time of apparition + delay is past
		if(!render.isVisible && Time.time > timeAppeared + timeBeforeDisappear)
        {
            //If not initialized, we set the start scale
            timePast += Time.deltaTime;
            if (!initialized)
            {
                startScale = transform.localScale;
                initialized = true;
            }

            //We do the lerp !
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timePast / timeDisappear);
            if(transform.localScale.x <= 0.0001f) //At a certain size, we destroy the object
            {
                Destroy(gameObject);
            }
        }
	}
}
