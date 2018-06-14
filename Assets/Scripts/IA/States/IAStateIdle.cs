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

    protected override void OnEnableState(IAStateTag previousState)
    {
        //If not coming from working or checking, the attendingInfo (unit that needs to respond to the officer) resets
        if(previousState != IAStateTag.WORKING && previousState != IAStateTag.CHECKING)
        {
            attendingInfo = null;
        }

        //If the officer is back from a state that is not working, let's go to work before idle
        if (brain.behavior == IABrain.IABehaviour.OFFICER && previousState != IAStateTag.WORKING)
        {
            brain.ChangeState(IAStateTag.WORKING);
        }
    }

    protected override void PeriodicStateUpdate()
    {
        switch (brain.behavior)
        {
            case IABrain.IABehaviour.PATROL:
                PatrolUpdate();
                break;
            case IABrain.IABehaviour.OFFICER:
                OfficerUpdate();
                break;
        }
    }

    //For normal patrol
    void PatrolUpdate()
    {
        //If there's nothing to say and not saying anything
        if (brain.mouth.HasNothingToSay() && !brain.mouth.IsTalkingToRadio())
        {
            //Find orders
            IAInformation order = brain.memory.AccessSoftMemory().Find(c => c.toDo);
            //Meet officer order
            if (order != null && order.type == IAInformation.InformationType.MEETOFFICER)
            {
                brain.memory.AccessSoftMemory().Remove(order);
                brain.meetingTarget = UnitManager.instance.GetOfficerZone();
                brain.talkingTarget = UnitManager.instance.GetCurrentOfficer();
                brain.legs.SetDestinationToClosest(brain.meetingTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                brain.ChangeState(IAStateTag.TALKING);
            }
            //Search zone order
            else if (order != null && order.type == IAInformation.InformationType.SEARCHZONE)
            {
                brain.memory.AccessSoftMemory().Remove(order);
                brain.SetZoneTarget(ZoneManager.instance.allZones.Find(c => c.zoneName == order.parameters));
                brain.legs.SetDestinationToClosest(brain.zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                brain.ChangeState(IAStateTag.WORKING);
            }
            else
            {
                //Get a new zone to go and go
                if (order != null) brain.memory.AccessSoftMemory().Remove(order);
                brain.SetZoneTarget();
                if (Random.Range(0f, 100f) < brain.talkative)
                {
                    brain.mouth.TellInformationToOthers(IAInformation.InformationType.SEARCHZONE, 2f, brain.zoneTarget.zoneName);
                }
                brain.legs.SetDestinationToClosest(brain.zoneTarget.GetAllEntriesTransform(), IALegs.Speed.WALK);
                brain.ChangeState(IAStateTag.WORKING);
            }
        }
    }

    IAInformation attendingInfo = null;
    float timeAttending;
    public float lostUnitTimeout = 30f;
    List<PatrolStatus> patrolStatus = new List<PatrolStatus>();

    //Officer update
    void OfficerUpdate()
    {
        //If there's nothing to say and say nothing and nothing to heard
        if (brain.mouth.HasNothingToSay() && !brain.mouth.IsTalkingToRadio() && !brain.ears.IsListeningRadio())
        {
            //Get all orders of this type
            IAInformation order = brain.memory.GetOrderOfTypes(IAInformation.InformationType.DEVIATETOZONE,
                                                        IAInformation.InformationType.BRINGTOOFFICER,
                                                        IAInformation.InformationType.MEETOFFICER);
            //If there's order it means that the officer ask for status update and waiting to give the order 
            if (order != null)
            {
                //Update the patrol status
                UpdatePatrolStatus();
                if (IsPatrolStatusInitialized())
                {
                    //If there's a patrol that didn't answer since a certain amount of time
                    if (!HasAPatrolSlientSince(order.timeCreation))
                    {
                        //We clean the order to gives and gives the order
                        brain.memory.CleanOrders();
                        if (order.type == IAInformation.InformationType.DEVIATETOZONE)
                        {
                            Zone zoneToSearch = brain.GetValidZoneToSearch().FindLast(c => true);
                            brain.mouth.TellInformationToOthers(IAInformation.InformationType.DEVIATETOZONE, 1f, GetClosestUnitName(zoneToSearch) + "$" + zoneToSearch, true);
                            brain.ChangeState(IAStateTag.WORKING);
                        }
                        else if (order.type == IAInformation.InformationType.BRINGTOOFFICER)
                        {
                            Zone zoneToTake = ZoneManager.instance.allZones.Find(c => c.zoneEntries.Count == 1 && c != brain.defaultZone);
                            brain.mouth.TellInformationToOthers(IAInformation.InformationType.BRINGTOOFFICER, 4f, GetClosestUnitName(zoneToTake) + "$" + zoneToTake.zoneName + "$" + Random.Range(0, 2).ToString(), true);
                            brain.ChangeState(IAStateTag.WORKING);
                            attendingInfo = brain.mouth.GetLastInfoToCommunicate();
                            timeAttending = Time.time;
                        }
                        else
                        {
                            brain.mouth.TellInformationToOthers(IAInformation.InformationType.MEETOFFICER, 4f, order.parameters, true);
                            brain.ChangeState(IAStateTag.WORKING);
                            attendingInfo = brain.mouth.GetLastInfoToCommunicate();
                            timeAttending = Time.time;
                        }
                    }
                    else //If a patrol didn't answer
                    {
                        //Check if a unit has disappear
                        ConfirmDisappearedUnit(order.timeCreation);
                    }
                }
            }
            else //If not order
            {
                //If we are waiting confirmation from the unit who receive the order
                if (brain.GetOrderConfirmation() != null)
                {
                    UpdatePatrolStatus();
                    string[] orderParameterSplit = brain.GetOrderConfirmation().parameters.Split('$');
                    PatrolStatus pat = patrolStatus.Find(c => c.unitID == orderParameterSplit[0]);
                    //If the unit has talk since the order, we relaunch the order
                    if (pat != null && pat.lastMessage > brain.GetOrderConfirmation().timeCreation)
                    {
                        brain.mouth.TellInformationToOthers(brain.GetOrderConfirmation().type, 4f, brain.GetOrderConfirmation().parameters, true);
                    }
                    else //Else we check if the unit has disappear
                    {
                        ConfirmDisappearedUnit(brain.GetOrderConfirmation().timeCreation);
                    }
                }
                //If the order has been given and the unit is on his way
                else if (attendingInfo != null)
                {
                    //Checinkg the time the unit operate
                    if (Time.time - timeAttending > (attendingInfo.type == IAInformation.InformationType.MEETOFFICER ? 60f : 120f))
                    {
                        //Update patrol status
                        UpdatePatrolStatus();
                        //Prepare to re-ask the task
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        brain.memory.RegisterMemory(new IAInformation(brain.unitID, IAInformation.InformationType.MEETOFFICER, 0f, attendingInfo.parameters, true));
                    }
                    else
                    {
                        //It's normal, lets back to work
                        brain.ChangeState(IAStateTag.WORKING);
                    }
                }
                else
                {
                    float rangeIdle = 0f;//Random.Range(0f, 100f);
                    if (rangeIdle < 25f) //We choose to ask a unit to meet the officer
                    {
                        List<string> allPatrol = UnitManager.instance.GetAllUnits().FindAll(c => c.Contains("PATROL"));
                        string patrolSelected = allPatrol[Random.Range(0, allPatrol.Count)];
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.MEETOFFICER, 4f, patrolSelected, true);
                        brain.ChangeState(IAStateTag.WORKING);
                        attendingInfo = brain.mouth.GetLastInfoToCommunicate();
                        timeAttending = Time.time;
                    }
                    else if (rangeIdle >= 25f && rangeIdle < 50f) //We choose to ask status to bring something to the officer
                    {
                        UpdatePatrolStatus();
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        brain.memory.RegisterMemory(new IAInformation(brain.unitID, IAInformation.InformationType.BRINGTOOFFICER, 0f, "", true));
                        attendingInfo = brain.mouth.GetLastInfoToCommunicate();
                        timeAttending = Time.time;
                    }
                    else if (rangeIdle >= 50f && rangeIdle < 75f) //We choose to ask status to check a zone
                    {
                        UpdatePatrolStatus();
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.ASKSTATUS, 1f, "all", true);
                        brain.memory.RegisterMemory(new IAInformation(brain.unitID, IAInformation.InformationType.DEVIATETOZONE, 0f, "", true));
                    }
                    else //Back to work
                    {
                        brain.ChangeState(IAStateTag.WORKING);
                    }
                }
            }
        }
    }

    //Check if the unit has disappear
    public void ConfirmDisappearedUnit(float timeAsk)
    {
        //Check the ask status order
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

    public void UpdatePatrolStatus(string unitID, string zone)
    {
        UpdatePatrolStatus(new PatrolStatus(unitID, zone, Time.time));
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
            if(pat.currentZone != "null")
            {
                distanceCache = Vector3.Distance(ZoneManager.instance.allZones.Find(c => c.zoneName == pat.currentZone).transform.position, zoneToSearch.transform.position);
                if (distanceCache < minimumDistance)
                {
                    minimumDistance = distanceCache;
                    closestUnit = pat.unitID;
                }
            }
        }

        if(string.IsNullOrEmpty(closestUnit))
        {
            return patrolStatus[Random.Range(0, patrolStatus.Count)].unitID;
        }

        return closestUnit;
    }

    
}
