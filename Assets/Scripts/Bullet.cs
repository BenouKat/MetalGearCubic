using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    Rigidbody bulletRigidbody;

    public float damage;

    public float forceBullet;
    public float chanceBounce;
    public int maxBounce;

    int bounceCount;

    public GameObject wallBounce;
    public GameObject wallHit;
    
    RaycastHit info;

    private void Start()
    {
        bulletRigidbody = GetComponent<Rigidbody>();

        //FIRE !! We use here ForceMode.VelocityChange because we simply the physic to a no-mass bullet. Note that we can use rb.velocity here to do the same
        bulletRigidbody.AddForce(transform.forward * forceBullet, ForceMode.VelocityChange);
    }

    //If it's a collision, it's necessary a wall or another solid object where the bullet is stopped
    private void OnCollisionEnter(Collision hitObject)
    {
        if (bounceCount < maxBounce && Random.Range(0f, 1f) <= chanceBounce)
        {
            RecalcTrajectory(hitObject.collider.gameObject.layer);
        }
        else
        {
            DestroyBulletOnWall(hitObject.collider.gameObject.layer);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Player or Enemy hit
        //To do
    }

    public void RecalcTrajectory(int layerObject)
    {
        //We are doing a raycast here to calculate the normal to the impact, way more easier than manage Collision.ContactPoint
        if (Physics.Raycast(transform.position - transform.forward * 1f, transform.forward, out info, 10f, 1 << layerObject))
        {
            //Display wall impact and make a sound
            GameObject wallHitInst = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Destroyable, wallBounce, info.point, Quaternion.identity);
            wallHitInst.transform.forward = info.normal;
            SoundManager.instance.play("Bounce", info.point, SoundManager.AudioType.SOUND);

            //To calculate the bounce, we just reflect the initial bullet forward to the hit surface normal
            Vector3 newBulletDirection = Vector3.Reflect(transform.forward, info.normal);
            transform.position = info.point;
            transform.forward = newBulletDirection;

            //We unsure that the collision has not change the angular velocity. Note that the bullet must have no freedom degrees of rotation.
            bulletRigidbody.angularVelocity = Vector3.zero;

            //We add the force again, but in the right direction this time !
            bulletRigidbody.AddForce(transform.forward * forceBullet, ForceMode.VelocityChange);
        }
    }

    //Basicly same as RecalcTrajetory in the logical, just replacing the effect and the sound played
    public void DestroyBulletOnWall(int layerObject)
    {
        if (Physics.Raycast(transform.position - transform.forward*1f, transform.forward, out info, 10f, 1 << layerObject))
        {
            //Display wall impact and make a sound
            GameObject wallHitInst = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Graphics, wallHit, info.point, Quaternion.identity);
            wallHitInst.transform.forward = info.normal;
            SoundManager.instance.play("Impact", info.point, SoundManager.AudioType.SOUND);
        }
    }
}
