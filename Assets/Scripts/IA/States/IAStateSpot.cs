using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAStateSpot : IAState
{
    public IAStateSpot(IABrain brain, float updateTime) : base(brain, updateTime)
    {
        tag = IAStateTag.SPOT;
        layer = IAStateLayer.ACTIVE;
    }

    protected override void PeriodicStateUpdate()
    {
        throw new System.NotImplementedException();
    }
}
