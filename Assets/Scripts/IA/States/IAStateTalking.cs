﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAStateTalking : IAState
{
    public IAStateTalking(IABrain brain, float updateTime) : base(brain, updateTime)
    {
        tag = IAStateTag.TALKING;
        layer = IAStateLayer.PASSIVE;
    }

    protected override void OnEnableState(IAStateTag previousState)
    {
        talkingCount = 0;
    }

    int talkingCount = 0;
    Collider[] meetingColliders;
    protected override void PeriodicStateUpdate()
    {
        if (brain.meetingTarget != null)
        {
            if (brain.meetingTarget.IsInsideZone(brain.transform.position))
            {
                brain.meetingTarget = null;

                if (brain.talkingTarget == null)
                {
                    //Not here ?
                    //Prudence !
                }

            }
            else if (brain.legs.IsDestinationReached())
            {
                brain.legs.SetDestination(brain.meetingTarget.transform, IALegs.Speed.WALK);
            }
        }
        else if (brain.talkingTarget != null)
        {
            if (Vector3.Distance(brain.talkingTarget.position, brain.transform.position) > 1.5f && brain.legs.IsDestinationReached())
            {
                brain.legs.SetDestination(brain.talkingTarget, IALegs.Speed.WALK, false, 0f, 1f);
            }

            if (Vector3.Distance(brain.talkingTarget.position, brain.transform.position) < brain.mouth.voiceRange &&
                brain.eyes.CanBeSeen(brain.talkingTarget, brain.mouth.voiceRange, UnitManager.instance.friendLayer)
                && Vector3.Dot(brain.talkingTarget.forward, brain.transform.forward) > -0.9f)
            {
                brain.mouth.SpeakOut(IAEars.NoiseType.INTERPEL);
            }
            else if (brain.legs.IsDestinationReached())
            {
                if (Random.Range((int)(5 * brain.talkative / 100f) - talkingCount, 5 + (int)(30 * brain.talkative / 100f) - talkingCount) < 0)
                {
                    brain.mouth.SpeakOut(IAEars.NoiseType.BYE);
                }
                else
                {
                    brain.mouth.SpeakOut(IAEars.NoiseType.FRIENDLY);
                }

                talkingCount++;
            }
        }
    }
}