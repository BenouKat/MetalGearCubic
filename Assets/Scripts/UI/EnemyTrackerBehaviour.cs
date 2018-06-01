using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyTrackerBehaviour : TrackerBehaviour {

    IABrain brain;
    IABrain.IAState previousState;
    public Image filledImage;
    public RectTransform imageTransform;
    public ParticleSystem particleListening;

    [System.Serializable]
    internal class StateColor
    {
        public IABrain.IAState state = IABrain.IAState.IDLE;
        public Color color = Color.white;
    }
    [SerializeField]
    List<StateColor> stateColors;

	public override void InitBehaviour(RadarTracker tracker)
    {
        brain = tracker.GetComponent<IABrain>();
        filledImage.fillAmount = 0f;
        ChangeStateColor();
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

        if(previousState != brain.currentState)
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
        filledImage.color = (stateColors.Find(c => c.state == brain.currentState) ?? stateColors.Find(c => c.state == IABrain.IAState.IDLE)).color;
        previousState = brain.currentState;
    }
}
