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
        dialogText.text = currentInfo.from + " : ";
        string[] parametersStatus = currentInfo.parameters.Split('$');

        switch (currentInfo.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                dialogText.text += "\"I'm heading to the " + info.parameters + ".\"";
                break;
            case IAInformation.InformationType.ZONECLEAR:
                dialogText.text += "\"The " + info.parameters + " is clear. Nothing here. \"";
                break;
            case IAInformation.InformationType.ALREADYZONE:
                dialogText.text += "\"No, I'm already at " + info.parameters + ". Go somewhere else.\"";
                break;
            case IAInformation.InformationType.REPLACEZONE:
                dialogText.text += "\"Hey. I'm close to the " + info.parameters + ". I will check for you.\"";
                break;
            case IAInformation.InformationType.OK:
                dialogText.text += "\"OK.\"";
                break;
            case IAInformation.InformationType.NOK:
                dialogText.text += "\"Negative.\"";
                break;
            case IAInformation.InformationType.ASKSTATUS:
                dialogText.text += "\"" + (currentInfo.parameters == "all" ? "Team" : currentInfo.parameters) + ", report your current situation.\"";
                break;
            case IAInformation.InformationType.TELLSTATUS:
                dialogText.text += "\"" + parametersStatus[0] + " here, currently in the " + parametersStatus[1] + ".\"";
                break;
            case IAInformation.InformationType.DEVIATETOZONE:
                if(parametersStatus[0] == "all")
                {
                    dialogText.text += "\"To all unit, check the " + parametersStatus[1] + " immediatly. We might have a problem.\"";
                }
                else
                {
                    dialogText.text += "\"" + parametersStatus[0] + ", check the " + parametersStatus[1] + " to see if there's nothing wrong.\"";
                }
                break;
            case IAInformation.InformationType.MEETOFFICER:
                dialogText.text += "\"" + info.parameters + ", meet me at my office, I need to talk to you in private.\"";
                break;
            case IAInformation.InformationType.BRINGTOOFFICER:
                dialogText.text += "\"" + parametersStatus[0] + ", look for " + GetRandomItem() + " in the " + parametersStatus[1] + " and bring it to me.\"";
                break;

        }
    }

    List<string> randomItem = new List<string>() { "something to eat", "a book", "some unclassified folders", "an empty USB key", "a security report" };
    public string GetRandomItem()
    {
        return randomItem[Random.Range(0, randomItem.Count)];
    }

    public void OnMessageEnd(IAInformation info)
    {
        if (!debugMessage) Debug.LogWarning("Message end with no begin...");
        debugMessage = false;
        dialogText.text = "";
    }
}
