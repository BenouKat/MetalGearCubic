using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAMouth : MonoBehaviour {

    List<IAInformation> informationCache;
    public IABrain brain;

    public IARadio radio;

    float lastTalk = -10f;
    float lastLength = 0;

    public void Say(List<IAInformation> informations)
    {
        if(informations != null)
        {
            informationCache = informations;
            StartCoroutine(SpeechRoutine());
        }
        else if(IsTalking())
        {
            radio.StopTalk();
        }
    }

    IEnumerator SpeechRoutine()
    {
        while(informationCache.Count > 0)
        {
            SayNextInfo();

            while (IARadioManager.instance.IsRadioOnline(radio))
            {
                yield return 0;
            }
        }
        informationCache = null;
    }

    void SayNextInfo()
    {
        lastTalk = Time.time;
        lastLength = informationCache[0].length;
        radio.Talk(informationCache[0]);
        informationCache.RemoveAt(0);
    }

    public bool IsTalking()
    {
        return informationCache != null || Time.time < lastTalk + lastLength;
    }

    public float GetTalkingCompletion()
    {
        return IsTalking() ? ((Time.time - lastTalk) / lastLength) : 1f;
    }
}
