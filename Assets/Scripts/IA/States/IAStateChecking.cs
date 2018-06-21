using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAStateChecking : IAState
{
    public IAStateChecking(IABrain brain, float updateTime) : base(brain, updateTime)
    {
        tag = IAStateTag.CHECKING;
        layer = IAStateLayer.PASSIVE;
    }

    protected override void OnEnableState(IAStateTag previousState)
    {
        Debug.Log("Enter checking");
        checkCount = 0;
        interpelBrain = brain.checkTarget.GetComponent<IABrain>();
        isPositioned = false;
        maxCheckCount = Random.Range(2, 6);
    }

    protected override void ConstantStateUpdate()
    {
        isPositioned = false;
        if (Vector3.Dot(brain.transform.forward, brain.checkTarget.forward) < -0.99f)
        {
            if (interpelBrain != null && interpelBrain.currentState.tag == IAStateTag.TALKING)
            {
                if (Vector3.Distance(brain.checkTarget.position, brain.transform.position) < 2f)
                {
                    brain.talkingTarget = brain.checkTarget;
                    brain.checkTarget = null;
                    brain.ChangeState(IAStateTag.TALKING);
                }
            }
            else if (!brain.eyes.CanBeSeen(brain.checkTarget, 1f, -1))
            {
                brain.legs.SetDestination(brain.checkTarget, IALegs.Speed.RUN, true, 1f);
            }
            else
            {
                isPositioned = true;
            }
        }
        else
        {
            brain.legs.TurnToTarget(brain.checkTarget);
        }
    }

    IABrain interpelBrain;
    int checkCount = 0;
    int maxCheckCount = 4;
    Vector3 checkTempPosition;
    bool isPositioned = false;
    protected override void PeriodicStateUpdate()
    {
        if(isPositioned)
        {
            if (checkCount < 4)
            {
                checkTempPosition = brain.transform.position + Random.onUnitSphere;
                checkTempPosition.y = brain.transform.position.y;
                brain.checkTarget.position = checkTempPosition;

                brain.legs.TurnToTarget(brain.checkTarget);

                checkCount++;
            }
            else
            {
                brain.checkTarget = null;
                brain.mouth.TellInformationToOthers(IAInformation.InformationType.CHECKINGOVER, 1f, "");
                brain.ChangeState(IAStateTag.IDLE);
            }
        }
       
    }
}
