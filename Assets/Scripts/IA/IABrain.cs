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

        if(behavior == IABehaviour.OFFICER)
        {
            UnitManager.instance.SetOfficer(transform);
        }

        ChangeState(IAState.IAStateTag.IDLE);
	}
	
	// Update is called once per frame
	void Update () {

        if (currentState.layer == IAState.IAStateLayer.PASSIVE && eyes.HasTargetOnSight())
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

    void ProcessSearchZone(IAInformation information)
    {
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
    }

    void ProcessZoneClear(IAInformation information)
    {
        if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
        {
            CancelZoneTarget();
        }
        ForgetThisZoneTarget(information.parameters);
    }

    void ProcessReplaceZone(IAInformation information)
    {
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
    }

    void ProcessAlreadyZone(IAInformation information)
    {
        if (zoneTarget != null && information.parameters == zoneTarget.zoneName)
        {
            CancelZoneTarget();
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
        }
        ForgetThisZoneTarget(information.parameters);
        memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, information.parameters), true);

    }

    void ProcessDeviateToZone(IAInformation information)
    {
        string[] parametersSplit = information.parameters.Split('$');
        if (parametersSplit[0] == unitID || parametersSplit[0] == "all")
        {
            SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == parametersSplit[1]));
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            legs.SetDestinationToClosest(zoneTarget.GetAllEntriesTransform(), IALegs.Speed.RUN);
        }
    }

    void ProcessMeetOfficer(IAInformation information)
    {
        if (unitID == information.parameters)
        {
            zoneTarget = null;
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            ChangeState(IAState.IAStateTag.TALKING);
            meetingTarget = ZoneManager.instance.allZones.Find(c => c.zoneName.Contains("Officer"));
            talkingTarget = UnitManager.instance.GetCurrentOfficer();
        }
    }

    void ProcessBringToOfficer(IAInformation information)
    {
        string[] parametersBrings = information.parameters.Split('$');
        if (unitID == parametersBrings[0])
        {
            mouth.TellInformationToOthers(IAInformation.InformationType.OK, 0.5f, information.id.ToString());
            if (parametersBrings[2] == "1")
            {
                CancelZoneTarget();
            }
            memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.MEETOFFICER, 0f, "", true), true);
            memory.RegisterMemory(new IAInformation(unitID, IAInformation.InformationType.SEARCHZONE, 0f, "", true), true);
        }
    }

    void ProcessAskStatus(IAInformation information)
    {
        if (information.parameters == unitID || information.parameters == "all")
        {
            mouth.TellInformationToOthers(IAInformation.InformationType.TELLSTATUS, 2f, unitID + "$" + ZoneManager.instance.GetZone(transform.position).zoneName);
        }
    }

    void ProcessTellStatus(IAInformation information)
    {
        string[] parametersStatus = information.parameters.Split('$');
        ((IAStateIdle)GetState(IAState.IAStateTag.IDLE)).UpdatePatrolStatus(parametersStatus[0], parametersStatus[1]);
    }

    void ProcessAgreement(IAInformation information)
    {
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

    public List<Transform> GetPendingCheckers()
    {
        return pendingCheckers;
    }

    public void ProcessCheckers()
    {
        eyes.ProcessCheckers(ref pendingCheckers);
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
        legs.StopDestination();
        ChangeState(IAState.IAStateTag.CHECKING);
        checkTarget = source;
    }

    public void StopTalking()
    {
        legs.StopTurnToTarget();
        talkingTarget = null;
        ChangeState(IAState.IAStateTag.IDLE);
    }
    #endregion
}
