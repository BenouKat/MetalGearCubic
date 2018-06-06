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
        checkCount = 0;
    }

    IABrain interpelBrain;
    int checkCount = 0;
    Vector3 checkTempPosition;
    protected override void PeriodicStateUpdate()
    {
        if (brain.legs.IsDestinationReached())
        {
            if (Vector3.Dot(brain.transform.position - brain.checkTarget.position, brain.checkTarget.forward) > 0.9f)
            {
                interpelBrain = brain.checkTarget.GetComponent<IABrain>();
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
                    if (checkCount < 4)
                    {
                        checkTempPosition = brain.transform.position + Random.onUnitSphere;
                        checkTempPosition.y = brain.transform.position.y;
                        brain.checkTarget.position = checkTempPosition;

                        brain.legs.LookAtTarget(brain.checkTarget);

                        checkCount++;
                    }
                    else
                    {
                        brain.mouth.TellInformationToOthers(IAInformation.InformationType.CHECKINGOVER, 1f, "");
                        brain.ChangeState(IAStateTag.IDLE);
                    }
                }
            }
            else
            {
                brain.legs.LookAtTarget(brain.checkTarget);
            }
        }
    }
}
