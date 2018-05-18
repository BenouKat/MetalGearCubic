using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(IARadioManager))]
class IARadioManagerEditor : Editor
{
    public void OnSceneGUI()
    {
        IARadioManager radio = ((IARadioManager)target);

        radio.DrawRadioEditor();
    }
}
#endif

public class IARadioManager : MonoBehaviour {

    #region Editor Getters
#if UNITY_EDITOR
    public void DrawRadioEditor()
    {
        foreach(IARadio radio in radioNetwork)
        {
            radio.transform.parent.parent.GetComponent<IABrain>().DrawBrainEditor();
        }
    }
#endif
    #endregion

    public static IARadioManager instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    List<IARadio> radioNetwork = new List<IARadio>();
    List<IARadio> onlineRadio = new List<IARadio>();

    public enum Channels { LOCALS, INTERTEAM }

    //Connect the radio on the network
    public void ConnectRadio(IARadio radio)
    {
        if(!radioNetwork.Contains(radio))
        {
            radioNetwork.Add(radio);
        }
    }

    //Disconnect the radio from the network
    public void DisconnectRadio(IARadio radio)
    {
        radioNetwork.Remove(radio);
    }

    public void BeginCommunication(IARadio inputRadio, IAInformation information, Channels channel)
    {
        //Add the radio to the online radio. There's only one online radio per channel.
        onlineRadio.Add(inputRadio);

        //Begin the message reception on all connected radio on this channel
        foreach (IARadio radio in radioNetwork)
        {
            if(radio.channel == channel && !IsRadioOnline(radio))
            {
                radio.BeginMessageReception(information);
            }
        }

        StartCoroutine(ProcessCommunication(channel, information.length));
    }

    IEnumerator ProcessCommunication(Channels channel, float messageLength)
    {
        //Talking into the radio
        float communicationTime = 0f;
        while(communicationTime < messageLength && IsChannelOnline(channel))
        {
            yield return 0;
            //Process the message on the radio
            communicationTime += Time.deltaTime;
            foreach (IARadio radio in radioNetwork)
            {
                if (radio.channel == channel && !IsRadioOnline(radio))
                {
                    //Process completion
                    radio.ProcessMessageReception(Mathf.Clamp(communicationTime / messageLength, 0f, 1f));
                }
            }
        }
        //If the channel is still online, end the communication
        if(IsChannelOnline(channel))
        {
            EndCommunication(channel);
        }
    }

    public void EndCommunication(Channels channel)
    {

        foreach (IARadio radio in radioNetwork)
        {
            //End communication on all connected radio on this channel
            if (radio.channel == channel && !IsRadioOnline(radio))
            {
                radio.EndMessageReception();
            }
        }
        
        //Remove the radio from being online
        onlineRadio.RemoveAll(c => c.channel == channel);
    }

    bool IsChannelOnline(Channels channel)
    {
        return onlineRadio.Exists(c => c.channel == channel);
    }

    bool IsRadioOnline(IARadio radio)
    {
        return onlineRadio.Contains(radio);
    }
}
