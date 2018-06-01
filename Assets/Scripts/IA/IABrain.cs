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

        brain.DrawBrainEditor();
        brain.DrawMemoryEditor();
    }
}
#endif

public class IABrain : MonoBehaviour {

    #region Editor Getters
#if UNITY_EDITOR
    public void DrawBrainEditor()
    {
        //Handles.DrawSolidRectangleWithOutline(new Rect(brain.transform.position + (Vector3.up * 4f), Vector2.one*5f), Color.black, Color.clear);
        Handles.Label(transform.position + (Vector3.up * 1.5f), currentState.ToString());

        if (zoneTarget != null)
        {
            zoneTarget.DrawZone(true);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            Handles.color = Color.red;
            foreach (Transform checker in pendingCheckers)
            {
                if (checker != null)
                {
                    Handles.DrawSolidDisc(checker.position + Vector3.up * 0.6f, checker.up, 0.2f);
                }
            }
        }

        eyes.DrawEyesEditor();

        Handles.color = new Color(1f, 1f, 0f, 0.5f);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        if (mouth.IsTalkingToRadio())
        {
            Handles.DrawSolidArc(transform.position + (Vector3.up*0.5f), Vector3.up, Vector3.right, mouth.GetTalkingCompletion()*360f, 2f);
        }

        Handles.color = new Color(0f, 1f, 1f, 0.75f);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        if (ears.IsListeningRadio())
        {
            Handles.DrawSolidArc(transform.position + Vector3.up, Vector3.up, Vector3.right, ears.GetListeningCompletion() * 360f, 1f);
        }
    }

    public void DrawMemoryEditor()
    {
        Handles.color = Color.white;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;

        foreach (IAInformation info in hardMemory)
        {
            if (info.type == IAInformation.InformationType.SEARCHZONE || info.type == IAInformation.InformationType.ZONECLEAR)
            {
                Zone zoneDisplayed = ZoneManager.instance.allZones.Find(c => c.zoneName == info.parameters);
                if (zoneDisplayed != null)
                {
                    Vector3[] vects = new Vector3[4]
                    {
                        new Vector3((zoneDisplayed.transform.position.x + (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z + (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x - (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z + (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x - (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z - (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x + (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z - (zoneDisplayed.transform.localScale.z/2f)))
                    };

                    Color color = info.type == IAInformation.InformationType.SEARCHZONE ? new Color(1f, 1f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
                    color.a = (memoryPerformance.Evaluate(Time.time - info.timeReceived) * info.completion) * 0.5f;

                    if (info.type == IAInformation.InformationType.SEARCHZONE)
                    {
                        Handles.DrawSolidRectangleWithOutline(vects, Color.clear, color);
                    }
                    else
                    {
                        Handles.DrawSolidRectangleWithOutline(vects, color, Color.clear);
                    }
                }
            }
        }
    }
#endif
    #endregion

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

    [System.Serializable]
    internal class BestChecker
    {
        public float maxAngle;
        public float chance;
        [HideInInspector]
        public float maxDistance;
        [HideInInspector]
        public Transform selectedChecker;

        public BestChecker(float angle, float chance)
        {
            this.maxAngle = angle;
            this.chance = chance;
        }
    }
    
    internal class PatrolStatus
    {
        public string unitID;
        public string currentZone;
        public float lastMessage;

        public PatrolStatus(string unitID, string currentZone, float lastMessage)
        {
            this.unitID = unitID;
            this.currentZone = currentZone;
            this.lastMessage = lastMessage;
        }
    }

    public bool isActiveState;
    public enum IAState { WORKING, TALKING, IDLE, CHECKING, SPOT, FREEZE, ALERT, DANGER, PRUDENCE }
    public IAState currentState;
    public enum IABehaviour { PATROL, OFFICER, INTERLEADER, INTERPATROL }
    public IABehaviour behavior;

    [Header("Plugs")]
    public IAEyes eyes;
    public IAMouth mouth;
    public IAEars ears;
    public IALegs legs;
    public IAArms arms;
    public string unitID;

    [Header("Targets")]
    Zone zoneTarget;
    public Zone defaultZone;
    List<Transform> pendingCheckers;
    public int minZoneToVisit = 3;
    [SerializeField]
    List<BestChecker> checkersDivisions;

    [Header("Talking state")]
    Zone meetingTarget;
    Transform talkingTarget;
    
    [Header("Checking state")]
    Transform checkTarget;

    [Header("Memory")]
    List<IAInformation> hardMemory = new List<IAInformation>();
    List<IAInformation> softMemory = new List<IAInformation>();
    List<IAInformation> consultableMemory = new List<IAInformation>();
    List<IAInformation> informationToCommunicate = new List<IAInformation>();
    IAInformation orderWaitingConfirmation;
    float lastSoftMemoryAccess;
    public int maxMemory;
    public AnimationCurve memoryPerformance;

    [Header("Decisions")]
    [SerializeField]
    List<DecisionTime> decisionTime;
    float[] decisionTimeArray;
    float lastDecision = -100f;

    [Header("Officer info")]
    public float confirmationOrderTimeout = 20f;
    public float lostUnitTimeout = 30f;
    List<PatrolStatus> patrolStatus = new List<PatrolStatus>();
    IAInformation attendingInfo = null;
    float timeAttending;

    [Header("Personality")]
    [Range(0f, 100f)]
    public float friendly;
    [Range(0f, 100f)]
    public float orientation;
    [Range(0f, 100f)]
    public float talkative;


    // Use this for initialization
    void Start () {
        unitID = UnitManager.instance.GetNewUnitID(behavior.ToString());
        hardMemory = new List<IAInformation>();
        softMemory = new List<IAInformation>();
        pendingCheckers = new List<Transform>();

        if(!checkersDivisions.Exists(c => c.maxAngle >= 180f))
        {
            checkersDivisions.Add(new BestChecker(180f, 100f));
        }
        checkersDivisions.Sort(delegate (BestChecker bc1, BestChecker bc2)
        {
            return bc1.maxAngle.CompareTo(bc2.maxAngle);
        });

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
        if (!HasNothingToSay())
        {
            if (!ears.IsListeningRadio() && !mouth.IsTalkingToRadio())
            {
                mouth.SayToRadio(informationToCommunicate);
                informationToCommunicate.Clear();
                HaveMadeDecision();
            }
        }
        
        //Look constant
        eyes.LookToEnemy();

        if(IsPassiveState() && eyes.HasTargetOnSight())
        {
            mouth.SayToRadio(null);
            currentState = IAState.SPOT;
        }

        if(behavior == IABehaviour.OFFICER && orderWaitingConfirmation != null)
        {
            if(Time.time - orderWaitingConfirmation.timeCreation > confirmationOrderTimeout)
            {
                currentState = IAState.IDLE;
            }
        }

        //Process state
        switch (currentState)
        {
            case IAState.IDLE:
                switch(behavior)
                {
                    case IABehaviour.PATROL:
                    case IABehaviour.INTERLEADER:
                    case IABehaviour.INTERPATROL:
                        IdleStatePatrolUpdate();
                        break;
                    case IABehaviour.OFFICER:
                        IdleStateOfficerUpdate();
                        break;
                }
                break;
            case IAState.WORKING:
                switch (behavior)
                {
                    case IABehaviour.PATROL:
                    case IABehaviour.INTERLEADER:
                    case IABehaviour.INTERPATROL:
                        WorkingStatePatrolUpdate();
                        break;
                    case IABehaviour.OFFICER:
                        WorkingStateOfficerUpdate();
                        break;
                }
                break;
            case IAState.TALKING:
                TalkingStateUpdate();
                break;
            case IAState.CHECKING:
                CheckingStateUpdate();
                break;
        }
	}

    #region State
    void IdleStatePatrolUpdate()
    {
        if(isTimeToMakeDecision(IAState.IDLE) && HasNothingToSay() && !mouth.IsTalkingToRadio())
        {
            IAInformation order = AccessSoftMemory().Find(c => c.toDo);
            if(order != null && order.type == IAInformation.InformationType.MEETOFFICER)
            {
                AccessSoftMemory().Remove(order);
                meetingTarget = ZoneManager.instance.allZones.Find(c => c.zoneName.Contains("Officer"));
                talkingTarget = UnitManager.instance.GetCurrentOfficer();
                legs.SetDestinationToClosest(meetingTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                currentState = IAState.TALKING;
            }
            else if(order != null && order.type == IAInformation.InformationType.SEARCHZONE)
            {
                AccessSoftMemory().Remove(order);
                SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == order.parameters));
                legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                currentState = IAState.WORKING;
            }
            else
            {
                if (order != null) AccessSoftMemory().Remove(order);
                SetZoneTarget(GetClosestZoneWithErrorRate(GetValidZoneToSearch(), 100f - orientation));
                if(Random.Range(0f, 100f) < talkative)
                {
                    TellInformationToOthers(IAInformation.InformationType.SEARCHZONE, 2f, zoneTarget.zoneName);
                }
                legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                currentState = IAState.WORKING;
            }
            
            HaveMadeDecision();
        }
    }

    void IdleStateOfficerUpdate()
    {
        if (isTimeToMakeDecision(IAState.IDLE) && HasNothingToSay() && !mouth.IsTalkingToRadio() && !ears.IsListeningRadio())
        {
            IAInformation order = hardMemory.Find(c => c.toDo && (c.type == IAInformation.InformationType.DEVIATETOZONE 
                                                                || c.type == IAInformation.InformationType.BRINGTOOFFICER
                                                                || c.type == IAInformation.InformationType.MEETOFFICER));
            if (order != null)
            {
                UpdatePatrolStatus();
                if (patrolStatus.Count > 0)
                {
                    if (!patrolStatus.Exists(c => c.lastMessage < order.timeCreation))
                    {
                        hardMemory.RemoveAll(c => c.toDo);
                        if (order.type == IAInformation.InformationType.DEVIATETOZONE)
                        {
                            Zone zoneToSearch = GetValidZoneToSearch().FindLast(c => true);
                            TellInformationToOthers(IAInformation.InformationType.DEVIATETOZONE, 1f, GetClosestUnitName(zoneToSearch) + "$" + zoneToSearch, true);
                            currentState = IAState.WORKING;
                        }
                        else if(order.type == IAInformation.InformationType.BRINGTOOFFICER)
                        {
                            Zone zoneToTake = ZoneManager.instance.allZones.Find(c => c.zoneEntries.Count == 1 && c != defaultZone);
                            TellInformationToOthers(IAInformation.InformationType.BRINGTOOFFICER, 4f, GetClosestUnitName(zoneToTake) + "$" + zoneToTake.zoneName + "$" + Random.Range(0, 2).ToString(), true);
                            currentState = IAState.WORKING;
                            attendingInfo = informationToCommunicate.FindLast(c => true);
                            timeAttending = Time.time;
                        }
                        else
                        {
                            TellInformationToOthers(IAInformation.InformationType.MEETOFFICER, 4f, order.parameters, true);
                            currentState = IAState.WORKING;
                            attendingInfo = informationToCommunicate.FindLast(c => true);
                            timeAttending = Time.time;
                        }
                    }
                    else
                    {
                        ConfirmDisappearedUnit(order.timeCreation);
                    }
                }
            }
            else
            {
                if (orderWaitingConfirmation != null)
                {
                    UpdatePatrolStatus();
                    string[] orderParameterSplit = orderWaitingConfirmation.parameters.Split('$');
                    PatrolStatus pat = patrolStatus.Find(c => c.unitID == orderParameterSplit[0]);
                    if (pat != null && pat.lastMessage > orderWaitingConfirmation.timeCreation)
                    {
                        TellInformationToOthers(orderWaitingConfirmation.type, 4f, orderWaitingConfirmation.parameters, true);
                    }
                    else
                    {
                        ConfirmDisappearedUnit(orderWaitingConfirmation.timeCreation);
                    }
                }
                else if(attendingInfo != null)
                {
                    if(Time.time - timeAttending > (attendingInfo.type == IAInformation.InformationType.MEETOFFICER ? 60f : 120f))
                    {
                        UpdatePatrolStatus();
                        TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.MEETOFFICER, 0f, attendingInfo.parameters, true));
                    }
                    else
                    {
                        currentState = IAState.WORKING;
                    }
                }else
                {
                    float rangeIdle = Random.Range(0f, 100f);
                    if (rangeIdle < 25f)
                    {
                        List<string> allPatrol = UnitManager.instance.GetAllUnits().FindAll(c => c.Contains("PATROL"));
                        string patrolSelected = allPatrol[Random.Range(0, allPatrol.Count)];
                        TellInformationToOthers(IAInformation.InformationType.MEETOFFICER, 4f, patrolSelected, true);
                        currentState = IAState.WORKING;
                        attendingInfo = informationToCommunicate.FindLast(c => true);
                        timeAttending = Time.time;
                    }
                    else if (rangeIdle >= 25f && rangeIdle < 50f)
                    {
                        UpdatePatrolStatus();
                        TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.BRINGTOOFFICER, 0f, "", true));
                        attendingInfo = informationToCommunicate.FindLast(c => true);
                        timeAttending = Time.time;
                    }
                    else if (rangeIdle >= 50f && rangeIdle < 75f)
                    {
                        UpdatePatrolStatus();
                        TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.DEVIATETOZONE, 0f, "", true));
                    }
                    else
                    {
                        currentState = IAState.WORKING;
                    }
                }
            }

            HaveMadeDecision();
        }
    }

    int checkerCount;
    void WorkingStatePatrolUpdate()
    {
        if (zoneTarget != null && (zoneTarget.IsInsideZone(transform.position) || legs.IsDestinationReached()))
        {
            checkerCount = pendingCheckers.Count;
            eyes.ProcessCheckers(ref pendingCheckers);
            
            if (checkerCount != pendingCheckers.Count || !legs.IsDestinationReached())
            {
                HaveMadeDecision();
            }

            if(isTimeToMakeDecision(IAState.WORKING))
            {
                if(checkerCount > 0)
                {
                    legs.SetDestination(GetBestChecker() ?? pendingCheckers[0], IALegs.Speed.WALK, true, eyes.spotDistance, 0f);
                }
                else
                {
                    TellInformationToOthers(IAInformation.InformationType.ZONECLEAR, 3f, zoneTarget.zoneName);
                    zoneTarget = null;
                    currentState = IAState.IDLE;
                }
                HaveMadeDecision();
            }
        }
    }

    void WorkingStateOfficerUpdate()
    {
        if (!defaultZone.IsInsideZone(transform.position))
        {
            if (legs.IsDestinationReached())
            {
                legs.SetDestination(defaultZone.transform, IALegs.Speed.WALK);
            }
        }
        else if (isTimeToMakeDecision(IAState.WORKING))
        {
            if(Random.Range(0f, 100f) < 30f)
            {
                currentState = IAState.IDLE;
            }else if (Random.Range(0f, 100f) < 50f)
            {
                legs.SetDestination(defaultZone.zoneChecker[Random.Range(0, defaultZone.zoneChecker.Count)], IALegs.Speed.WALK);
            }
            HaveMadeDecision();
        }
    }

    int talkingCount = 0;
    Collider[] meetingColliders;
    void TalkingStateUpdate()
    {
        if (!isTimeToMakeDecision(IAState.TALKING)) return;
        if(meetingTarget != null)
        {
            if(meetingTarget.IsInsideZone(transform.position))
            {
                meetingTarget = null;

                if(talkingTarget == null)
                {
                    //Not here ?
                    //Prudence !
                }

            }else if(legs.IsDestinationReached())
            {
                legs.SetDestination(meetingTarget.transform, IALegs.Speed.WALK);
            }
        }
        else if(talkingTarget != null)
        {
            if (Vector3.Distance(talkingTarget.position, transform.position) > 1.5f && legs.IsDestinationReached())
            {
                legs.SetDestination(talkingTarget, IALegs.Speed.WALK, false, 0f, 1f);
            }

            if (Vector3.Distance(talkingTarget.position, transform.position) < mouth.voiceRange &&
                eyes.CanBeSeen(talkingTarget, mouth.voiceRange, UnitManager.instance.friendLayer)
                && Vector3.Dot(talkingTarget.forward, transform.forward) > -0.9f)
            {
                mouth.SpeakOut(IAEars.NoiseType.INTERPEL);
            }else if(legs.IsDestinationReached())
            {
                if (Random.Range((int)(5*talkative/100f) - talkingCount, 5 + (int)(30 * talkative / 100f) - talkingCount) < 0)
                {
                    mouth.SpeakOut(IAEars.NoiseType.BYE);
                }
                else
                {
                    mouth.SpeakOut(IAEars.NoiseType.FRIENDLY);
                }

                talkingCount++;
            }
        }

        HaveMadeDecision();
    }

    IABrain interpelBrain;
    int checkCount = 0;
    Vector3 checkTempPosition;
    void CheckingStateUpdate()
    {
        if (!isTimeToMakeDecision(IAState.CHECKING)) return;
        if (legs.IsDestinationReached())
        {
            if(Vector3.Dot(transform.position - checkTarget.position, checkTarget.forward) > 0.9f)
            {
                interpelBrain = checkTarget.GetComponent<IABrain>();
                if(interpelBrain != null && interpelBrain.currentState == IAState.TALKING)
                {
                    if(Vector3.Distance(checkTarget.position, transform.position) < 2f)
                    {
                        talkingTarget = checkTarget;
                        currentState = IAState.TALKING;
                    }
                }
                else if(!eyes.CanBeSeen(checkTarget, 1f, -1))
                {
                    legs.SetDestination(checkTarget, IALegs.Speed.RUN, true, 1f);
                }
                else
                {
                    if(checkCount < 4)
                    {
                        checkTempPosition = transform.position + Random.onUnitSphere;
                        checkTempPosition.y = transform.position.y;
                        checkTarget.position = checkTempPosition;

                        legs.LookAtTarget(checkTarget);

                        checkCount++;
                    }
                    else
                    {
                        TellInformationToOthers(IAInformation.InformationType.CHECKINGOVER, 1f, "");
                        currentState = IAState.IDLE;
                    }
                }
            }
            else
            {
                legs.LookAtTarget(checkTarget);
            }
        }

        HaveMadeDecision();
    }
    #endregion

    #region Memory
    public void RegisterMemory(IAInformation information, bool directToBrain = false)
    {
        information.timeReceived = Time.time;
        if (directToBrain) information.completion = 1f;
        if (information.completion >= 1f || Random.Range(0f, 1f) <= information.completion)
        {
            if(information.IsRememberNeeded() || directToBrain)
            {
                ReplaceInformation(information);
                hardMemory.Insert(0, information);
            }
            if(!directToBrain) ProcessInformation(information);
        }

        if (hardMemory.Count > maxMemory)
        {
            hardMemory.Remove(hardMemory.FindLast(c => true));
        }
    }

    public void ReplaceInformation(IAInformation information)
    {
        switch (information.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                hardMemory.RemoveAll(c => c.type == IAInformation.InformationType.SEARCHZONE && c.parameters == information.parameters);
                break;
            case IAInformation.InformationType.ZONECLEAR:
                hardMemory.RemoveAll(c => (c.type == IAInformation.InformationType.SEARCHZONE || c.type == IAInformation.InformationType.ZONECLEAR) && c.parameters == information.parameters);
                break;
        }
    }
    
    public void ProcessInformation(IAInformation information)
    {
        switch (information.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                if (zoneTarget != null)
                {
                    if (zoneTarget.zoneName == information.parameters)
                    {
                        if (zoneTarget.IsInsideZone(transform.position))
                        {
                            TellInformationToOthers(IAInformation.InformationType.ALREADYZONE, 2f, information.parameters, true);
                        }
                        else
                        {
                            CancelZoneTarget();
                        }
                    }
                    else if (Random.Range(0f, 100f) <= friendly
                    && !AccessSoftMemory().Exists(c => c.toDo)
                    && zoneTarget.zoneEntries.Exists(c => c.zoneConnected.Exists(d => d.zoneName == information.parameters))
                    && zoneTarget.zoneEntries.Count > 1)
                    {
                        informationToCommunicate.RemoveAll(c => c.type == IAInformation.InformationType.SEARCHZONE);
                        TellInformationToOthers(IAInformation.InformationType.REPLACEZONE, 3f, information.parameters, true);
                        RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters, true), true);
                    }
                }
                break;

            case IAInformation.InformationType.ZONECLEAR:
                if (behavior == IABehaviour.OFFICER) break;
                if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
                {
                    CancelZoneTarget();
                }
                ForgetThisZoneTarget(information.parameters);
                break;

            case IAInformation.InformationType.REPLACEZONE:
                if (behavior == IABehaviour.OFFICER) break;
                if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
                {
                    CancelZoneTarget();
                    TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
                }
                else if (zoneTarget == null && informationToCommunicate.Exists(c => c.type == IAInformation.InformationType.ZONECLEAR && c.parameters == information.parameters))
                {
                    TellInformationToOthers(IAInformation.InformationType.NOK, 0.5f, information.id.ToString());
                }
                else
                {
                    RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
                }
                ForgetThisZoneTarget(information.parameters);
                break;

            case IAInformation.InformationType.ALREADYZONE:
                if (behavior == IABehaviour.OFFICER) break;
                if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
                {
                    CancelZoneTarget();
                    TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                }
                ForgetThisZoneTarget(information.parameters);
                RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
                break;

            case IAInformation.InformationType.DEVIATETOZONE:
                if (behavior == IABehaviour.OFFICER) break;
                string[] parametersSplit = information.parameters.Split('$');
                if (parametersSplit[0] == unitID || parametersSplit[0] == "all")
                {
                    SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == parametersSplit[1]));
                    TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.RUN);
                }
                break;
            case IAInformation.InformationType.MEETOFFICER:
                if(unitID == information.parameters)
                {
                    zoneTarget = null;
                    TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    currentState = IAState.TALKING;
                    meetingTarget = ZoneManager.instance.allZones.Find(c => c.zoneName.Contains("Officer"));
                    talkingTarget = UnitManager.instance.GetCurrentOfficer();
                }
                break;
            case IAInformation.InformationType.BRINGTOOFFICER:
                string[] parametersBrings = information.parameters.Split('$');
                if(unitID == parametersBrings[0])
                {
                    TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    if (parametersBrings[2] == "1")
                    {
                        CancelZoneTarget();
                    }
                    RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.MEETOFFICER, 0f, "", true), true);
                    RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, "", true), true);
                }
                break;
            case IAInformation.InformationType.ASKSTATUS:
                if(information.parameters == unitID || information.parameters == "all")
                {
                    TellInformationToOthers(IAInformation.InformationType.TELLSTATUS, 2f, unitID + "$" + ZoneManager.instance.GetZone(transform.position).zoneName);
                }
                break;
            case IAInformation.InformationType.TELLSTATUS:
                string[] parametersStatus = information.parameters.Split('$');
                if (behavior == IABehaviour.OFFICER)
                {
                    UpdatePatrolStatus(new PatrolStatus(parametersStatus[0], parametersStatus[1], Time.time));
                }
                break;
            case IAInformation.InformationType.OK:
                if (orderWaitingConfirmation != null && orderWaitingConfirmation.id.ToString() == information.parameters) orderWaitingConfirmation = null;
                break;
            case IAInformation.InformationType.NOK:
                if (orderWaitingConfirmation != null && orderWaitingConfirmation.id.ToString() == information.parameters) orderWaitingConfirmation = null;
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
    IAInformation infoToTell;
    public void TellInformationToOthers(IAInformation.InformationType type, float length, string parameters, bool order = false)
    {
        infoToTell = new IAInformation(unitID, type, length, parameters, order);
        informationToCommunicate.Add(infoToTell);
        if(order)
        {
            if(infoToTell.NeedConfirmation()) orderWaitingConfirmation = infoToTell;
        }
        else
        {
            RegisterMemory(infoToTell, true);
        }
    }

    public void RemoveSameInformationToTell(IAInformation info)
    {
        informationToCommunicate.RemoveAll(c => c.CompareTo(info));
    }

    bool HasNothingToSay()
    {
        return informationToCommunicate.Count == 0;
    }

    void ForgetThisZoneTarget(string zoneName)
    {
        informationToCommunicate.RemoveAll(c => c.type == IAInformation.InformationType.REPLACEZONE && c.parameters == zoneName);
        hardMemory.RemoveAll(c => c.toDo && c.type == IAInformation.InformationType.SEARCHZONE && c.parameters == zoneName);
    }

    void CancelZoneTarget()
    {
        zoneTarget = null;
        currentState = IAState.IDLE;
        legs.StopDestination();
    }

    List<Zone> GetValidZoneToSearch()
    {
        List<Zone> validZoneToVisit = new List<Zone>();
        validZoneToVisit.AddRange(ZoneManager.instance.allZones);
        validZoneToVisit.RemoveAll(c => c.zoneEntries.Count <= 1);
        consultableMemory = AccessSoftMemory();

        foreach (IAInformation information in consultableMemory)
        {
            if (information.type == IAInformation.InformationType.ZONECLEAR || information.type == IAInformation.InformationType.SEARCHZONE)
            {
                Zone designatedZone = ZoneManager.instance.allZones.Find(c => c.zoneName == information.parameters);
                if (validZoneToVisit.Contains(designatedZone) && validZoneToVisit.Count >= minZoneToVisit)
                {
                    validZoneToVisit.Remove(designatedZone);
                }
            }
        }

        return validZoneToVisit;
    }

    Zone GetClosestZoneWithErrorRate(List<Zone> validZones, float errorRate)
    {
        float minDistance = Mathf.Infinity;
        Zone closestZone;
        closestZone = ZoneManager.instance.GetZone(transform.position);

        if (closestZone != null && Random.Range(0f, 100f) >= errorRate)
        {
            closestZone = validZones.Find(c => closestZone.zoneEntries.Exists(d => d.zoneConnected.Contains(c)));
            if (closestZone != null) return closestZone;
        }

        while (validZones.Count > 1)
        {
            minDistance = Mathf.Infinity;
            closestZone = null;
            foreach(Zone zone in validZones)
            {
                if((zone.transform.position - transform.position).sqrMagnitude < minDistance)
                {
                    minDistance = (zone.transform.position - transform.position).sqrMagnitude;
                    closestZone = zone;
                }
            }

            if(closestZone != null)
            {
                if (Random.Range(0f, 100f) >= errorRate)
                {
                    return closestZone;
                }
                else
                {
                    validZones.Remove(closestZone);
                }
            }
            else
            {
                break;
            }
        }
        return validZones[0];
    }

    public void SetZoneTarget(Zone zone)
    {
        zoneTarget = zone;
        pendingCheckers.Clear();
        pendingCheckers.AddRange(zone.zoneChecker);
    }

    BestChecker bestChecker;
    public Transform GetBestChecker()
    {
        float angle;
        float distance;

        foreach(BestChecker bc in checkersDivisions)
        {
            bc.maxDistance = Mathf.NegativeInfinity;
            bc.selectedChecker = null;
        }

        float chanceMax = 0f;
        foreach (Transform checker in pendingCheckers)
        {
            angle = Vector3.Angle(transform.forward, checker.position - transform.position);
            distance = (checker.position - transform.position).sqrMagnitude;

            bestChecker = checkersDivisions.Find(c => c.maxAngle >= angle);
            if (bestChecker.maxDistance < distance)
            {
                bestChecker.selectedChecker = checker;
                bestChecker.maxDistance = distance;

                if (chanceMax < bestChecker.chance) chanceMax = bestChecker.chance;
            }
        }
        
        float randomSeed = Random.Range(0f, chanceMax);

        return checkersDivisions.Find(c => c.selectedChecker != null && randomSeed <= c.chance).selectedChecker;
    }

    void UpdatePatrolStatus()
    {
        foreach(IAInformation information in AccessSoftMemory())
        {
            if(information.from != unitID && (!patrolStatus.Exists(c => c.unitID == information.from) || information.timeReceived > patrolStatus.Find(c => c.unitID == information.from).lastMessage))
            {
                UpdatePatrolStatus(new PatrolStatus(information.from, information.type == IAInformation.InformationType.SEARCHZONE || information.type == IAInformation.InformationType.ZONECLEAR ? information.parameters : "", information.timeReceived));
            }
        }

    }

    void UpdatePatrolStatus(PatrolStatus pat)
    {
        patrolStatus.RemoveAll(c => c.unitID == pat.unitID);
        patrolStatus.Add(pat);
    }

    string closestUnit;
    float minimumDistance;
    float distanceCache;
    public string GetClosestUnitName(Zone zoneToSearch)
    {
        closestUnit = "";
        minimumDistance = Mathf.Infinity;
        foreach (PatrolStatus pat in patrolStatus)
        {
            distanceCache = Vector3.Distance(ZoneManager.instance.allZones.Find(c => c.zoneName == pat.currentZone).transform.position, zoneToSearch.transform.position);
            if (distanceCache < minimumDistance)
            {
                minimumDistance = distanceCache;
                closestUnit = pat.unitID;
            }
        }
        return closestUnit;
    }

    public void ConfirmDisappearedUnit(float timeAsk)
    {
        List<IAInformation> orderStatus = hardMemory.FindAll(c => c.toDo && c.type == IAInformation.InformationType.ASKSTATUS);
        if (orderStatus != null && orderStatus.Count > 0)
        {
            if (orderStatus[0].timeCreation > 15f)
            {
                hardMemory.RemoveAll(c => c.toDo);
                if (orderStatus.Count == patrolStatus.Count)
                {
                    //Prudence !
                    behavior = IABehaviour.PATROL;
                }
                else
                {
                    PatrolStatus lostPat = patrolStatus.Find(c => c.unitID == orderStatus[Random.Range(0, orderStatus.Count)].parameters);
                    TellInformationToOthers(IAInformation.InformationType.DEVIATETOZONE, 1f, "all" + "$" + lostPat.currentZone, true);
                }
            }
        }
        else if (patrolStatus.Exists(c => c.lastMessage < timeAsk) && (Time.time - timeAsk > lostUnitTimeout))
        {
            foreach (PatrolStatus pat in patrolStatus)
            {
                if (pat.lastMessage < timeAsk)
                {
                    TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 0.5f, pat.unitID, true);
                    RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.ASKSTATUS, 0f, "", true));
                }
            }
        }
    }

    public void SetCheck(Transform source)
    {
        checkCount = 0;
        legs.StopDestination();
        currentState = IAState.CHECKING;
        checkTarget = source;
    }

    public void StopTalking()
    {
        currentState = IAState.IDLE;
        talkingCount = 0;
        talkingTarget = null;
        attendingInfo = null;
    }
    #endregion
}
