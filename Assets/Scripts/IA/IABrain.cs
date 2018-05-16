using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IABrain : MonoBehaviour {

    [System.Serializable]
    internal class DecisionTime
    {
        public IAState state;
        public float internalStateUpdateTime;
    }

    public bool isActiveState;
    public enum IAState { WORKING, TALKING, IDLE, SPOT, FREEZE, ALERT, DANGER, PRUDENCE }
    public IAState currentState;

    [Header("Plugs")]
    public IAEyes eyes;
    public IAMouth mouth;
    public IAHears hears;
    public IALegs legs;
    public IAArms arms;

    [Header("Targets")]
    Zone zoneTarget;
    Transform liveTarget;
    List<Transform> pendingCheckers;
    public int minZoneToVisit = 3;

    [Header("Memory")]
    List<IAInformation> hardMemory;
    List<IAInformation> softMemory;
    List<IAInformation> consultableMemory;
    IAInformation informationToCommunicate;
    float lastSoftMemoryAccess;
    public int maxMemory;
    public AnimationCurve memoryPerformance;

    [Header("Decisions")]
    [SerializeField]
    List<DecisionTime> decisionTime;
    float[] decisionTimeArray;
    float lastDecision = -100f;

	// Use this for initialization
	void Start () {
        hardMemory = new List<IAInformation>();
        softMemory = new List<IAInformation>();
        pendingCheckers = new List<Transform>();
        currentState = IAState.IDLE;
        decisionTimeArray = new float[System.Enum.GetValues(typeof(IAState)).Length];
        for (int i=0; i<decisionTimeArray.Length; i++)
        {
            decisionTimeArray[i] = decisionTime.Find(c => c.state == (IAState)i).internalStateUpdateTime;
        }
	}
	
	// Update is called once per frame
	void Update () {

        //Information constant
        if (informationToCommunicate != null)
        {
            if (!hears.IsListening() && !mouth.IsTalking())
            {
                mouth.Say(informationToCommunicate);
                informationToCommunicate = null;
            }
        }


        //Look constant
        eyes.LookToEnemy();

        if(IsPassiveState() && eyes.HasTargetOnSight())
        {
            mouth.Say(null);
            currentState = IAState.SPOT;
        }

        //Process state
        switch (currentState)
        {
            case IAState.IDLE:
                IdleStateUpdate();
                break;
            case IAState.WORKING:
                WorkingStateUpdate();
                break;
        }
	}

    public void IdleStateUpdate()
    {
        if(isTimeToMakeDecision(IAState.IDLE) && informationToCommunicate == null && !mouth.IsTalking())
        {
            List<Zone> validZones = GetValidZoneToSearch();
            zoneTarget = validZones[Random.Range(0, validZones.Count)];
            informationToCommunicate = new IAInformation(IAInformation.InformationType.SEARCHZONE, zoneTarget.name);
            RegisterMemory(informationToCommunicate, true);

            legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
            currentState = IAState.WORKING;
        }
    }

    List<Zone> GetValidZoneToSearch()
    {
        List<Zone> validZoneToVisit = new List<Zone>();
        validZoneToVisit.AddRange(ZoneManager.instance.allZones);
        consultableMemory = AccessSoftMemory();

        foreach (IAInformation information in consultableMemory)
        {
            if (information.type == IAInformation.InformationType.ZONECLEAR || information.type == IAInformation.InformationType.SEARCHZONE)
            {
                Zone designatedZone = ZoneManager.instance.allZones.Find(c => c.name == information.parameters);
                if (validZoneToVisit.Contains(designatedZone) && validZoneToVisit.Count >= minZoneToVisit)
                {
                    validZoneToVisit.Add(designatedZone);
                } 
            }
        }

        return validZoneToVisit;
    }
    
    int checkerCount;
    public void WorkingStateUpdate()
    {
        if(zoneTarget != null)
        {
            checkerCount = pendingCheckers.Count;
            eyes.ProcessCheckers(ref pendingCheckers);

            if(checkerCount != pendingCheckers.Count)
            {
                HaveMadeDecision();
            }

            if(isTimeToMakeDecision(IAState.WORKING))
            {
                if(checkerCount > 0)
                {
                    legs.SetDestination(pendingCheckers[0], IALegs.Speed.WALK, true, 0f);
                }
                else
                {
                    informationToCommunicate = new IAInformation(IAInformation.InformationType.ZONECLEAR, zoneTarget.zoneName);
                    zoneTarget = null;
                    currentState = IAState.IDLE;
                }
                HaveMadeDecision();
            }
        }
    }

    public void ProcessInformation(IAInformation information)
    {
        switch(information.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                //Captain will verify this order
                break;
            case IAInformation.InformationType.ZONECLEAR:
                if(information.parameters == zoneTarget.zoneName)
                {
                    zoneTarget = null;
                    currentState = IAState.IDLE;
                }
                break;
            case IAInformation.InformationType.ORDER:
                break;
        }
    }

    public void RegisterMemory(IAInformation information, bool directToBrain = false)
    {
        if (directToBrain) information.completion = 1f;
        if (information.completion >= 1f || Random.Range(0f, 1f) <= information.completion)
        {
            hardMemory.Insert(0, information);
            ProcessInformation(information);
        }

        if (hardMemory.Count > maxMemory)
        {
            hardMemory.Remove(hardMemory.FindLast(c => true));
        }
    }

    public List<IAInformation> AccessSoftMemory()
    {
        if(lastSoftMemoryAccess < Time.time)
        {
            lastSoftMemoryAccess = Time.time;
            softMemory.Clear();
            ComputeSoftMemory();
        }
        return softMemory;
    }

    public void ComputeSoftMemory()
    {
        float time = Time.time;
        foreach(IAInformation info in hardMemory)
        {
            if(Random.Range(0f, 0.99f) <= memoryPerformance.Evaluate(time - info.timeReceived) * info.completion)
            {
                softMemory.Add(info);
            }
        }
    }



    void HaveMadeDecision()
    {
        lastDecision = Time.time;
    }

    bool isTimeToMakeDecision(IAState state)
    {
        return Time.time > lastDecision + GetInternalStateDecision(state);
    }

    public float GetInternalStateDecision(IAState state)
    {
        return decisionTimeArray[(int)state];
    }

    public bool IsPassiveState()
    {
        return currentState == IAState.IDLE || currentState == IAState.TALKING || currentState == IAState.WORKING;
    }
}
