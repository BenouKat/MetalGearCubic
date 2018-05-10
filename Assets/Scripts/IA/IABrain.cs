using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IABrain : MonoBehaviour {

    public bool isActiveState;
    public enum IAState { WORKING, TALKING, IDLE, SPOT, FREEZE, ALERT, DANGER, PRUDENCE }
    public IAState currentState;

    [Header("Plugs")]
    public IAEyes eyes;
    public IAMouth mouth;
    public IAHears hears;
    public IALegs legs;

    [Header("Targets")]
    Zone zoneTarget;
    Transform liveTarget;
    List<Transform> pendingCheckers;

    [Header("Memory")]
    List<IAInformation> memory;


	// Use this for initialization
	void Start () {
        memory = new List<IAInformation>();
        pendingCheckers = new List<Transform>();
        currentState = IAState.IDLE;
	}
	
	// Update is called once per frame
	void Update () {

        eyes.LookToEnemy();

        switch (currentState)
        {
            case IAState.WORKING:
                WorkingStateUpdate();
                break;
        }
	}

    float lastCheckerRemoved;
    int checkerCount;
    public void WorkingStateUpdate()
    {
        if(zoneTarget != null)
        {
            checkerCount = pendingCheckers.Count;
            eyes.ProcessCheckers(ref pendingCheckers);

            if(checkerCount != pendingCheckers.Count)
            {
                lastCheckerRemoved = Time.time;
            }

            if(Time.time > lastCheckerRemoved + 1f)
            {

            }
        }
    }
}
