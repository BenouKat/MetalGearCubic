using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour {

    public GameObject equipedWeaponPrefab;
    Weapon equipedWeapon;

    public Transform weaponCarrier;
    public Transform itemHandledPosition;

    //Movement
    Transform enemyRotation;
    public float speedRotationSmooth;
    float walkingSpeed;
    Vector3 oldPosition;

    //State
    bool hasWeaponOut = false;
    bool isLookingAt = false;
    bool isAiming = false;
    bool isShooting = false;
    Vector3 targetAim;

	// Use this for initialization
	void Start () {

        //Util for applying a smooth rotation to enemy
        enemyRotation = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "PlayerRotation").transform;

        equipedWeapon = (Instantiate(equipedWeaponPrefab) as GameObject).GetComponent<Weapon>();
        equipedWeapon.Picked();
        SetWeaponOut(false);
    }
	
	// Update is called once per frame
	void Update () {

        //Velocity Update
        UpdatePosition();

        //Aim Update
        if(isLookingAt)
        {
            UpdateAimBehaviour();
        }

        //Rotation and late position update
        UpdateLatePosition();
    }

    //This is the very simplifid version of the playerBehaviour stuff
    public void UpdatePosition()
    {
        enemyVelocity = Vector3.Distance(oldPosition, transform.position);
        enemyRotation.position = transform.position;

        if (oldPosition != transform.position)
        {
            enemyRotation.LookAt(-oldPosition);
        }

        oldPosition = transform.position;
    }

    public void UpdateAimBehaviour()
    {
        enemyRotation.LookAt(targetAim);
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

    public void UpdateLatePosition()
    {
        //Player look in the direction (with a quick smooth)
        transform.rotation = Quaternion.Slerp(transform.rotation, enemyRotation.rotation, speedRotationSmooth);
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
        isLookingAt = true;
        isAiming = aim;
        targetAim = target;
        targetAim.y = equipedWeapon.canonPosition.position.y;
    }

    public void StopAim()
    {
        isLookingAt = false;
        isAiming = false;
    }

    public bool IsAimingWithWeapon()
    {
        return isAiming;
    }

    float enemyVelocity;
    public float GetEnemyVelocity()
    {
        return enemyVelocity;
    }

    public Weapon GetEquipedWeapon()
    {
        return equipedWeapon;
    }
}
