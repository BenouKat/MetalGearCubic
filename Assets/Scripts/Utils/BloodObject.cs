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

    public void Init(float timeDisappear, float timeBeforeDisappear, Material bloodMaterial)
    {
        render = GetComponent<MeshRenderer>();
        if(bloodMaterial != null)
        {
            render.sharedMaterial = bloodMaterial;
        }
        timeAppeared = Time.time;
        this.timeDisappear = timeDisappear;
        this.timeBeforeDisappear = timeBeforeDisappear + 1f;
    }

    // Update is called once per frame
    void Update () {
        
		if(Time.time > timeAppeared + timeBeforeDisappear && !render.isVisible)
        {
            timePast += Time.deltaTime;
            if (!initialized)
            {
                startScale = transform.localScale;
                initialized = true;
            }

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, timePast / timeDisappear);
            if(transform.localScale.x <= 0.0001f)
            {
                Destroy(gameObject);
            }
        }
	}
}
