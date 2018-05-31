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
    bool hasSetDestination;
    bool hasReachedDestination;
    [Range(0f, 1f)]
    public float speedRotation;
    float seenDistance;

    public enum Speed { SNEAK, WALK, RUN, DASH }
    public float[] speedValues = new float[4] { 0.5f, 2f, 5f, 7f };

    private void Start()
    {
        pathFromDistance = new NavMeshPath();
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
        Transform selectedTarget = null;
        float distanceMin = Mathf.Infinity;
        float currentDistance = 0f;
        foreach(Transform target in targets)
        {
            currentDistance = GetDistanceFrom(target);
            if (currentDistance < distanceMin)
            {
                selectedTarget = target;
                distanceMin = currentDistance;
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

    NavMeshPath pathFromDistance;
    float distanceFrom = 0f;
    public float GetDistanceFrom(Transform target)
    {
        agent.CalculatePath(target.position, pathFromDistance);
        distanceFrom = 0f;
        if (pathFromDistance.corners.Length < 2)
        {
            return 0f;
        }
        else
        {
            for (int i = 0; i < pathFromDistance.corners.Length - 1; i++)
            {
                distanceFrom += (pathFromDistance.corners[i] - pathFromDistance.corners[i + 1]).sqrMagnitude;
            }

            return distanceFrom;
        }
    }

    public void StopDestination()
    {
        currentTarget = null;
        agent.isStopped = true;
        hasReachedDestination = true;
    }

    public void SetDestination(Transform target, Speed speed, bool stopIfCanBeSeen = false, float seenDistance = 0f, float stopDistance = 0f)
    {
        currentTarget = target;
        this.stopIfCanBeSeen = stopIfCanBeSeen;
        this.isDestinationMoving = stopDistance > 0f;
        this.seenDistance = seenDistance <= 0f ? brain.eyes.spotDistance : seenDistance;

        agent.stoppingDistance = stopDistance;
        agent.speed = GetSpeedValue(speed);
        hasSetDestination = false;
        hasReachedDestination = false;
    }
    
    public float GetSpeedValue(Speed speed)
    {
        return speedValues[(int)speed];
    }
    
    private void Update()
    {
        if(currentTarget != null)
        {
            RotationUpdate();

            PositionUpdate();
        }
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
        else if(agent.isStopped && stopIfCanBeSeen && hasReachedDestination)
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
        if (stopIfCanBeSeen && brain.eyes.CanBeSeen(currentTarget, seenDistance, currentTarget.gameObject.layer))
        {
            agent.isStopped = true;
            if (!isDestinationMoving) hasReachedDestination = true;

        } else if (isDestinationMoving || !hasSetDestination)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            hasSetDestination = true;

        }else if(!isDestinationMoving && agent.remainingDistance <= agent.stoppingDistance && agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            hasReachedDestination = true;
        }
    }

    public bool IsDestinationReached()
    {
        return hasReachedDestination;
    }
}
