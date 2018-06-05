using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAStateIdle : IAState
{
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

    public IAStateIdle(IABrain brain, float updateTime) : base(brain, updateTime)
    {
        tag = IAStateTag.IDLE;
        layer = IAStateLayer.PASSIVE;
    }

    protected override void OnEnableState()
    {
        attendingInfo = null;
    }

    protected override void PeriodicStateUpdate()
    {
        switch (brain.behavior)
        {
            case IABrain.IABehaviour.INTERPATROL:
                PatrolUpdate();
                break;
            case IABrain.IABehaviour.OFFICER:
                OfficerUpdate();
                break;
        }
    }

    void PatrolUpdate()
    {
        if (brain.mouth.HasNothingToSay() && !brain.mouth.IsTalkingToRadio())
        {
            IAInformation order = brain.memory.AccessSoftMemory().Find(c => c.toDo);
            if (order != null && order.type == IAInformation.InformationType.MEETOFFICER)
            {
                brain.memory.AccessSoftMemory().Remove(order);
                brain.meetingTarget = ZoneManager.instance.allZones.Find(c => c.zoneName.Contains("Officer"));
                brain.talkingTarget = UnitManager.instance.GetCurrentOfficer();
                brain.legs.SetDestinationToClosest(brain.meetingTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                currentState = IAState.TALKING;
            }
            else if (order != null && order.type == IAInformation.InformationType.SEARCHZONE)
            {
                brain.memory.AccessSoftMemory().Remove(order);
                brain.SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == order.parameters));
                brain.legs.SetDestinationToClosest(brain.zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                currentState = IAState.WORKING;
            }
            else
            {
                if (order != null) brain.memory.AccessSoftMemory().Remove(order);
                brain.SetZoneTarget();
                if (Random.Range(0f, 100f) < brain.talkative)
                {
                    brain.mouth.TellInformationToOthers(IAInformation.InformationType.SEARCHZONE, 2f, brain.zoneTarget.zoneName);
                }
                brain.legs.SetDestinationToClosest(brain.zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                currentState = IAState.WORKING;
            }
        }
    }

    IAInformation attendingInfo = null;
    float timeAttending;


    public float lostUnitTimeout = 30f;
    List<PatrolStatus> patrolStatus = new List<PatrolStatus>();

    void OfficerUpdate()
    {
        if (brain.mouth.HasNothingToSay() && !brain.mouth.IsTalkingToRadio() && !brain.ears.IsListeningRadio())
        {
            IAInformation order = brain.memory.GetOrderOfTypes(IAInformation.InformationType.DEVIATETOZONE,
                                                        IAInformation.InformationType.BRINGTOOFFICER,
                                                        IAInformation.InformationType.MEETOFFICER);
            if (order != null)
            {
                UpdatePatrolStatus();
                if (IsPatrolStatusInitialized())
                {
                    if (!HasAPatrolSlientSince(order.timeCreation))
                    {
                        brain.memory.CleanOrders();
                        if (order.type == IAInformation.InformationType.DEVIATETOZONE)
                        {
                            Zone zoneToSearch = brain.GetValidZoneToSearch().FindLast(c => true);
                            brain.mouth.TellInformationToOthers(IAInformation.InformationType.DEVIATETOZONE, 1f, GetClosestUnitName(zoneToSearch) + "$" + zoneToSearch, true);
                            currentState = IAState.WORKING;
                        }
                        else if (order.type == IAInformation.InformationType.BRINGTOOFFICER)
                        {
                            Zone zoneToTake = ZoneManager.instance.allZones.Find(c => c.zoneEntries.Count == 1 && c != brain.defaultZone);
                            brain.mouth.TellInformationToOthers(IAInformation.InformationType.BRINGTOOFFICER, 4f, GetClosestUnitName(zoneToTake) + "$" + zoneToTake.zoneName + "$" + Random.Range(0, 2).ToString(), true);
                            currentState = IAState.WORKING;
                            attendingInfo = brain.mouth.GetLastInfoToCommunicate();
                            timeAttending = Time.time;
                        }
                        else
                        {
                            brain.mouth.TellInformationToOthers(IAInformation.InformationType.MEETOFFICER, 4f, order.parameters, true);
                            currentState = IAState.WORKING;
                            attendingInfo = brain.mouth.GetLastInfoToCommunicate();
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
                if (brain.GetOrderConfirmation() != null)
                {
                    UpdatePatrolStatus();
                    string[] orderParameterSplit = brain.GetOrderConfirmation().parameters.Split('$');
                    PatrolStatus pat = patrolStatus.Find(c => c.unitID == orderParameterSplit[0]);
                    if (pat != null && pat.lastMessage > brain.GetOrderConfirmation().timeCreation)
                    {
                        brain.mouth.TellInformationToOthers(brain.GetOrderConfirmation().type, 4f, brain.GetOrderConfirmation().parameters, true);
                    }
                    else
                    {
                        ConfirmDisappearedUnit(brain.GetOrderConfirmation().timeCreation);
                    }
                }
                else if (attendingInfo != null)
                {
                    if (Time.time - timeAttending > (attendingInfo.type == IAInformation.InformationType.MEETOFFICER ? 60f : 120f))
                    {
                        UpdatePatrolStatus();
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        brain.memory.RegisterMemory(new IAInformation(brain.unitID, IAInformation.InformationType.MEETOFFICER, 0f, attendingInfo.parameters, true));
                    }
                    else
                    {
                        currentState = IAState.WORKING;
                    }
                }
                else
                {
                    float rangeIdle = Random.Range(0f, 100f);
                    if (rangeIdle < 25f)
                    {
                        List<string> allPatrol = UnitManager.instance.GetAllUnits().FindAll(c => c.Contains("PATROL"));
                        string patrolSelected = allPatrol[Random.Range(0, allPatrol.Count)];
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.MEETOFFICER, 4f, patrolSelected, true);
                        currentState = IAState.WORKING;
                        attendingInfo = mouth.GetLastInfoToCommunicate();
                        timeAttending = Time.time;
                    }
                    else if (rangeIdle >= 25f && rangeIdle < 50f)
                    {
                        UpdatePatrolStatus();
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        brain.memory.RegisterMemory(new IAInformation(brain.unitID, IAInformation.InformationType.BRINGTOOFFICER, 0f, "", true));
                        attendingInfo = brain.mouth.GetLastInfoToCommunicate();
                        timeAttending = Time.time;
                    }
                    else if (rangeIdle >= 50f && rangeIdle < 75f)
                    {
                        UpdatePatrolStatus();
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        brain.memory.RegisterMemory(new IAInformation(.brain.unitID, IAInformation.InformationType.DEVIATETOZONE, 0f, "", true));
                    }
                    else
                    {
                        currentState = IAState.WORKING;
                    }
                }
            }
        }
    }

    public void ConfirmDisappearedUnit(float timeAsk)
    {
        List<IAInformation> orderStatus = brain.memory.GetOrdersOfType(IAInformation.InformationType.ASKSTATUS);
        if (orderStatus != null && orderStatus.Count > 0)
        {
            if (orderStatus[0].timeCreation > 15f)
            {
                brain.memory.CleanOrders();
                if (orderStatus.Count == patrolStatus.Count)
                {
                    //Que faire ???
                }
                else
                {
                    PatrolStatus lostPat = patrolStatus.Find(c => c.unitID == orderStatus[Random.Range(0, orderStatus.Count)].parameters);
                    brain.mouth.TellInformationToOthers(IAInformation.InformationType.DEVIATETOZONE, 1f, "all" + "$" + lostPat.currentZone, true);
                }
            }
        }
        else if (patrolStatus.Exists(c => c.lastMessage < timeAsk) && (Time.time - timeAsk > lostUnitTimeout))
        {
            foreach (PatrolStatus pat in patrolStatus)
            {
                if (pat.lastMessage < timeAsk)
                {
                    brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 0.5f, pat.unitID, true);
                    brain.memory.RegisterMemory(new IAInformation(brain.unitID, IAInformation.InformationType.ASKSTATUS, 0f, "", true));
                }
            }
        }
    }

    void UpdatePatrolStatus()
    {
        foreach (IAInformation information in brain.memory.AccessSoftMemory())
        {
            if (information.from != brain.unitID && (!patrolStatus.Exists(c => c.unitID == information.from) || information.timeReceived > patrolStatus.Find(c => c.unitID == information.from).lastMessage))
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

    bool IsPatrolStatusInitialized()
    {
        return patrolStatus.Count > 0;
    }

    bool HasAPatrolSlientSince(float time)
    {
        return patrolStatus.Exists(c => c.lastMessage < time);
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

    
}
