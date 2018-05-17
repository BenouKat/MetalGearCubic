using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAArms : MonoBehaviour {

    public IABrain brain;

    public GameObject equipedWeaponPrefab;
    Weapon equipedWeapon;

    public Transform weaponCarrier;
    public Transform itemHandledPosition;

    //State
    bool hasWeaponOut = false;
    bool isAiming = false;
    bool isShooting = false;
    Vector3 targetAim;

    // Use this for initialization
    void Start()
    {
        equipedWeapon = (Instantiate(equipedWeaponPrefab) as GameObject).GetComponent<Weapon>();
        equipedWeapon.Picked();
        SetWeaponOut(false);
    }

    public void Shoot()
    {
        Weapon.ShootOutput output = equipedWeapon.Shoot();

        switch (output)
        {
            case Weapon.ShootOutput.VALID:
                SoundManager.instance.play(equipedWeapon.shootSound, transform.position, SoundManager.AudioType.SOUND);
                break;
            case Weapon.ShootOutput.EMPTY:
                SoundManager.instance.play("EmptyAmmo", transform.position, SoundManager.AudioType.SOUND);
                break;
            case Weapon.ShootOutput.RELOAD:
                SoundManager.instance.play("Reload", transform.position, SoundManager.AudioType.SOUND);
                break;
        }

        isShooting = true;
    }

    public void StopShoot()
    {
        equipedWeapon.Release();
        isShooting = false;
    }

    public bool IsShootingWithWeapon()
    {
        return isShooting;
    }

    //Is the enemy take the weapon on his hand
    public void SetWeaponOut(bool weaponOut)
    {
        hasWeaponOut = weaponOut;

        equipedWeapon.Equip((weaponOut ? itemHandledPosition : weaponCarrier));
    }

    public bool GetWeaponOut()
    {
        return hasWeaponOut;
    }

    public void Aim(Vector3 target, bool aim = false)
    {
        isAiming = aim;
        targetAim = target;
        targetAim.y = equipedWeapon.canonPosition.position.y;
    }

    public void StopAim()
    {
        isAiming = false;
    }

    public bool IsAimingWithWeapon()
    {
        return isAiming;
    }

    public Weapon GetEquipedWeapon()
    {
        return equipedWeapon;
    }
}
