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

    public int walkingMode = 0;
    public bool aim = false;
    public bool aimMG = false;
    public bool shoot = false;
    public bool shootMG = false;

    // Update is called once per frame
    void Update()
    {
        playerAnimator.SetInteger("WalkingMode", VelocityToWalkingMode(playerBehaviour.GetPlayerVelocity()));

        if (Input.GetButtonDown("Aim"))
        {
            playerAnimator.SetBool("Aim" + playerBehaviour.GetEquipedWeapon().WeaponID, true);
        }

        if (Input.GetButtonUp("Aim"))
        {
            playerAnimator.SetBool("Aim" + playerBehaviour.GetEquipedWeapon().WeaponID, false);
        }

        if(Input.GetButton("Aim"))
        {
            if (Input.GetButtonDown("Shoot"))
            {
                Weapon.ShootOutput output = playerBehaviour.GetEquipedWeapon().GetShootOutput();
                switch (output)
                {
                    case Weapon.ShootOutput.VALID:
                        playerAnimator.SetBool("Shoot" + playerBehaviour.GetEquipedWeapon().WeaponID, true);
                        break;
                    case Weapon.ShootOutput.RELOAD:
                        playerAnimator.SetTrigger("Reload" + playerBehaviour.GetEquipedWeapon().WeaponID);
                        break;
                }
            }

            if (Input.GetButtonUp("Shoot"))
            {
                playerAnimator.SetBool("Shoot" + playerBehaviour.GetEquipedWeapon().WeaponID, false);
            }
        }
        else
        {
            if (Input.GetButtonDown("Shoot") && playerBehaviour.GetEquipedWeapon().GetShootOutput() == Weapon.ShootOutput.RELOAD)
            {
                playerAnimator.SetBool("Shoot" + playerBehaviour.GetEquipedWeapon().WeaponID, false);
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
