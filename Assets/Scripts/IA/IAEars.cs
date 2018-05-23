using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAEars : MonoBehaviour {

    public IABrain brain;
    public IARadio radio;
    bool listenMessage;
    bool isReceivingMessage;
    public bool IsFocus = true;

    public Vector2 delayStopListening;
    float lastListening;

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
        isReceivingMessage = true;
    }

    void OnMessageReceptionEnd(IAInformation information)
    {
        isReceivingMessage = false;
        StartCoroutine(StopListen(Random.Range(delayStopListening.x, delayStopListening.y)));
        brain.RemoveSameInformationToTell(information);
        brain.RegisterMemory(information);
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

    public bool IsListening()
    {
        return listenMessage;
    }

    public float GetListeningCompletion()
    {
        return isReceivingMessage ? 1f : 1f - (timer / DEBUGDelay);
    }
}
