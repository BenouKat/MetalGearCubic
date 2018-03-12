using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour {

    public Animator playerAnimator;
    public PlayerBehaviour playerBehaviour;

    public float[] walkingModeSpeed;
    int armLayer;

	// Use this for initialization
	void Start () {
        if (playerBehaviour == null) playerBehaviour = GetComponent<PlayerBehaviour>();
        if (playerAnimator == null) playerAnimator = transform.GetChild(0).GetComponent<Animator>();
        armLayer = playerAnimator.GetLayerIndex("ArmLayer");
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
     *          - ReloadPistol
     *          - ReloadMachineGun
     **/
     
	// Update is called once per frame
	void Update () {
        
        playerAnimator.SetFloat("PlayerVelocity", VelocityToWalkingMode(playerBehaviour.GetPlayerVelocity()));

        if(Input.GetButtonDown("Aim"))
        {
            playerAnimator.SetBool("Aim" + playerBehaviour.GetEquipedWeapon().WeaponID, true);
        }

        if(Input.GetButtonUp("Aim"))
        {
            playerAnimator.SetBool("Aim", false);
        }

        if(Input.GetButtonDown("Shoot"))
        {
            Weapon.ShootOutput output = playerBehaviour.GetEquipedWeapon().GetShootOutput();
            switch(output)
            {
                case Weapon.ShootOutput.VALID:
                    playerAnimator.Play("Shoot" + playerBehaviour.GetEquipedWeapon().WeaponID, armLayer);
                    break;
                case Weapon.ShootOutput.RELOAD:
                    playerAnimator.Play("Reload" + playerBehaviour.GetEquipedWeapon().WeaponID, armLayer);
                    break;
            }
        }
    }

    int VelocityToWalkingMode(float velocity)
    {
        for(int i=0; i<walkingModeSpeed.Length; i++)
        {
            if (walkingModeSpeed[i] > velocity) return i - 1;
        }
        return walkingModeSpeed.Length - 1;
    }
}
