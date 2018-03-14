using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour {

    public Animator playerAnimator;
    public PlayerBehaviour playerBehaviour;

    public float[] walkingModeSpeed;

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
     *          - ReloadPistol
     *          - ReloadMachineGun
     **/

    // Update is called once per frame
    void Update()
    {
        playerAnimator.SetInteger("WalkingMode", VelocityToWalkingMode(playerBehaviour.GetPlayerVelocity()));
        
        if (playerBehaviour.GetEquipedWeapon() != null)
        {
            playerAnimator.SetInteger("WeaponMode", (int)playerBehaviour.GetEquipedWeapon().weaponType);
            playerAnimator.SetBool("Aim", Input.GetButton("Aim"));

            //If we aim, do we shoot ?
            if (Input.GetButton("Aim"))
            {
                //Shoot
                if (Input.GetButtonDown("Shoot"))
                {
                    Weapon.ShootOutput output = playerBehaviour.GetEquipedWeapon().GetShootOutput();
                    switch (output)
                    {
                        case Weapon.ShootOutput.VALID:
                            playerAnimator.SetBool("Shoot", true);
                            break;
                        case Weapon.ShootOutput.RELOAD:
                            playerAnimator.SetBool("Shoot", false);
                            playerAnimator.SetTrigger("Reload");
                            break;
                    }
                }

                //We stop the shoot animation in case it's a loop (machine gun)
                if (Input.GetButtonUp("Shoot"))
                {
                    playerAnimator.SetBool("Shoot", false);
                }
            }
            else
            {
                //If we are not aiming but clic the shoot button, it reloads the weapon
                if (Input.GetButtonDown("Shoot") && playerBehaviour.GetEquipedWeapon().GetShootOutput() == Weapon.ShootOutput.RELOAD)
                {
                    playerAnimator.SetTrigger("Reload");
                }
            }
        }
    }

    //Convert the player velocity into a walking state
    int VelocityToWalkingMode(float velocity)
    {
        for(int i=0; i<walkingModeSpeed.Length; i++)
        {
            if (walkingModeSpeed[i] > velocity) return i - 1;
        }
        return walkingModeSpeed.Length - 1;
    }
}
