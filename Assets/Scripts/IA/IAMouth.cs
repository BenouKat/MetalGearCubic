using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAMouth : MonoBehaviour {

    public IABrain brain;

    public IARadio radio;

    float lastTalk = -10f;
    float lastLength = 0;

	public void Say(IAInformation information)
    {
        if(information != null)
        {
            lastTalk = Time.time;
            lastLength = information.length;
            radio.Talk(information);
        }
        else if(IsTalking())
        {
            radio.InterruptTalk();
        }
    }

    public bool IsTalking()
    {
        return Time.time < lastTalk + lastLength;
    }
}
