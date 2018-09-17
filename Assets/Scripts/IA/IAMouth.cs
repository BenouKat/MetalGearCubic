using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAMouth : MonoBehaviour {


    List<IAInformation> informationToCommunicate = new List<IAInformation>();
    List<IAInformation> informationCommunicating;
    public IABrain brain;

    public IARadio radio;

    float lastTalk = -10f;
    float lastLength = 0;

    [Range(1f, 10f)]
    public float voiceRange = 3f;
    Collider[] speakingTo = new Collider[10];

    private void Update()
    {
        //Information constant
        if (!HasNothingToSay())
        {
            if (!brain.ears.IsListeningRadio() && !IsTalkingToRadio())
            {
                SayToRadio(informationToCommunicate);
                informationToCommunicate.Clear();
                brain.currentState.ResetUpdateTime();
            }
        }
    }

    IAInformation infoToTell;
    public void TellInformationToOthers(IAInformation.InformationType type, float length, string parameters, bool order = false)
    {
        infoToTell = new IAInformation(brain.unitID, type, length, parameters, order);
        informationToCommunicate.Add(infoToTell);
        if (order)
        {
            if (infoToTell.NeedConfirmation()) brain.SetOrderConfirmation(infoToTell);
        }
        else
        {
            brain.memory.RegisterMemory(infoToTell, true);
        }
    }

    public bool HasNothingToSay()
    {
        return informationToCommunicate.Count == 0;
    }

    public IAInformation GetLastInfoToCommunicate()
    {
        if (informationToCommunicate.Count == 0) return null;

        return informationToCommunicate[informationToCommunicate.Count - 1];
    }

    public void RemoveInformation(IAInformation info)
    {
        informationToCommunicate.RemoveAll(c => c.CompareTo(info));
    }

    public void RemoveInformation(IAInformation.InformationType type, string parameters = "")
    {
        informationToCommunicate.RemoveAll(c => c.type == type && (string.IsNullOrEmpty(parameters) || c.parameters == parameters));
    }

    public bool ExistInformation(IAInformation.InformationType type, string parameters = "")
    {
        return informationToCommunicate.Exists(c => c.type == type && (string.IsNullOrEmpty(parameters) || c.parameters == parameters));
    }

    public void SpeakOut(IAEars.NoiseType type)
    {
        int resultCount = Physics.OverlapSphereNonAlloc(transform.position, voiceRange, speakingTo, 1 << UnitManager.instance.FriendLayer | 1 << UnitManager.instance.IntruderLayer);
        IABrain enemyBrainCatch;

        Debug.Log(Time.time + " - Speak : " + type.ToString() + " : " + resultCount + " ears catched");

        for (int i=0; i<resultCount; i++)
        {
            if(speakingTo[i].transform.position != transform.position)
            {
                enemyBrainCatch = speakingTo[i].GetComponent<IABrain>();
                if(enemyBrainCatch != null && enemyBrainCatch != brain)
                {
                    enemyBrainCatch.ears.Heard(brain.transform, type);
                }
            }
        }
    }

    public void SayToRadio(List<IAInformation> informations)
    {
        if(informations != null)
        {
            informationCommunicating = informations;
            StartCoroutine(SpeechRoutine());
        }
        else if(IsTalkingToRadio())
        {
            radio.StopTalk();
        }
    }

    IEnumerator SpeechRoutine()
    {
        while(informationCommunicating.Count > 0)
        {
            SayNextInfo();

            while (IARadioManager.instance.IsRadioOnline(radio))
            {
                yield return 0;
            }
        }
        informationCommunicating = null;
    }

    void SayNextInfo()
    {
        lastTalk = Time.time;
        lastLength = informationCommunicating[0].length;
        radio.Talk(informationCommunicating[0]);
        informationCommunicating.RemoveAt(0);
    }

    public bool IsTalkingToRadio()
    {
        return informationCommunicating != null || Time.time < lastTalk + lastLength;
    }

    public float GetTalkingCompletion()
    {
        return IsTalkingToRadio() ? ((Time.time - lastTalk) / lastLength) : 1f;
    }
}
