﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyTrackerBehaviour : TrackerBehaviour {

    IABrain brain;
    IAState.IAStateTag previousState;
    public Image filledImage;
    public RectTransform imageTransform;
    public ParticleSystem particleListening;

    [System.Serializable]
    internal class StateColor
    {
        public IAState.IAStateTag state = IAState.IAStateTag.IDLE;
        public Color color = Color.white;
    }
    [SerializeField]
    List<StateColor> stateColors;

	public override void InitBehaviour(RadarTracker tracker)
    {
        brain = tracker.GetComponent<IABrain>();
        filledImage.fillAmount = 0f;
        filledImage.color = stateColors.Find(c => c.state == IAState.IAStateTag.IDLE).color;
    }

    // Update is called once per frame
    Vector3 eulerAngles;
    bool isTalking = false;
	void Update () {
        if(!Mathf.Approximately(filledImage.fillAmount, brain.eyes.fieldOfView / 360f))
        {
            filledImage.fillAmount = brain.eyes.fieldOfView / 360f;
            eulerAngles = imageTransform.localEulerAngles;
            eulerAngles.z = brain.eyes.fieldOfView / 2f;
            imageTransform.sizeDelta = Vector2.one * brain.eyes.spotDistance * 2.5f;
            imageTransform.localEulerAngles = eulerAngles;
        }

        if(previousState != brain.currentState.tag)
        {
            ChangeStateColor();
        }

        isTalking = brain.mouth.IsTalkingToRadio();

        if (isTalking && !particleListening.isEmitting)
        {
            particleListening.Play();
        }else if(!isTalking && particleListening.isEmitting)
        {
            particleListening.Stop();
        }
    }

    void ChangeStateColor()
    {
        filledImage.color = (stateColors.Find(c => c.state == brain.currentState.tag) ?? stateColors.Find(c => c.state == IAState.IAStateTag.IDLE)).color;
        previousState = brain.currentState.tag;
    }
}
