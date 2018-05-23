using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAInformation {
    
    public enum InformationType { ZONECLEAR, SEARCHZONE, REPLACEZONE, ALREADYZONE, DEVIATEZONE, OK, NOK }
    public InformationType type;
    public string parameters;
    public float completion;
    public float length = 1f;
    public float timeReceived;
    public bool toDo;

    public IAInformation(InformationType type, float length, string parameters, bool toDo = false)
    {
        this.type = type;
        this.parameters = parameters;
        this.length = length;
        this.toDo = toDo;
    }

    public bool IsRememberNeeded()
    {
        return type == InformationType.ZONECLEAR
            || type == InformationType.SEARCHZONE;
    }

    public bool CompareTo(IAInformation info)
    {
        return type == info.type && parameters == info.parameters && toDo == info.toDo;
    }
}
