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
    public string associatedAnimation;

    public enum ShootOutput { VALID, FAILED, EMPTY, RELOAD }
    ShootOutput lastOutput;
    
    public ShootOutput shoot()
    {
        if(!isReadyToShoot())
        {
            lastOutput = currentAmmo > 0 ? ShootOutput.FAILED : ShootOutput.EMPTY;
            return lastOutput;
        }

        lastAction = Time.time;
        currentAmmo--;

        if(currentAmmo == 0 && currentMagazine > 0)
        {
            StartCoroutine(reload());
            lastOutput = ShootOutput.RELOAD;
            return lastOutput;
        }

        lastOutput = ShootOutput.VALID;
        return lastOutput;
    }

    public bool isReadyToShoot()
    {
        return currentAmmo > 0 && Time.time - lastAction >= timeBeforeNextShoot;
    }

    IEnumerator reload()
    {
        yield return new WaitForSeconds(timeReload);

        currentAmmo = maxAmmoPerMagazine;
        currentMagazine--;
    }
}
