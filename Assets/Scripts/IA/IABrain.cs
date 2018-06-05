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
#endif
    #endregion

    [System.Serializable]
    internal class StateUpdate
    {
        public IAState.IAStateTag state = IAState.IAStateTag.IDLE;
        public float internalStateUpdateTime = 1f;

        public StateUpdate(IAState.IAStateTag state)
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
    
    [Header("Plugs")]
    public IAEyes eyes;
    public IAMouth mouth;
    public IAEars ears;
    public IALegs legs;
    public IAArms arms;
    public IAMemory memory;
    public string unitID;

    [Header("States")]
    [SerializeField]
    List<StateUpdate> stateUpdates;
    List<IAState> availableStates = new List<IAState>();
    public IAState currentState;
    public enum IABehaviour { PATROL, OFFICER, INTERLEADER, INTERPATROL }
    public IABehaviour behavior;


    [Header("Targets")]
    public Zone zoneTarget;
    public Zone defaultZone;
    List<Transform> pendingCheckers;
    public int minZoneToVisit = 3;
    [SerializeField]
    List<BestChecker> checkersDivisions;

    [Header("Talking state")]
    public Zone meetingTarget;
    public Transform talkingTarget;
    
    [Header("Checking state")]
    public Transform checkTarget;

    [Header("Communication")]
    IAInformation orderWaitingConfirmation;

    [Header("Decisions")]

    [Header("Officer info")]
    public float confirmationOrderTimeout = 20f;


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
        pendingCheckers = new List<Transform>();

        if(!checkersDivisions.Exists(c => c.maxAngle >= 180f))
        {
            checkersDivisions.Add(new BestChecker(180f, 100f));
        }
        checkersDivisions.Sort(delegate (BestChecker bc1, BestChecker bc2)
        {
            return bc1.maxAngle.CompareTo(bc2.maxAngle);
        });

        foreach (StateUpdate stateUpdate in stateUpdates)
        {
            availableStates.Add(IAState.CreateNewState(stateUpdate.state, this, stateUpdate.internalStateUpdateTime));
        }

        ChangeState(IAState.IAStateTag.IDLE);
        
	}
	
	// Update is called once per frame
	void Update () {

        if (IsPassiveState() && eyes.HasTargetOnSight())
        {
            mouth.SayToRadio(null);
            ChangeState(IAState.IAStateTag.SPOT);
        }

        if (behavior == IABehaviour.OFFICER && orderWaitingConfirmation != null)
        {
            if(Time.time - orderWaitingConfirmation.timeCreation > confirmationOrderTimeout)
            {
                ChangeState(IAState.IAStateTag.IDLE);
            }
        }

        currentState.StateUpdate();
	}

    #region State
    public void ChangeState(IAState.IAStateTag tag)
    {
        currentState = availableStates.Find(c => c.tag == tag);
        currentState.OnEnable();
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
                    mouth.TellInformationToOthers(IAInformation.InformationType.ZONECLEAR, 3f, zoneTarget.zoneName);
                    zoneTarget = null;
                    ChangeState(IAState.IAStateTag.IDLE);
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
                ChangeState(IAState.IAStateTag.IDLE);
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
                        ChangeState(IAState.IAStateTag.TALKING);
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
                        mouth.TellInformationToOthers(IAInformation.InformationType.CHECKINGOVER, 1f, "");
                        ChangeState(IAState.IAStateTag.IDLE);
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

    #region Communication
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
                            mouth.TellInformationToOthers(IAInformation.InformationType.ALREADYZONE, 2f, information.parameters, true);
                        }
                        else
                        {
                            CancelZoneTarget();
                        }
                    }
                    else if (Random.Range(0f, 100f) <= friendly
                    && !memory.AccessSoftMemory().Exists(c => c.toDo)
                    && zoneTarget.zoneEntries.Exists(c => c.zoneConnected.Exists(d => d.zoneName == information.parameters))
                    && zoneTarget.zoneEntries.Count > 1)
                    {
                        mouth.RemoveInformation(IAInformation.InformationType.SEARCHZONE);
                        mouth.TellInformationToOthers(IAInformation.InformationType.REPLACEZONE, 3f, information.parameters, true);
                        memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters, true), true);
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
                    mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
                }
                else if (zoneTarget == null && mouth.ExistInformation(IAInformation.InformationType.ZONECLEAR, information.parameters))
                {
                    mouth.TellInformationToOthers(IAInformation.InformationType.NOK, 0.5f, information.id.ToString());
                }
                else
                {
                    memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
                }
                ForgetThisZoneTarget(information.parameters);
                break;

            case IAInformation.InformationType.ALREADYZONE:
                if (behavior == IABehaviour.OFFICER) break;
                if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
                {
                    CancelZoneTarget();
                    mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                }
                ForgetThisZoneTarget(information.parameters);
                memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
                break;

            case IAInformation.InformationType.DEVIATETOZONE:
                if (behavior == IABehaviour.OFFICER) break;
                string[] parametersSplit = information.parameters.Split('$');
                if (parametersSplit[0] == unitID || parametersSplit[0] == "all")
                {
                    SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == parametersSplit[1]));
                    mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.RUN);
                }
                break;
            case IAInformation.InformationType.MEETOFFICER:
                if(unitID == information.parameters)
                {
                    zoneTarget = null;
                    mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    currentState = IAState.TALKING;
                    meetingTarget = ZoneManager.instance.allZones.Find(c => c.zoneName.Contains("Officer"));
                    talkingTarget = UnitManager.instance.GetCurrentOfficer();
                }
                break;
            case IAInformation.InformationType.BRINGTOOFFICER:
                string[] parametersBrings = information.parameters.Split('$');
                if(unitID == parametersBrings[0])
                {
                    mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
                    if (parametersBrings[2] == "1")
                    {
                        CancelZoneTarget();
                    }
                    memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.MEETOFFICER, 0f, "", true), true);
                    memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, "", true), true);
                }
                break;
            case IAInformation.InformationType.ASKSTATUS:
                if(information.parameters == unitID || information.parameters == "all")
                {
                    mouth.TellInformationToOthers(IAInformation.InformationType.TELLSTATUS, 2f, unitID + "$" + ZoneManager.instance.GetZone(transform.position).zoneName);
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

    
    #endregion

    #region Decisions
    public void HaveMadeDecision()
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
    #endregion

    #region Utils
    public void SetOrderConfirmation(IAInformation order)
    {
        orderWaitingConfirmation = order;
    }

    public IAInformation GetOrderConfirmation()
    {
        return orderWaitingConfirmation;
    }

    void ForgetThisZoneTarget(string zoneName)
    {
        mouth.RemoveInformation(IAInformation.InformationType.REPLACEZONE, zoneName);
        memory.CleanOrders(IAInformation.InformationType.SEARCHZONE, zoneName);
    }

    void CancelZoneTarget()
    {
        zoneTarget = null;
        ChangeState(IAState.IAStateTag.IDLE);
        legs.StopDestination();
    }

    public List<Zone> GetValidZoneToSearch()
    {
        List<Zone> validZoneToVisit = new List<Zone>();
        validZoneToVisit.AddRange(ZoneManager.instance.allZones);
        validZoneToVisit.RemoveAll(c => c.zoneEntries.Count <= 1);

        foreach (IAInformation information in memory.AccessSoftMemory())
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

    public void SetZoneTarget()
    {
        SetZoneTarget(GetClosestZoneWithErrorRate(GetValidZoneToSearch(), 100f - orientation));
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

    public void SetCheck(Transform source)
    {
        checkCount = 0;
        legs.StopDestination();
        ChangeState(IAState.IAStateTag.CHECKING);
        checkTarget = source;
    }

    public void StopTalking()
    {
        ChangeState(IAState.IAStateTag.IDLE);
        talkingCount = 0;
        talkingTarget = null;
    }
    #endregion
}
