using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Items {

    public int currentAmmo;
    public int currentMagazine;

    public int maxAmmoPerMagazine;
    public int maxMagasine;

    float lastAction;
    public float timeBeforeNextShoot;
    public float timeReload;

    public bool automatic;
    public string WeaponID;

    public enum ShootOutput { VALID, FAILED, EMPTY, RELOAD }
    ShootOutput lastOutput;
    
    public ShootOutput Shoot()
    {
        if(!IsReadyToShoot())
        {
            lastOutput = (currentAmmo > 0 || currentMagazine > 0) ? ShootOutput.FAILED : ShootOutput.EMPTY;
        }
        else
        {
            lastAction = Time.time;
            currentAmmo--;

            if (currentAmmo == 0 && currentMagazine > 0)
            {
                StartCoroutine(Reload());
                lastOutput = ShootOutput.RELOAD;
            }
            else
            {
                lastOutput = ShootOutput.VALID;
            }
        }
        
        return lastOutput;
    }

    public bool IsReadyToShoot()
    {
        return currentAmmo > 0 && Time.time - lastAction >= timeBeforeNextShoot;
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
}
