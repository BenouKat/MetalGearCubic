using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IARadio : MonoBehaviour {

    public IARadioManager.Channels channel;
    bool receivingMessage;
    IAInformation informationReceived;

    public delegate void MessageReceptionHandler(IAInformation information);
    public event MessageReceptionHandler OnMessageReceptionBegin;
    public event MessageReceptionHandler OnMessageReceptionEnd;

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

    public void Talk(IAInformation information)
    {
        IARadioManager.instance.BeginCommunication(this, information, channel);
    }

    public void InterruptTalk()
    {
        IARadioManager.instance.EndCommunication(channel);
    }

    public void BeginMessageReception(IAInformation information)
    {
        informationReceived = information;
        Debug.Log("begin message reception : " + informationReceived.completion);
        receivingMessage = true;
        if(OnMessageReceptionBegin != null) OnMessageReceptionBegin.Invoke(informationReceived);
    }

    public void ProcessMessageReception(float completion)
    {
        informationReceived.completion = completion;
        Debug.Log("process message reception : " + informationReceived.completion);
    }

    public void EndMessageReception()
    {
        receivingMessage = false;
        Debug.Log("end message reception : " + informationReceived.completion);
        if (OnMessageReceptionEnd != null) OnMessageReceptionEnd.Invoke(informationReceived);
    }

    public bool isReceivingMessage()
    {
        return receivingMessage;
    }
}
