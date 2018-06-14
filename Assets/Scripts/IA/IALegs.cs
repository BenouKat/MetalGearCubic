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
    bool lookAtTarget;
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

    //Get multiple targets and set the destination to the closest
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

    //Get the length of a Nav Mesh Path
    NavMeshPath pathFromDistance;
    float distanceFrom = 0f;
    public float GetDistanceFrom(Transform target)
    {
        //Calculate the path
        agent.CalculatePath(target.position, pathFromDistance);
        distanceFrom = 0f;
        //If the corners has less than to corner, the distance is 0
        if (pathFromDistance.corners.Length < 2)
        {
            return 0f;
        }
        else
        {
            //Simple addition for squared distance (enough to make comparaison)
            for (int i = 0; i < pathFromDistance.corners.Length - 1; i++)
            {
                distanceFrom += (pathFromDistance.corners[i] - pathFromDistance.corners[i + 1]).sqrMagnitude;
            }

            return distanceFrom;
        }
    }

    //Cancel the destination of the agent
    public void CancelDestination()
    {
        currentTarget = null;
        agent.isStopped = true;
        hasReachedDestination = true;
        lookAtTarget = false;
    }

    //Set the destination of the age,t
    public void SetDestination(Transform target, Speed speed, bool stopIfCanBeSeen = false, float seenDistance = 0f, float stopDistance = 0f)
    {
        currentTarget = target;
        //Stop if can be seen means that if the agent see the target at the seen distance, it stops
        this.stopIfCanBeSeen = stopIfCanBeSeen;
        //The seen distance max of the current target
        this.seenDistance = seenDistance <= 0f ? brain.eyes.spotDistance : seenDistance;
        //If the stop distance > 0, it means that the destination is moving, else we use the two variable above
        this.isDestinationMoving = stopDistance > 0f;

        //Set the variables
        agent.stoppingDistance = stopDistance;
        agent.speed = GetSpeedValue(speed);
        hasSetDestination = false;
        hasReachedDestination = false;
        lookAtTarget = false;
    }

    //Lock the direction on target
    public void TurnToTarget(Transform target)
    {
        lookAtTarget = true;
        currentTarget = target;
    }

    //Stop the direction
    public void StopTurnToTarget()
    {
        lookAtTarget = false;
        currentTarget = null;
    }
    
    public float GetSpeedValue(Speed speed)
    {
        return speedValues[(int)speed];
    }
    
    private void Update()
    {
        //If the target exist
        if(currentTarget != null)
        {
            //Update rotation any time
            RotationUpdate();

            //If not locking the direction to the target, we move
            if(!lookAtTarget) PositionUpdate();
        }
    }

    Vector3 lookAtPosition;
    void RotationUpdate()
    {
        //If we have the enemy on sight or lock eyes to target
        if (brain.eyes.HasTargetOnSight() || lookAtTarget)
        {
            //We stop the auto rotation of the agent
            if(agent.updateRotation)
            {
                agent.updateRotation = false;
            }
            //We rotate
            rotationHelper.position = transform.position;
            rotationHelper.LookAt(lookAtTarget ? currentTarget : brain.eyes.GetEyesTarget());
        }
        //If the agent is stop, can see the target and have reached destination
        else if(agent.isStopped && stopIfCanBeSeen && hasReachedDestination)
        {
            //We rotate
            if (agent.updateRotation)
            {
                agent.updateRotation = false;
                rotationHelper.position = transform.position;
                lookAtPosition = currentTarget.position;
                lookAtPosition.y = transform.position.y;
                rotationHelper.LookAt(lookAtPosition);
            }
        }
        else //Else we let the agent update its rotation
        {
            agent.updateRotation = true;
        }

        //If it's not auto, we update the rotation manually
        if(!agent.updateRotation)
        {
            agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, rotationHelper.rotation, speedRotation);
        }
    }

    //Positional update
    void PositionUpdate()
    {
        //If the target can be seen at seen distance
        if (stopIfCanBeSeen && brain.eyes.CanBeSeen(currentTarget, seenDistance, currentTarget.gameObject.layer))
        {
            //Stop
            agent.isStopped = true;
            if (!isDestinationMoving) hasReachedDestination = true;

        //If the target is moving or we didn't set the destination, we set the destination
        } else if (isDestinationMoving || !hasSetDestination)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            hasSetDestination = true;

        //If the destination is not moving and the remaining distance is less than the stop distance, the destination is reached
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
