using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour {

    public Animator playerAnimator;
    public PlayerBehaviour playerBehaviour;

	// Use this for initialization
	void Start () {
        if (playerBehaviour == null) playerBehaviour = GetComponent<PlayerBehaviour>();
        if (playerAnimator == null) playerAnimator = transform.GetChild(0).GetComponent<Animator>();
	}

    /**
     * Animation to do :
     * Legs :   - Idle
     *          - Walk
     *          - Run
     * Body :   - Idle
     *          - Walk
     *          - Run
     *          - Aim Pistol
     *          - Shoot Pistol
     *          - Aim machinegun
     *          - Shoot Machinegun
     **/
	
	// Update is called once per frame
	void Update () {
        playerAnimator.SetFloat("PlayerVelocity", playerBehaviour.getPlayerVelocity());

        if(Input.GetButtonDown("Aim"))
        {
            playerAnimator.SetBool("Aim" + playerBehaviour.getEquipedWeapon().associatedAnimation, true);
        }

        if(Input.GetButtonUp("Aim"))
        {
            playerAnimator.SetBool("Aim" + playerBehaviour.getEquipedWeapon().associatedAnimation, false);
        }
    }
}
