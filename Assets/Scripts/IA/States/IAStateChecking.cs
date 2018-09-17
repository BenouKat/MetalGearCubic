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

    float timeStartChecking;

    protected override void OnEnableState(IAStateTag previousState)
    {
        Debug.Log("Enter checking");
        checkCount = 0;
        interpelBrain = brain.checkTarget.GetComponent<IABrain>();
        isPositioned = false;
        maxCheckCount = Random.Range(2, 6);
        timeStartChecking = Time.time;
    }

    protected override void ConstantStateUpdate()
    {
        isPositioned = false;
        if(interpelBrain != null)
        {
            if(interpelBrain.talkingTarget != brain.transform)
            {
                brain.ChangeState(IAStateTag.IDLE);
                return;
            }

            if (Vector3.Dot(brain.transform.forward, brain.checkTarget.forward) < -0.99f)
            {
                if (interpelBrain.currentState.tag == IAStateTag.TALKING)
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
        else
        {
            if(Time.time - timeStartChecking < 1f)
            {
                brain.legs.TurnToTarget(brain.checkTarget);
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
        

        if (brain.zoneTarget != null && (brain.zoneTarget.IsInsideZone(brain.transform.position) || brain.legs.IsDestinationReached()))
        {
            brain.ProcessCheckers();
        }
    }

    IABrain interpelBrain;
    int checkCount = 0;
    int maxCheckCount = 4;
    Vector3 checkTempPosition;
    Transform transformHelper;
    bool isPositioned = false;
    protected override void PeriodicStateUpdate()
    {
        if(isPositioned)
        {
            if (checkCount < maxCheckCount)
            {
                if (transformHelper == null) transformHelper = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "CheckingStateHelper").transform;
                checkTempPosition = brain.transform.position + Random.onUnitSphere;
                checkTempPosition.y = brain.transform.position.y;
                transformHelper.position = checkTempPosition;

                brain.legs.TurnToTarget(transformHelper);
                checkCount++;

                Debug.Log("Check count : " + checkCount);
            }
            else
            {
                if(brain.zoneTarget != null && brain.GetPendingCheckers().Count > 0)
                {
                    checkCount = 0;
                    maxCheckCount =  Random.Range(2, 6);
                    brain.checkTarget = brain.GetBestChecker() ?? brain.GetPendingCheckers()[0];

                    Debug.Log("Check new checker");
                }
                else
                {
                    brain.zoneTarget = null;
                    brain.checkTarget = null;
                    brain.mouth.TellInformationToOthers(IAInformation.InformationType.CHECKINGOVER, 1f, "");
                    brain.ChangeState(IAStateTag.IDLE);
                }
                
            }
        }
       
    }

	protected override void OnDisableState(IAStateTag nextState)
	{
		//Nothing
	}
}
