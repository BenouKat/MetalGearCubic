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
        if(Time.time - lastAction < timeBeforeNextShoot)
        {
             lastOutput = ShootOutput.FAILED;
        }
        else
        {
            if (currentAmmo == 0)
            {
                if (currentMagazine > 0)
                {
                    CallReload();
                }
                else
                {
                    lastOutput = ShootOutput.EMPTY;
                }
            }
            else
            {
                currentAmmo--;
                lastAction = Time.time;
                lastOutput = ShootOutput.VALID;
            }
        }
        
        return lastOutput;
    }

    //A revoir...
    public void CallReload()
    {
        if(currentAmmo < maxAmmoPerMagazine && currentMagazine > 0)
        {
            lastOutput = ShootOutput.RELOAD;
            lastAction = Time.time;
            CallReload();
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
}
