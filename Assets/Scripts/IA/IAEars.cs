using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAEars : MonoBehaviour {

    public IABrain brain;
    public IARadio radio;
    bool listenMessage;
    bool isReceivingMessage;
    public bool IsFocusOnConversation = false;

    public Vector2 delayStopListening;
    float lastListening;

    public enum NoiseType { INTERPEL, FRIENDLY, BYE, AGRESSIVE, WEIRD }

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

    public void Heard(Transform source, NoiseType type)
    {
        switch(type)
        {
            case NoiseType.INTERPEL:
                brain.SetCheck(source);
                break;
            case NoiseType.AGRESSIVE:
            case NoiseType.WEIRD:
                brain.mouth.SayToRadio(null);
                brain.mouth.TellInformationToOthers(IAInformation.InformationType.CHECKING, 2f, type.ToString());
                brain.SetCheck(source);
                break;
            case NoiseType.BYE:
                if(brain.currentState == IABrain.IAState.TALKING)
                {
                    brain.StopTalking();
                }
                break;
        }
    }

    void OnMessageReceptionBegin(IAInformation information)
    {
        listenMessage = true;
        isReceivingMessage = true;
    }

    void OnMessageReceptionEnd(IAInformation information)
    {
        isReceivingMessage = false;
        StartCoroutine(StopListen(Random.Range(delayStopListening.x, delayStopListening.y)));
        brain.mouth.RemoveInformation(information);
        brain.memory.RegisterMemory(information);
    }

    float timer;
    float DEBUGDelay;
    IEnumerator StopListen(float delay)
    {
        DEBUGDelay = delay;
        timer = 0f;
        while(timer < delay && !isReceivingMessage)
        {
            timer += Time.deltaTime;
            yield return 0;
        }

        if(!isReceivingMessage)
        {
            listenMessage = false;
        }
    }

    public bool IsListeningRadio()
    {
        return listenMessage;
    }

    public float GetListeningCompletion()
    {
        return isReceivingMessage ? 1f : 1f - (timer / DEBUGDelay);
    }
}
