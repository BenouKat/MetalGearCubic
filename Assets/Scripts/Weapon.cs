using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Item {

    public int currentAmmo;
    public int currentMagazine;

    public int maxAmmoPerMagazine;
    public int maxMagasine;

    float lastAction;

    public float timeBeforeNextShoot;
    public float timeReload;
    
    public enum WeaponType { ONESHOT = 0, AUTO = 1 };
    public WeaponType weaponType;

    public enum ShootOutput { VALID, FAILED, EMPTY, RELOAD }
    ShootOutput lastOutput;

    public string shootSound;
    
    public ShootOutput Shoot()
    {
        if(Time.time - lastAction < timeBeforeNextShoot) //If we call a shoot before the next action it failed
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
        
        return lastOutput;
    }

    public void sendBullet()
    {

    }

    //Reload the weapon
    public void CallReload()
    {
        //Do we need to reload, and do we can ?
        if(currentAmmo < maxAmmoPerMagazine && currentMagazine > 0 && Time.time - lastAction >= timeBeforeNextShoot)
        {
            lastOutput = ShootOutput.RELOAD;

            //To not deal with 2 variables, we set the time that triggers the last action at timeReload and not timeBNS
            lastAction = Time.time - timeReload + timeBeforeNextShoot;
            StartCoroutine(Reload());
        }
        else
        {
            lastOutput = ShootOutput.FAILED;
        }
    }

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
