using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListenRadioManager : MonoBehaviour {

    public IARadio radio;
    public Text dialogText;
    IAInformation currentInfo;
	// Use this for initialization
	void Start () {
        radio.OnMessageReceptionBegin += OnMessageBegin;
        radio.OnMessageReceptionEnd += OnMessageEnd;

        dialogText.text = "";
    }

    bool debugMessage = false;
	public void OnMessageBegin(IAInformation info)
    {
        if (debugMessage) Debug.LogWarning("Message begin but the previous didn't end...");
        debugMessage = true;

        currentInfo = info;
        switch (currentInfo.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                dialogText.text = "- \"I'm heading to the " + info.parameters + ".\"";
                break;
            case IAInformation.InformationType.ZONECLEAR:
                dialogText.text = "- \"The " + info.parameters + " is clear. Nothing here. \"";
                break;
            case IAInformation.InformationType.ALREADYZONE:
                dialogText.text = "- \"No, I'm already at " + info.parameters + ". Go somewhere else.\"";
                break;
            case IAInformation.InformationType.REPLACEZONE:
                dialogText.text = "- \"Hey. I'm close to the " + info.parameters + ". I will check for you.\"";
                break;
            case IAInformation.InformationType.DEVIATETOZONE:
                dialogText.text = "- \"Negative, there's no need to check the " + info.parameters.Split('$')[0] 
                    + ". Check the " + info.parameters.Split('$')[1] + " instead.\"";
                break;
            case IAInformation.InformationType.OK:
                dialogText.text = "- \"OK.\"";
                break;
            case IAInformation.InformationType.NOK:
                dialogText.text = "- \"Negative.\"";
                break;

        }
    }

    public void OnMessageEnd(IAInformation info)
    {
        if (!debugMessage) Debug.LogWarning("Message end with no begin...");
        debugMessage = false;
        dialogText.text = "";
    }
}
