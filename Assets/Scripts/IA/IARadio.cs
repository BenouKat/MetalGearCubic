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

    //Connect to the radio
    public void SwitchRadioOn()
    {
        IARadioManager.instance.ConnectRadio(this);
    }

    //Disconnect to the radio
    public void SwitchRadioOff()
    {
        IARadioManager.instance.DisconnectRadio(this);
    }

    //Talk on radio
    public void Talk(IAInformation information)
    {
        IARadioManager.instance.BeginCommunication(this, information, channel);
    }

    //Stop talk to radio (interrupt)
    public void StopTalk()
    {
        IARadioManager.instance.EndCommunication(channel);
    }

    //Message reception begins on radio
    public void BeginMessageReception(IAInformation information)
    {
        informationReceived = information;
        receivingMessage = true;
        if(OnMessageReceptionBegin != null) OnMessageReceptionBegin.Invoke(informationReceived);
    }

    //Message reception is progression on radio
    public void ProcessMessageReception(float completion)
    {
        informationReceived.completion = completion;
    }

    //Message on radio has ended
    public void EndMessageReception()
    {
        receivingMessage = false;
        if (OnMessageReceptionEnd != null) OnMessageReceptionEnd.Invoke(informationReceived);
    }

    public bool isReceivingMessage()
    {
        return receivingMessage;
    }
}
