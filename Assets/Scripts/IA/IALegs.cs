using System;
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
    bool hasDestinationReached;
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

    public float GetEnemyVelocity()
    {
        return agent.velocity.magnitude;
    }

    public void SetDestinationToClosest(List<Transform> targets, Speed speed, bool stopIfCanBeSeen = false, float stopDistance = 0f)
    {
        NavMeshPath path = new NavMeshPath();
        Transform selectedTarget = null;
        float distanceMin = Mathf.Infinity;
        float currentDistance = 0f;
        foreach(Transform target in targets)
        {
            agent.CalculatePath(target.position, path);
            currentDistance = 0f;
            if(path.corners.Length < 2)
            {
                currentDistance = 0f;
            }
            else
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    currentDistance += (path.corners[i] - path.corners[i + 1]).sqrMagnitude;
                }

                if(currentDistance < distanceMin)
                {
                    selectedTarget = target;
                    distanceMin = currentDistance;
                }
            }
        }
        
        if(selectedTarget != null)
        {
            SetDestination(selectedTarget, speed, stopIfCanBeSeen, stopDistance);
        }
        else
        {
            Debug.LogWarning("Path hasn't found a selected target... No destination has been set");
        }
    }

    public void SetDestination(Transform target, Speed speed, bool stopIfCanBeSeen = false, float stopDistance = 0f)
    {
        currentTarget = target;
        this.stopIfCanBeSeen = stopIfCanBeSeen;
        this.isDestinationMoving = stopDistance > 0f;

        agent.stoppingDistance = stopDistance;
        agent.speed = GetSpeedValue(speed);
        hasDestinationSet = false;
        hasDestinationReached = false;
    }
    
    public float GetSpeedValue(Speed speed)
    {
        return speedValues[(int)speed];
    }
    
    private void Update()
    {
        RotationUpdate();

        PositionUpdate();
    }

    Vector3 lookAtPosition;
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
        }
        else if(agent.isStopped && stopIfCanBeSeen && hasDestinationReached)
        {
            if (agent.updateRotation)
            {
                agent.updateRotation = false;
                rotationHelper.position = transform.position;
                lookAtPosition = currentTarget.position;
                lookAtPosition.y = transform.position.y;
                rotationHelper.LookAt(lookAtPosition);
            }
        }
        else
        {
            agent.updateRotation = true;
        }

        if(!agent.updateRotation)
        {
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, rotationHelper.rotation, speedRotation);
        }
    }

    void PositionUpdate()
    {
        if (stopIfCanBeSeen && brain.eyes.CanBeSeen(currentTarget, brain.eyes.viewDistance, currentTarget.gameObject.layer))
        {
            agent.isStopped = true;
            if (!isDestinationMoving) hasDestinationReached = true;

        } else if (isDestinationMoving || !hasDestinationSet)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            hasDestinationSet = true;

        }else if(!isDestinationMoving && agent.remainingDistance <= agent.stoppingDistance && agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            hasDestinationReached = true;
        }
    }
}
