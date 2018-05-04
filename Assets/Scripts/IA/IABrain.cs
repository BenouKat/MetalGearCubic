using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IABrain : MonoBehaviour {

    public bool isActiveState;
    public enum PassiveState { WORKING, TALKING, WAITING }
    public enum ActiveState { SPOT, FREEZE, ALERT, DANGER, PRUDENCE }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
