using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAStateWorking : IAState
{
    public IAStateWorking(IABrain brain, float updateTime) : base(brain, updateTime)
    {
        tag = IAStateTag.WORKING;
        layer = IAStateLayer.PASSIVE;
    }

    protected override void ConstantStateUpdate()
    {
        base.ConstantStateUpdate();

        switch (brain.behavior)
        {
            case IABrain.IABehaviour.INTERPATROL:
                PatrolConstantUpdate();
                break;
            case IABrain.IABehaviour.OFFICER:
                OfficerConstantUpdate();
                break;
        }

        
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

    void PatrolConstantUpdate()
    {
        if (brain.zoneTarget != null && (brain.zoneTarget.IsInsideZone(brain.transform.position) || brain.legs.IsDestinationReached()))
        {
            brain.ProcessCheckers();
        }
    }

    void OfficerConstantUpdate()
    {
        if (!brain.defaultZone.IsInsideZone(brain.transform.position))
        {
            if (brain.legs.IsDestinationReached())
            {
                brain.legs.SetDestination(brain.defaultZone.transform, IALegs.Speed.WALK);
            }
        }
    }

    int checkerCount;
    void PatrolUpdate()
    {
        if(brain.legs.IsDestinationReached())
        {
            if(checkerCount != brain.GetPendingCheckers().Count)
            {
                checkerCount = brain.GetPendingCheckers().Count;
            }
            else
            {
                if (checkerCount > 0)
                {
                    brain.legs.SetDestination(brain.GetBestChecker() ?? brain.GetPendingCheckers()[0], IALegs.Speed.WALK, true, brain.eyes.spotDistance, 0f);
                }
                else
                {
                    brain.mouth.TellInformationToOthers(IAInformation.InformationType.ZONECLEAR, 3f, brain.zoneTarget.zoneName);
                    brain.zoneTarget = null;
                    brain.ChangeState(IAStateTag.IDLE);
                }
            }
        }
    }

    void OfficerUpdate()
    {
        if (brain.defaultZone.IsInsideZone(brain.transform.position))
        {
            if (Random.Range(0f, 100f) < 30f)
            {
                brain.ChangeState(IAStateTag.IDLE);
            }
            else if (Random.Range(0f, 100f) < 50f)
            {
                brain.legs.SetDestination(brain.defaultZone.zoneChecker[Random.Range(0, brain.defaultZone.zoneChecker.Count)], IALegs.Speed.WALK);
            }
        }
    }
}
