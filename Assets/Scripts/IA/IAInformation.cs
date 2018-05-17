using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAInformation {
    
    public enum InformationType { ZONECLEAR, SEARCHZONE, ORDER }
    public InformationType type;
    public string parameters;
    public float completion;
    public float length = 1f;
    public float timeReceived;

    public IAInformation(InformationType type, float length, string parameters)
    {
        this.type = type;
        this.parameters = parameters;
        this.length = length;
    }
}
