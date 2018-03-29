using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    Rigidbody bulletRigidbody;

    public float damage;
    public float impactRadius;
    public float forceImpact;
    public AnimationCurve forceImpactInRadius = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    public float physicalForceBullet;

    public float forceSpeedBullet;
    public float chanceBounce;
    public int maxBounce;

    int bounceCount;

    public GameObject wallBounce;
    public GameObject wallHit;
    
    RaycastHit info;

    Hitbox targetedHitbox;
    Vector3 futurHitPoint;
    Vector3 oldBulletPosition;

    private void Start()
    {
        bulletRigidbody = GetComponent<Rigidbody>();

        //We use here ForceMode.VelocityChange because we simply the physic to a no-mass bullet. 
        //Note that we can use rb.velocity here to do the same
        bulletRigidbody.AddForce(transform.forward * forceSpeedBullet, ForceMode.VelocityChange);
        ResetBulletDestination();
        CalcBulletDestination();
    }

    private void Update()
    {
        CalcBulletDestination();
    }

    //To be honest, Unity physic extrapolation is shit. Let's do it ourselves :)
    //This function calculate the future impact point (if exist), and check we have gone trough it
    void CalcBulletDestination()
    {
        //We check if the bullet has reached the impact point
        if ((futurHitPoint -transform.position).normalized == (futurHitPoint - oldBulletPosition).normalized)
        {
            //If a player is in the way, its our future impact point. We update it as long as the player move.
            if (Physics.Raycast(transform.position, transform.forward, out info, 1000f, 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Enemy")))
            {
                targetedHitbox = info.collider.GetComponent<Hitbox>();
                futurHitPoint = info.point;
            }else if (Physics.Raycast(transform.position, transform.forward, out info, 1000f, 1 << LayerMask.NameToLayer("Unmovable")))
            {
                //If there's no players in the way, we target the wall
                targetedHitbox = null;
                futurHitPoint = info.point;
            }
            oldBulletPosition = transform.position;

        }
        else
        {
            //If we have reached our target, we see where is the impact point based on its previous position
            if (Physics.Raycast(oldBulletPosition, transform.forward, out info, 1000f, 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Enemy")))
            {
                //We just check if the collider targeted is the same than the previous frame
                if(targetedHitbox == info.collider.GetComponent<Hitbox>())
                {
                    //If a player or enemy is in the way, we have touched it !
                    transform.position = info.point;
                    info.collider.GetComponent<Hitbox>().OnImpact(this);
                }
                else
                {
                    //We change the target and not update oldposition to redone the calc newt frame
                    targetedHitbox = info.collider.GetComponent<Hitbox>();
                    futurHitPoint = info.point;
                }
            }
            else if (Physics.Raycast(oldBulletPosition, transform.forward, out info, 1000f, 1 << LayerMask.NameToLayer("Unmovable")))
            {
                //We just check we are targeting a wall on this frame
                if (targetedHitbox == null)
                {
                    //If a player or enemy is in the way, we have touched it !
                    transform.position = info.point;
                    OnWallCollision(info);
                }
                else
                {
                    //We change the target to nothing and not update oldposition to redone the calc newt frame
                    targetedHitbox = null;
                    futurHitPoint = info.point;
                }
            }
            else
            {
                //Else if there's no one in the way, we reset the destination
                ResetBulletDestination();
            }
        }
    }

    void ResetBulletDestination()
    {
        oldBulletPosition = transform.position;
        futurHitPoint = transform.position + transform.forward * 1000f;
    }

    //If it's a collision, it's necessary a wall or another solid object where the bullet is stopped
    private void OnWallCollision(RaycastHit raycastInfo)
    {
        if (bounceCount < maxBounce && Random.Range(0f, 1f) <= chanceBounce)
        {
            RecalcTrajectory(raycastInfo);
            bounceCount++;
        }
        else
        {
            DestroyBulletOnWall(raycastInfo);
        }
    }

    public void RecalcTrajectory(RaycastHit info)
    {
        //We are using the raycast info here to calculate the normal to the impact, way more easier than manage Collision.ContactPoint
        //Display wall impact and make a sound
        if(wallBounce != null)
        {
            GameObject wallHitInst = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Destroyable, wallBounce, info.point, Quaternion.identity);
            wallHitInst.transform.forward = info.normal;
        }
        SoundManager.instance.play("Bounce", info.point, SoundManager.AudioType.SOUND);

        //To calculate the bounce, we just reflect the initial bullet forward to the hit surface normal
        Vector3 newBulletDirection = Vector3.Reflect(transform.forward, info.normal);
        transform.position = info.point;
        transform.forward = newBulletDirection;
        transform.LookAt(transform.position + transform.forward);

        //We unsure that the collision has not change the angular velocity and start with 0 velocity. Note that the bullet must have no freedom degrees of rotation.
        bulletRigidbody.velocity = Vector3.zero;
        bulletRigidbody.angularVelocity = Vector3.zero;

        //We add the force again, but in the right direction this time !
        bulletRigidbody.AddForce(transform.forward * forceSpeedBullet, ForceMode.VelocityChange);
        ResetBulletDestination();
        
    }

    //Basicly same as RecalcTrajetory in the logical, just replacing the effect and the sound played
    public void DestroyBulletOnWall(RaycastHit info)
    {
        //Display wall impact and make a sound
        if(wallHit != null)
        {
            GameObject wallHitInst = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Graphics, wallHit, info.point, Quaternion.identity);
            wallHitInst.transform.forward = info.normal;
        }
        SoundManager.instance.play("Impact", info.point, SoundManager.AudioType.SOUND);
        Destroy(gameObject);
        
    }
}
