﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Item {

    public Transform canonPosition;
    public GameObject shootEffect;
    public GameObject bullet;

    public int currentAmmo;
    public int currentMagazine;

    public int maxAmmoPerMagazine;
    public int maxMagasine;

    float lastAction;

    public float timeBeforeNextShoot;
    public float timeReload;
    
    public enum WeaponType { ONESHOT = 0, AUTO = 1 };
    public WeaponType weaponType;
    public bool triggerPulled = false;

    public enum ShootOutput { VALID, FAILED, EMPTY, RELOAD }
    ShootOutput lastOutput;

    public string shootSound;
    
    public ShootOutput Shoot()
    {
        //If the weapon is not automatic, it needs to be release each time
        if(!isAutomatic() && triggerPulled)
        {
            lastOutput = ShootOutput.FAILED;
        }
        else
        {
            triggerPulled = true;
            if (Time.time - lastAction < timeBeforeNextShoot) //If we call a shoot before the next action it failed
            {
                lastOutput = ShootOutput.FAILED;
            }
            else
            {
                if (currentAmmo == 0)
                {
                    //If the current ammo are 0 but there's still magazine, we call reload
                    if (currentMagazine > 0)
                    {
                        CallReload();
                    }
                    else
                    {
                        //Else, the weapon is empty
                        lastOutput = ShootOutput.EMPTY;
                    }
                }
                else
                {
                    //Lost an ammo when the shoot is valid
                    currentAmmo--;
                    lastAction = Time.time;
                    lastOutput = ShootOutput.VALID;
                    sendBullet();
                }
            }
        }
        return lastOutput;
    }

    //Release the trigger
    public void Release()
    {
        triggerPulled = false;
    }

    //Point the canon to the right direction, relative to what has been targeted in player behavior
    public void CanonLook(Vector3 pointToLook)
    {
        canonPosition.LookAt(pointToLook, Vector3.up);
    }

    public void CanonLookForward()
    {
        canonPosition.LookAt(canonPosition.position + canonPosition.forward);
    }
    
    public void sendBullet()
    {
        //Fire ! :)
        if(shootEffect != null) InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Destroyable, shootEffect, canonPosition.position, canonPosition.rotation);
        InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Destroyable, bullet, canonPosition.position, canonPosition.rotation);
    }

    //Reload the weapon
    public void CallReload()
    {
        //Do we need to reload, and do we can ?
        if(currentAmmo < maxAmmoPerMagazine && currentMagazine > 0 && Time.time - lastAction >= timeBeforeNextShoot)
        {
            lastOutput = ShootOutput.RELOAD;

            //To not deal with 2 variables, we set the time that triggers the last action at timeReload and not timeBNS
            lastAction = Time.time + timeReload - timeBeforeNextShoot;
            StartCoroutine(Reload());
        }
        else
        {
            lastOutput = ShootOutput.FAILED;
        }
    }

    //When the time is out, we reload for good !
    IEnumerator Reload()
    {
        yield return new WaitForSeconds(timeReload);

        currentAmmo = maxAmmoPerMagazine;
        currentMagazine--;
    }

    public ShootOutput GetShootOutput()
    {
        return lastOutput;
    }

    public bool isAutomatic()
    {
        return weaponType == WeaponType.AUTO;
    }
}
