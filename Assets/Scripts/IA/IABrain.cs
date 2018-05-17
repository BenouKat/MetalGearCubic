using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(IABrain))]
class IABrainEditor : Editor
{
    public void OnSceneGUI()
    {
        IABrain brain = ((IABrain)target);

        //Handles.DrawSolidRectangleWithOutline(new Rect(brain.transform.position + (Vector3.up * 4f), Vector2.one*5f), Color.black, Color.clear);
        Handles.Label(brain.transform.position + (Vector3.up * 1.5f), brain.currentState.ToString());

        if(brain.GetZoneTarget() != null)
        {
            brain.GetZoneTarget().DrawZone(true);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            Handles.color = Color.red;
            foreach (Transform checker in brain.GetCheckers())
            {
                if (checker != null)
                {
                    Handles.DrawSolidDisc(checker.position + Vector3.up * 0.6f, checker.up, 0.2f);
                }
            }
        }

        brain.eyes.DrawEyesEditor();

        foreach(IAInformation info in brain.GetSoftMemory())
        {
            if(info.type == IAInformation.InformationType.SEARCHZONE || info.type == IAInformation.InformationType.ZONECLEAR)
            {
                Zone zoneDisplayed = ZoneManager.instance.allZones.Find(c => c.zoneName == info.parameters);
                if(zoneDisplayed != null)
                {
                    Vector3[] vects = new Vector3[4]
                    {
                        new Vector3((zoneDisplayed.transform.position.x + (zoneDisplayed.transform.localScale.x/2f)), 3f, (zoneDisplayed.transform.position.z + (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x - (zoneDisplayed.transform.localScale.x/2f)), 3f, (zoneDisplayed.transform.position.z + (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x - (zoneDisplayed.transform.localScale.x/2f)), 3f, (zoneDisplayed.transform.position.z - (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x + (zoneDisplayed.transform.localScale.x/2f)), 3f, (zoneDisplayed.transform.position.z - (zoneDisplayed.transform.localScale.z/2f)))
                    };
                    Color color = info.type == IAInformation.InformationType.SEARCHZONE ? new Color(1f, 1f, 0f, 1f) : new Color(0f, 1f, 0f, 1f);
                    color.a = brain.memoryPerformance.Evaluate(Time.time - info.timeReceived) * info.completion;

                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                    Handles.DrawSolidRectangleWithOutline(vects, color, Color.clear);

                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    Handles.DrawSolidRectangleWithOutline(vects, Color.clear, color);
                }
            }
        }
    }
}
#endif

public class IABrain : MonoBehaviour {

    [System.Serializable]
    internal class DecisionTime
    {
        public IAState state = IAState.IDLE;
        public float internalStateUpdateTime = 1f;

        public DecisionTime(IAState state)
        {
            this.state = state;
        }
    }

    public bool isActiveState;
    public enum IAState { WORKING, TALKING, IDLE, SPOT, FREEZE, ALERT, DANGER, PRUDENCE }
    public IAState currentState;

    [Header("Plugs")]
    public IAEyes eyes;
    public IAMouth mouth;
    public IAEars hears;
    public IALegs legs;
    public IAArms arms;

    [Header("Targets")]
    Zone zoneTarget;
    Transform liveTarget;
    List<Transform> pendingCheckers;
    public int minZoneToVisit = 3;

    [Header("Memory")]
    List<IAInformation> hardMemory = new List<IAInformation>();
    List<IAInformation> softMemory = new List<IAInformation>();
    List<IAInformation> consultableMemory = new List<IAInformation>();
    IAInformation informationToCommunicate = null;
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
            DecisionTime decision = decisionTime.Find(c => c.state == (IAState)i);
            if(decision == null)
            {
                decision = new DecisionTime((IAState)i);
                decisionTime.Add(decision);
            }
            decisionTimeArray[i] = decision.internalStateUpdateTime;
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
                HaveMadeDecision();
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

    #region State
    public void IdleStateUpdate()
    {
        if(isTimeToMakeDecision(IAState.IDLE) && informationToCommunicate == null && !mouth.IsTalking())
        {
            List<Zone> validZones = GetValidZoneToSearch();
            zoneTarget = validZones[Random.Range(0, validZones.Count)];
            informationToCommunicate = new IAInformation(IAInformation.InformationType.SEARCHZONE, 2f, zoneTarget.name);
            RegisterMemory(informationToCommunicate, true);

            legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
            currentState = IAState.WORKING;
            HaveMadeDecision();
        }
    }
    
    int checkerCount;
    public void WorkingStateUpdate()
    {
        if(zoneTarget != null && (zoneTarget.IsInsideZone(transform.position) || legs.IsDestinationReached()))
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
                    informationToCommunicate = new IAInformation(IAInformation.InformationType.ZONECLEAR, 3f, zoneTarget.zoneName);
                    zoneTarget = null;
                    currentState = IAState.IDLE;
                }
                HaveMadeDecision();
            }
        }
    }
    #endregion

    #region Memory
    public void RegisterMemory(IAInformation information, bool directToBrain = false)
    {
        if (directToBrain) information.completion = 1f;
        if (information.completion >= 1f || Random.Range(0f, 1f) <= information.completion)
        {
            hardMemory.Insert(0, information);
            if(!directToBrain) ProcessInformation(information);
        }

        if (hardMemory.Count > maxMemory)
        {
            hardMemory.Remove(hardMemory.FindLast(c => true));
        }
    }

    public void ProcessInformation(IAInformation information)
    {
        switch (information.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                //Captain will verify this order
                break;
            case IAInformation.InformationType.ZONECLEAR:
                if (information.parameters == zoneTarget.zoneName)
                {
                    zoneTarget = null;
                    currentState = IAState.IDLE;
                }
                break;
            case IAInformation.InformationType.ORDER:
                break;
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
    #endregion

    #region Decisions
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
    #endregion

    #region Utils
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
    #endregion

    #region Editor Getters
#if UNITY_EDITOR
    public Zone GetZoneTarget()
    {
        return zoneTarget;
    }

    public List<Transform> GetCheckers()
    {
        return pendingCheckers;
    }

    public List<IAInformation> GetSoftMemory()
    {
        return softMemory;
    }
#endif
    #endregion
}
