﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimation : MonoBehaviour {

    public Animator enemyAnimator;
    public IABrain enemyBrain;
    public Hitbox enemyHitbox;

    public float[] walkingModeSpeed;
    bool isCollidingWithWall;
    float lastCurrentLife;
	// Use this for initialization
	void Start () {
        if (enemyBrain == null) enemyBrain = GetComponent<IABrain>();
        if (enemyAnimator == null) enemyAnimator = transform.GetChild(0).GetComponent<Animator>();
        if (enemyHitbox == null) enemyHitbox = GetComponent<Hitbox>();
        lastCurrentLife = enemyHitbox.getCurrentLife();
    }

    // Update is called once per frame
    void Update()
    {
        enemyAnimator.SetInteger("WalkingMode", VelocityToWalkingMode(enemyBrain.legs.GetEnemyVelocity()));
        enemyAnimator.SetBool("Equiped", enemyBrain.arms.GetWeaponOut());
        enemyAnimator.SetInteger("WeaponMode", (int)enemyBrain.arms.GetEquipedWeapon().weaponType);

        enemyAnimator.SetBool("Aim", enemyBrain.arms.IsAimingWithWeapon());

        //If we aim, do we shoot ?
        if (enemyBrain.arms.IsAimingWithWeapon())
        {
            //Shoot
            if (enemyBrain.arms.IsShootingWithWeapon())
            {
                Weapon.ShootOutput output = enemyBrain.arms.GetEquipedWeapon().GetShootOutput();
                switch (output)
                {
                    //Shoot is valid, we do anim shoot !
                    case Weapon.ShootOutput.VALID:
                        enemyAnimator.SetBool("Shoot", true);
                        enemyAnimator.SetTrigger("TriggerPulled");
                        break;
                    //Oups, we need to reload, play reload animation
                    case Weapon.ShootOutput.RELOAD:
                        enemyAnimator.SetBool("Shoot", false);
                        enemyAnimator.SetTrigger("Reload");
                        break;
                }
            }
            else
            {
                //We stop the shoot animation in case it's a loop (machine gun)
                enemyAnimator.SetBool("Shoot", false);
            }
        }

        if(lastCurrentLife != enemyHitbox.getCurrentLife() && enemyHitbox.getCurrentLife() > 0f)
        {
            enemyAnimator.SetTrigger("Hurt");
            lastCurrentLife = enemyHitbox.getCurrentLife();
        }
    }

    //Convert the player velocity into a walking state
    int VelocityToWalkingMode(float velocity)
    {
        for(int i=1; i<walkingModeSpeed.Length; i++)
        {
            if (walkingModeSpeed[i] >= velocity) return i - 1;
        }
        return walkingModeSpeed.Length - 1;
    }
}
