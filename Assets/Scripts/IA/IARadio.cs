using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IARadio : MonoBehaviour {

    public IARadioManager.Channels channel;
    bool receivingMessage;
    IAInformation informationReceived;

    private void Start()
    {
        SwitchRadioOn();
    }

    public void SwitchRadioOn()
    {
        IARadioManager.instance.ConnectRadio(this);
    }

    public void SwitchRadioOff()
    {
        IARadioManager.instance.DisconnectRadio(this);
    }

    public void Talk(IAInformation information, float length)
    {
        IARadioManager.instance.BeginCommunication(this, information, channel, length);
    }

    public void InterruptTalk()
    {
        IARadioManager.instance.EndCommunication(channel);
    }

    public void BeginMessageReception(IAInformation information)
    {
        informationReceived = information;
        receivingMessage = true;
    }

    public void ProcessMessageReception(float completion)
    {
        informationReceived.informationCompletion = completion;
    }

    public void EndMessageReception()
    {
        receivingMessage = false;
    }

    public bool isReceivingMessage()
    {
        return receivingMessage;
    }
}
