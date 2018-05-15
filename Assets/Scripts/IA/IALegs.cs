using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IALegs : MonoBehaviour {

    public IABrain brain;

    public NavMeshAgent agent;
    Transform rotationHelper;

    public Transform currentTarget;
    public bool isDestinationMoving;
    public bool stopIfCanBeSeen;
    bool hasDestinationSet;
    public float defaultStopDistance;
    [Range(0f, 1f)]
    public float speedRotation;

    public enum Speed { SNEAK, WALK, RUN, DASH }
    public float[] speedValues = new float[4] { 0.5f, 2f, 5f, 7f };

    private void Start()
    {
        if (brain == null) brain = GetComponent<IABrain>();
        rotationHelper = (InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "RotationHelper")).transform;
        rotationHelper.transform.rotation = transform.rotation;
    }

    public void SetDestination(Transform target, Speed speed, bool stopIfCanBeSeen, bool isDestinationMoving)
    {
        currentTarget = target;
        this.stopIfCanBeSeen = stopIfCanBeSeen;
        this.isDestinationMoving = isDestinationMoving;

        agent.stoppingDistance = isDestinationMoving ? defaultStopDistance : 0f;
        agent.speed = GetSpeedValue(speed);
    }
    
    public float GetSpeedValue(Speed speed)
    {
        return speedValues[(int)speed];
    }

    //To do
    private void Update()
    {
        RotationUpdate();

        PositionUpdate();
    }

    void RotationUpdate()
    {
        if (brain.eyes.HasTargetOnSight())
        {
            if(agent.updateRotation)
            {
                agent.updateRotation = false;
                rotationHelper.position = transform.position;
                rotationHelper.LookAt(brain.eyes.GetEyesTarget());
            }
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, rotationHelper.rotation, speedRotation);
        }
        else
        {
            agent.updateRotation = true;
        }
    }

    void PositionUpdate()
    {
        if(stopIfCanBeSeen && brain.eyes.IsOnSpotSight(currentTarget, currentTarget.gameObject.layer))
        {
            agent.isStopped = true;
        } else if (isDestinationMoving || !hasDestinationSet)
        {
            agent.SetDestination(currentTarget.position);
        }
    }
}
