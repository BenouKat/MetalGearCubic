using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodObject : MonoBehaviour {
    
    float timeAppeared;
    float timeBeforeDisappear;
    float timeDisappear;
    float timePast;
    MeshRenderer render;
    Rigidbody rigidB;
    BoxCollider boxCollider;

    bool initialized = false;
    Vector3 startScale;
    Vector3 oldPosition;

    enum PostionalState { MOVING, STATIONARY, STATIC }
    PostionalState state;
    float lastTimeStatic = 1f;
    float timeStaticCheck = 0f;

    //Initialize the blood (or the body part)
    public void Init(float timeDisappear, float timeBeforeDisappear, Material bloodMaterial)
    {
        render = GetComponent<MeshRenderer>();
        if (bloodMaterial != null)
        {
            render.sharedMaterial = bloodMaterial;
            render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        timeAppeared = Time.time;
        this.timeDisappear = timeDisappear;
        //We set +1 to prevent the initialization occurs in the same frame where the blood is separate from the body.
        this.timeBeforeDisappear = timeBeforeDisappear + 1f;
        enabled = true;
        
        //Stationary state init
        oldPosition = Vector3.zero;
        state = PostionalState.MOVING;
        lastTimeStatic = -1f;
        timeStaticCheck = Time.time - Random.Range(0f, BloodManager.instance.maxTimeCheckingState);
        
    }
    
    //Pool variable for avoid Time.time multiple slow call
    float currentTime;
    private void Update()
    {
        currentTime = Time.time;

        //If it's off camera and time of apparition + delay is past
        if (!render.isVisible && currentTime > timeAppeared + timeBeforeDisappear)
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
            if (transform.localScale.x <= BloodManager.instance.subdivisionMinSize) //At a certain size, we destroy the object
            {
                DisableObject();
            }
        }

        //If the object is not static yet
        if (state != PostionalState.STATIC)
        {
            //Check if in stationary state or if it's time to check
            if(state == PostionalState.STATIONARY || currentTime > timeStaticCheck + BloodManager.instance.maxTimeCheckingState)
            {
                //Checking distance from old position
                if (Vector3.Distance(oldPosition, transform.position) < BloodManager.instance.minDistanceForStatic)
                {
                    //If it was moving it's stationary
                    if (state == PostionalState.MOVING)
                    {
                        state = PostionalState.STATIONARY;
                        lastTimeStatic = currentTime;
                    } //if it's already stationary, we check if it's stationary since a long time
                    else if(currentTime > lastTimeStatic + BloodManager.instance.timeStateStationary)
                    {
                        state = PostionalState.STATIC;
                        DisablePhysicObject();
                    }
                }
                else //It's still moving
                {
                    state = PostionalState.MOVING;
                    oldPosition = transform.position;
                }

                if (state == PostionalState.MOVING)
                {
                    //Too far away onto the ground
                    if (oldPosition.y <= BloodManager.instance.maxUndergroundDistance)
                    {
                        DisableObject();
                    }
                    timeStaticCheck = currentTime;
                }
            }
        }

    }

    //Disable the physics part of the object
    public void DisablePhysicObject()
    {
        if (rigidB == null) rigidB = GetComponent<Rigidbody>();
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
        if (rigidB != null)
        {
            rigidB.velocity = Vector3.zero;
            rigidB.isKinematic = true;
            rigidB.useGravity = false;
        }
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    //Disable the object
    public void DisableObject()
    {
        if (gameObject.name.StartsWith("[Pool]"))
        {
            DisablePhysicObject();
            BloodManager.instance.GetBackPoolObject(gameObject);
            enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
