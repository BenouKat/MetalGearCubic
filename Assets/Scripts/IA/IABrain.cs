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
        if (currentState == null) return;
        Handles.Label(transform.position + (Vector3.up * 1.5f), currentState.tag.ToString());

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
    public List<IAInformation.InformationType> informationToListen;


    [Header("Targets")]
    public Zone zoneTarget;
    public Zone defaultZone;
    public Zone meetingTarget;
    public Transform talkingTarget;
    public Transform checkTarget;

    [Header("Checkers")]
    List<Transform> pendingCheckers;
    public int minZoneToVisit = 3;
    [SerializeField]
    List<BestChecker> checkersDivisions;

    [Header("Communication")]
    IAInformation orderWaitingConfirmation;
    
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
        //Set some initial parameters
        unitID = UnitManager.instance.GetNewUnitID(behavior.ToString());
        pendingCheckers = new List<Transform>();

        //Set checkers division to the Working IA checkers
        if(!checkersDivisions.Exists(c => c.maxAngle >= 180f))
        {
            checkersDivisions.Add(new BestChecker(180f, 100f));
        }
        checkersDivisions.Sort(delegate (BestChecker bc1, BestChecker bc2)
        {
            return bc1.maxAngle.CompareTo(bc2.maxAngle);
        });

        //Set state update
        foreach (StateUpdate stateUpdate in stateUpdates)
        {
            availableStates.Add(IAState.CreateNewState(stateUpdate.state, this, stateUpdate.internalStateUpdateTime));
        }

        //If officer, notice to the unit manager
        if(behavior == IABehaviour.OFFICER)
        {
            //If there's no officer zone, set it
            if(UnitManager.instance.GetOfficerZone() == null)
            {
                UnitManager.instance.SetOfficerZone(ZoneManager.instance.GetZone(transform.position));
            }
            UnitManager.instance.SetOfficer(transform);
        }

        //Change state to IDLE
        ChangeState(IAState.IAStateTag.IDLE);
	}
	
	// Update is called once per frame
	void Update () {

        //Spot enemy (to remove ?)
        if (currentState.layer == IAState.IAStateLayer.PASSIVE && eyes.HasTargetOnSight())
        {
            mouth.SayToRadio(null);
            ChangeState(IAState.IAStateTag.SPOT);
        }

        //If officer is waiting for confirmation and the confirmation didn't come, we re-do an idle state
        if (behavior == IABehaviour.OFFICER && orderWaitingConfirmation != null)
        {
            if(Time.time - orderWaitingConfirmation.timeCreation > confirmationOrderTimeout)
            {
                ChangeState(IAState.IAStateTag.IDLE);
            }
        }

        //State update
        currentState.StateUpdate();
	}

    #region State
    public void ChangeState(IAState.IAStateTag tag)
    {
        IAState.IAStateTag previousState = currentState != null ? currentState.tag : IAState.IAStateTag.IDLE;
        currentState = availableStates.Find(c => c.tag == tag);
        currentState.OnEnable(previousState);
    }

    public IAState GetState(IAState.IAStateTag tag)
    {
        return availableStates.Find(c => c.tag == tag);
    }
    #endregion

    #region Communication
    //Process information into the brain to react
    public void ProcessInformation(IAInformation information)
    {
        if (!informationToListen.Contains(information.type)) return;

        switch (information.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                ProcessSearchZone(information);
                break;
            case IAInformation.InformationType.ZONECLEAR:
                ProcessZoneClear(information);
                break;
            case IAInformation.InformationType.REPLACEZONE:
                ProcessReplaceZone(information);
                break;
            case IAInformation.InformationType.ALREADYZONE:
                ProcessAlreadyZone(information);
                break;
            case IAInformation.InformationType.DEVIATETOZONE:
                ProcessDeviateToZone(information);
                break;
            case IAInformation.InformationType.MEETOFFICER:
                ProcessMeetOfficer(information);
                break;
            case IAInformation.InformationType.BRINGTOOFFICER:
                ProcessBringToOfficer(information);
                break;
            case IAInformation.InformationType.ASKSTATUS:
                ProcessAskStatus(information);
                break;
            case IAInformation.InformationType.TELLSTATUS:
                ProcessTellStatus(information);
                break;
            case IAInformation.InformationType.OK:
                ProcessAgreement(information);
                break;
            case IAInformation.InformationType.NOK:
                ProcessAgreement(information);
                break;
        }
    }

    //Unit heard that someone is searching a zone
    void ProcessSearchZone(IAInformation information)
    {
        if (zoneTarget != null)
        {
            //If it's the same zone that this unit
            if (zoneTarget.zoneName == information.parameters)
            {
                //And he's inside
                if (zoneTarget.IsInsideZone(transform.position))
                {
                    //Say that he's already here
                    mouth.TellInformationToOthers(IAInformation.InformationType.ALREADYZONE, 2f, information.parameters, true);
                }
                else //Else it's not going into it
                {
                    CancelZoneTarget();
                }
            } //if the zone is close and the unit is friendly, we wants to go instead
            else if (Random.Range(0f, 100f) <= friendly
            && !memory.AccessSoftMemory().Exists(c => c.toDo)
            && zoneTarget.zoneEntries.Exists(c => c.zoneConnected.Exists(d => d.zoneName == information.parameters))
            && zoneTarget.zoneEntries.Count > 1)
            {
                //Telling
                mouth.RemoveInformation(IAInformation.InformationType.SEARCHZONE);
                mouth.TellInformationToOthers(IAInformation.InformationType.REPLACEZONE, 3f, information.parameters, true);
                memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters, true), true);
            }
        }
    }

    //Unit heard than a zone is cleared
    void ProcessZoneClear(IAInformation information)
    {
        //If it's my zone
        if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
        {
            //Nope
            CancelZoneTarget();
        }
        ForgetThisZoneTarget(information.parameters);
    }

    //Unit heard that someone is replacing someone else in a zone
    void ProcessReplaceZone(IAInformation information)
    {
        //It's my zone ! Im' replaced
        if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
        {
            //Replacement, remember that a unit is searcing the zone
            CancelZoneTarget();
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
        }
        //Already clared
        else if (zoneTarget == null && mouth.ExistInformation(IAInformation.InformationType.ZONECLEAR, information.parameters))
        {
            mouth.TellInformationToOthers(IAInformation.InformationType.NOK, 0.5f, information.id.ToString());
        }
        else //If it doesn't concerned me, I just record the info
        {
            memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);
        }
        ForgetThisZoneTarget(information.parameters);
    }

    //Unit heard that somone is already in this zone
    void ProcessAlreadyZone(IAInformation information)
    {
        //Oops, it's me
        if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
        {
            //I cancel this objective
            CancelZoneTarget();
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
        }
        ForgetThisZoneTarget(information.parameters);
        memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);

    }

    //Unit have order to search a specific zone
    void ProcessDeviateToZone(IAInformation information)
    {
        //Me or all unit ?
        string[] parametersSplit = information.parameters.Split('$');
        if (parametersSplit[0] == unitID || parametersSplit[0] == "all")
        {
            //Searching the zone, confirm and go in it immediatly
            SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == parametersSplit[1]));
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.RUN);
        }
    }

    //Unit heard a meeting with the officer
    void ProcessMeetOfficer(IAInformation information)
    {
        //It's me ?
        if (unitID == information.parameters)
        {
            //I cancel my zone, confirm and go to talk with the officer at the officer room
            zoneTarget = null;
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            ChangeState(IAState.IAStateTag.TALKING);
            meetingTarget = UnitManager.instance.GetOfficerZone();
            talkingTarget = UnitManager.instance.GetCurrentOfficer();
        }
    }

    //Unit heard bring something to officer
    void ProcessBringToOfficer(IAInformation information)
    {
        //it's me ?
        string[] parametersBrings = information.parameters.Split('$');
        if (unitID == parametersBrings[0])
        {
            //Ok ! If parameter is "1" it means immediatly, so I cancel my zone target
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            if (parametersBrings[2] == "1")
            {
                CancelZoneTarget();
            }
            memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.MEETOFFICER, 0f, "", true), true);
            memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, "", true), true);
        }
    }

    //Unit heard question for status update
    void ProcessAskStatus(IAInformation information)
    {
        //It's me ?
        if (information.parameters == unitID || information.parameters == "all")
        {
            //Give status update
            mouth.TellInformationToOthers(IAInformation.InformationType.TELLSTATUS, 2f, unitID + "$" + ZoneManager.instance.GetZone(transform.position).zoneName);
        }
    }

    //Unit heard status update
    void ProcessTellStatus(IAInformation information)
    {
        //Update the patrol status
        string[] parametersStatus = information.parameters.Split('$');
        ((IAStateIdle)GetState(IAState.IAStateTag.IDLE)).UpdatePatrolStatus(parametersStatus[0], parametersStatus[1]);
    }

    //Unit heard agreement / disagreement
    void ProcessAgreement(IAInformation information)
    {
        //If the order match, no waiting
        if (orderWaitingConfirmation != null && orderWaitingConfirmation.id.ToString() == information.parameters) orderWaitingConfirmation = null;
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

    public float GetUpdateTime(IAState.IAStateTag tag)
    {
        return stateUpdates.Find(c => c.state == tag).internalStateUpdateTime;
    }

    void ForgetThisZoneTarget(string zoneName)
    {
        mouth.RemoveInformation(IAInformation.InformationType.REPLACEZONE, zoneName);
        memory.CleanOrders(IAInformation.InformationType.SEARCHZONE, zoneName);
    }

    //Stop the patrol to check this target and decide to go elsewhere
    void CancelZoneTarget()
    {
        zoneTarget = null;
        ChangeState(IAState.IAStateTag.IDLE);
        legs.CancelDestination();
    }

    //Get a zone to search from memory
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

    //Get the closest zone for a list, with an error rate to not pick the very closest
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

    public List<Transform> GetPendingCheckers()
    {
        return pendingCheckers;
    }

    public void ProcessCheckers()
    {
        eyes.ProcessCheckers(ref pendingCheckers);
    }

    //Get the best checker to go after the last one
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
            //Get the max distance checker for each angle in Best Checker list
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
        
        //Get the best candidate for the chance given (higher is the chance, narrow will be the angle)
        float randomSeed = Random.Range(0f, chanceMax);

        return checkersDivisions.Find(c => c.selectedChecker != null && randomSeed <= c.chance).selectedChecker;
    }

    //Check status
    public void SetCheck(Transform source)
    {
        legs.CancelDestination();
        ChangeState(IAState.IAStateTag.CHECKING);
        checkTarget = source;
    }

    //Stop talking
    public void StopTalking()
    {
        legs.StopTurnToTarget();
        talkingTarget = null;
        ChangeState(IAState.IAStateTag.IDLE);
    }
    #endregion
}
