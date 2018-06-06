using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class IAState {

    protected IABrain brain;
    float lastUpdate;
    public float updateTime;
    public enum IAStateTag { WORKING, TALKING, IDLE, CHECKING, SPOT, FREEZE, ALERT, DANGER, PRUDENCE }
    public enum IAStateLayer { PASSIVE, ACTIVE }
    public IAStateTag tag;
    public IAStateLayer layer;

    public IAState(IABrain brain, float updateTime)
    {
        this.brain = brain;
        this.updateTime = updateTime;
    }

    public void OnEnable(IAStateTag previousState)
    {
        OnEnableState(previousState);
    }
    
    public void StateUpdate()
    {
        ConstantStateUpdate();
        if (Time.time - lastUpdate > updateTime)
        {
            PeriodicStateUpdate();
            ResetUpdateTime();
        }
    }

    public void ResetUpdateTime()
    {
        lastUpdate = Time.time;
    }
    
    protected virtual void OnEnableState(IAStateTag previousState) { }
    protected virtual void ConstantStateUpdate() { }
    protected abstract void PeriodicStateUpdate();

    public static IAState CreateNewState(IAStateTag tag, IABrain brain, float updateTime)
    {
        switch(tag)
        {
            case IAStateTag.IDLE:
                return new IAStateIdle(brain, updateTime);
            case IAStateTag.WORKING:
                return new IAStateWorking(brain, updateTime);
            case IAStateTag.TALKING:
                return new IAStateTalking(brain, updateTime);
            case IAStateTag.CHECKING:
                return new IAStateChecking(brain, updateTime);
            case IAStateTag.SPOT:
                return new IAStateSpot(brain, updateTime);
        }
        return null;
    }
}

