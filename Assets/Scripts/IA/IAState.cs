using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class IAState {

    protected IABrain brain;
    float lastUpdate;
    float updateTime;
    public enum IAStateTag { WORKING, TALKING, IDLE, CHECKING, SPOT, FREEZE, ALERT, DANGER, PRUDENCE }
    public enum IAStateLayer { PASSIVE, ACTIVE }
    public IAStateTag tag;
    public IAStateLayer layer;

    public IAState(IABrain brain, float updateTime)
    {
        this.brain = brain;
        this.updateTime = updateTime;
    }

    //When the state change
    public void OnEnable(IAStateTag previousState)
    {
        OnEnableState(previousState);
        TurnReady();
    }
    
    public void OnDisable(IAStateTag nextState)
    {
	    OnDisableState(nextState);
    } 
    
    //On Update
    public void StateUpdate()
    {
        ConstantStateUpdate();
        if (Time.time - lastUpdate >= updateTime)
        {
            PeriodicStateUpdate();
            ResetUpdateTime();
        }
    }

    //Wait for another turn
    public void ResetUpdateTime()
    {
        lastUpdate = Time.time;
    }

    //Invoke next update directly next frame
    public void TurnReady()
    {
        lastUpdate = Time.time - updateTime;
    }
    
    protected abstract void OnEnableState(IAStateTag previousState) { }
    protected abstract void ConstantStateUpdate() { } //On Update every frame
    protected abstract void PeriodicStateUpdate(); //On Update every update time
    protected abstract void OnDisableState(IAStateTag nextState);

    //Create a new state from the tag (gives the right child class)
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

