using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAMouth : MonoBehaviour {

    List<IAInformation> informationCache;
    public IABrain brain;

    public IARadio radio;

    float lastTalk = -10f;
    float lastLength = 0;

    [Range(1f, 10f)]
    public float voiceRange = 3f;
    Collider[] speakingTo = new Collider[10];

    

    public void SpeakOut(IAEars.NoiseType type)
    {
        int resultCount = Physics.OverlapSphereNonAlloc(transform.position, voiceRange, speakingTo, 1 << UnitManager.instance.friendLayer | 1 << UnitManager.instance.intruderLayer);
        IABrain enemyBrainCatch;

        for (int i=0; i<resultCount; i++)
        {
            if(speakingTo[i].transform.position != transform.position)
            {
                enemyBrainCatch = speakingTo[i].GetComponent<IABrain>();
                if(enemyBrainCatch != null)
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
