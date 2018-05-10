using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class IALegs : MonoBehaviour {

    public IABrain brain;

    public NavMeshAgent agent;

    public Transform currentTarget;
    public bool stopIfCanBeSeen;

    private void Start()
    {
        if (brain == null) brain = GetComponent<IABrain>();
    }

    public void SetDestination(Transform target, bool stopIfCanBeSeen)
    {
        currentTarget = target;
        this.stopIfCanBeSeen = stopIfCanBeSeen;
    }

    //To do
    private void Update()
    {
        RotationUpdate();
    }

    void RotationUpdate()
    {
        if (brain.eyes.HasTargetOnSight())
        {

        }
    }
}
