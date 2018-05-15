using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAHears : MonoBehaviour {

    public IABrain brain;
    public IARadio radio;
    bool listenMessage;
    public bool IsFocus = true;

    private void Start()
    {
        radio.OnMessageReceptionBegin += OnMessageReceptionBegin;
        radio.OnMessageReceptionEnd += OnMessageReceptionEnd;
    }

    private void OnDestroy()
    {
        radio.OnMessageReceptionBegin -= OnMessageReceptionBegin;
        radio.OnMessageReceptionEnd -= OnMessageReceptionEnd;
    }

    public void HeardNoise()
    {
        //Heard strange noise
        //Lose focus on the message
    }

    void OnMessageReceptionBegin(IAInformation information)
    {
        listenMessage = true;
    }

    void OnMessageReceptionEnd(IAInformation information)
    {
        listenMessage = false;
        information.timeReceived = Time.time;
        brain.RegisterMemory(information);
    }

    public bool IsListening()
    {
        return listenMessage;
    }
}
