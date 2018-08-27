using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAInformation {
    
    public enum InformationType { ZONECLEAR, SEARCHZONE, REPLACEZONE, ALREADYZONE, DEVIATETOZONE, BRINGTOOFFICER, MEETOFFICER, CHECKING, CHECKINGOVER, ASKSTATUS, TELLSTATUS, OK, NOK, ASKHELP }
    public readonly System.Guid id;
    public InformationType type;
    public string from;
    public string parameters;
    public float completion;
    public float length = 1f;
    public float timeCreation;
    public float timeReceived;
    public bool toDo;

    public IAInformation(string from, InformationType type, float length, string parameters, bool toDo = false)
    {
        id = System.Guid.NewGuid();
        this.from = from;
        this.type = type;
        this.parameters = parameters;
        this.length = length;
        this.toDo = toDo;
        timeCreation = Time.time;
    }

    public bool IsRememberNeeded()
    {
        return type == InformationType.ZONECLEAR
            || type == InformationType.SEARCHZONE;
    }


    public bool NeedConfirmation()
    {
        return type == InformationType.BRINGTOOFFICER
            || type == InformationType.MEETOFFICER
            || type == InformationType.DEVIATETOZONE;
    }

    public bool CompareTo(IAInformation info)
    {
        return type == info.type && parameters == info.parameters && toDo == info.toDo;
    }

}
