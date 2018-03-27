using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour {

    [Header("Life")]
    public float maxLife;
    float currentLife;

    [Header("Shield")]
    public float shieldPercentAtSides;
    float minDistanceCenter;
    float maxDistanceCenter;

    [Header("Blood")]
    public GameObject bloodEffect;
    public float timeBeforeDivideOnDeath = 1f;
    public Material bloodMaterial;

    [Header("Hit")]
    bool enableHitAnimation = false;
    public GameObject body;
    float timeCurrentRotation;
    public float timeRotationIn;
    public float timeRotationOut;
    public float forceRotation;
    Vector3 bulletLocalPosition;
    Vector3 rotationApplied;
    Transform startRotation;
    Transform rotationTarget;

    private void Start()
    {
        //Taking the closest edge point of the cube from the center
        minDistanceCenter = Mathf.Min(transform.lossyScale.x/2f, transform.lossyScale.z/2f);
        //Taking the farest edge point of the cube from the center
        maxDistanceCenter = Vector3.Distance(Vector3.zero, transform.lossyScale/2f);

        currentLife = maxLife;
    }

    //On a player (or enemy) enter contact with a bullet
    public void OnImpact(Bullet bullet)
    {
        //Loss of life and blood effect
        //We applied a shield if the damage is on side of the hitbox
        currentLife -= bullet.damage - (bullet.damage * shieldPercentAtSides * Mathf.Clamp((Vector3.Distance(transform.position, bullet.transform.position) - minDistanceCenter) / (maxDistanceCenter - minDistanceCenter), 0f, 1f));
        
        if(bloodEffect != null)
        {
            InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Graphics, bloodEffect, bullet.transform.position, bullet.transform.rotation);
        }
            
        //If the life is under 0, the player dies
        if(currentLife <= 0f)
        {
            DieEffect(bullet);
        }
        else //Else, he's just hit !
        {
            HitEffect(bullet);
        }

        Destroy(bullet.gameObject);
    }

    void HitEffect(Bullet bullet)
    {
        //Calculate the rotation done to the body by the bullet
        bulletLocalPosition = bullet.transform.position - transform.position;
        rotationApplied = Vector3.zero;
        CalcHitRotation(bulletLocalPosition);

        //Instance a rotation helper that will be our goal rotation
        rotationTarget = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "RotationTarget").transform;
        rotationTarget.SetParent(body.transform.parent);
        rotationTarget.localPosition = body.transform.localPosition;
        rotationTarget.localRotation = body.transform.localRotation;
        rotationTarget.Rotate(rotationApplied);
        
        //Instance a start rotation
        startRotation = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "VirtualRotation").transform;
        startRotation.SetParent(body.transform.parent);
        startRotation.localRotation = body.transform.localRotation;

        timeCurrentRotation = 0f;
        enableHitAnimation = true;
    }

    //Calculate the rotation depending of where the hit is. We actually doing simple physics here, but without physics. Heh.
    void CalcHitRotation(Vector3 bulletLocalPosition)
    {
        if (bulletLocalPosition.z >= minDistanceCenter * 0.9f) //Front
        {
            if (Mathf.Abs(bulletLocalPosition.x) >= minDistanceCenter * 0.35f) //Front right/left side
            {
                rotationApplied += Mathf.Sign(bulletLocalPosition.x) * Vector3.up * forceRotation;
            }

            //If not really on side or up/bottom side
            if (rotationApplied.y == 0f || Mathf.Abs(bulletLocalPosition.y) >= minDistanceCenter * 0.35f)
            {
                rotationApplied -= Mathf.Sign(bulletLocalPosition.y) * Vector3.right * forceRotation;
            }
        }
        else if (bulletLocalPosition.z <= -(minDistanceCenter * 0.9f)) //Back
        {
            //We calculate the inverse of the front
            CalcHitRotation(-bulletLocalPosition);
        }
        else //Right or left side
        {
            if (Mathf.Abs(bulletLocalPosition.z) >= minDistanceCenter * 0.35f) //Front right/left side
            {
                rotationApplied -= Mathf.Sign(bulletLocalPosition.z) * Mathf.Sign(bulletLocalPosition.x) * Vector3.up * forceRotation;
            }

            //If not really on side or up/bottom side
            if (rotationApplied.y == 0f || Mathf.Abs(bulletLocalPosition.y) >= minDistanceCenter * 0.35f)
            {
                rotationApplied += Mathf.Sign(bulletLocalPosition.y) * Mathf.Sign(bulletLocalPosition.x) * Vector3.forward * forceRotation;
            }
        }
    }

    //During hit animation, slowly ping-pong between the goal rotation calculated and the start rotation.
    private void LateUpdate()
    {
        if(enableHitAnimation)
        {
            timeCurrentRotation += Time.deltaTime;
            if (timeCurrentRotation < timeRotationIn)
            {
                body.transform.localRotation = Quaternion.Slerp(startRotation.localRotation, rotationTarget.localRotation, timeCurrentRotation / timeRotationIn);
            }else if(timeCurrentRotation < timeRotationIn + timeRotationOut)
            {
                body.transform.localRotation = Quaternion.Slerp(rotationTarget.localRotation, startRotation.localRotation, (timeCurrentRotation-timeRotationIn) / timeRotationOut);
            }
            else
            {
                body.transform.localRotation = startRotation.localRotation;
                Destroy(startRotation.gameObject);
                Destroy(rotationTarget.gameObject);
                enableHitAnimation = false;
            }
        }
    }

    //The die effect will blow the player (or enemy) up
    List<GameObject> physicalObjects = new List<GameObject>();
    List<Rigidbody> instRigidObject = new List<Rigidbody>();
    List<Rigidbody> bodyPartRigidObject = new List<Rigidbody>();
    List<GameObject> parentObjectOfSubdivision = new List<GameObject>();
    public float sizeSubdivision;

    /*This is the die effect. A simple-complicated effect of subdivide any big part of the player.
     * Only works for this "marvellous designed" game character ;) Assume that it works of everything made of cubes and have no children under a mesh renderer !
     * This effect is pretty heavy to process because of the high number of instance done, and should make disappear the part of the body quickly to avoid too much object in the scene, or at least cut the physics.
     **/
    void DieEffect(Bullet bullet)
    {
        //First we search all the gameObject that compose the player. We don't take the root.
        SearchAllGameobject(transform.GetChild(0));

        //Calculation of the volume represented by the given subdivision
        float volumeSubdivision = Mathf.Pow(sizeSubdivision, 3f);

        BloodManager.instance.setTimeBeforeDisappear(timeBeforeDivideOnDeath);
        BloodManager.instance.registerNewBloodMaterial(new Material(bloodMaterial));

        foreach (GameObject gameObj in physicalObjects)
        {
            //We just move the object to the instance manager, to clean the scene
            InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Graphics, gameObj);

            //We add box and rigidbody to this object, we set the layer
            BoxCollider boxObj = gameObj.AddComponent<BoxCollider>();
            boxObj.isTrigger = false;
            Rigidbody rigidObj = gameObj.AddComponent<Rigidbody>();
            gameObj.layer = LayerMask.NameToLayer("Default");

            //We disable the box collider and the rigidbody for now
            boxObj.enabled = false;
            rigidObj.useGravity = false;
            rigidObj.isKinematic = true;

            //Here's the touchy part : If the object can contains at least 4 subdivision, we'll devide it
            if (gameObj.transform.lossyScale.x * gameObj.transform.lossyScale.y * gameObj.transform.lossyScale.z >= 4f* volumeSubdivision)
            {
                //First we calculate how many subdivision we are going to have on each side x, y, z. We ceil it to avoid a 0 case.
                Vector3 maxSubdivision = gameObj.transform.lossyScale / sizeSubdivision;
                maxSubdivision = new Vector3(Mathf.Ceil(maxSubdivision.x), Mathf.Ceil(maxSubdivision.y), Mathf.Ceil(maxSubdivision.z));

                //To simply the maths, we'll use the child property to adjust local position. The local position for one of the cube angle is -0.5, -0.5, -0.5
                //We had the half of a subdivision size to avoid a translation for instancing the small object (if not, the subdivision will be center at the corner, which is not good)
                Vector3 positionOriginSubdivision = (Vector3.one * -0.5f) + new Vector3(1f / (maxSubdivision.x * 2f), 1f / (maxSubdivision.y * 2f), 1f / (maxSubdivision.z * 2f));

                //We locate the model in another variable (to prevent instancing children)
                GameObject gameObjModel = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Destroyable, gameObj);

                //We are rolling all subdivision
                for (int x=0; x<maxSubdivision.x; x++)
                {
                    for(int y=0; y<maxSubdivision.y; y++)
                    {
                        for(int z=0; z<maxSubdivision.z; z++)
                        {
                            //The global scale of the subdivision
                            Vector3 scaleSubdivision = new Vector3(gameObj.transform.localScale.x / maxSubdivision.x,
                                                                        gameObj.transform.localScale.y / maxSubdivision.y,
                                                                            gameObj.transform.localScale.z / maxSubdivision.z);

                            //Local subdivision position, by going from Vector3(-0.5) to Vector3(0.5)
                            Vector3 positionSubdivision = positionOriginSubdivision + new Vector3((x / maxSubdivision.x), (y / maxSubdivision.y), (z / maxSubdivision.z));
                            
                            //Instancing the mini object
                            GameObject miniObject = Instantiate(gameObjModel, positionSubdivision, gameObj.transform.rotation);
                            
                            //We set the local scale (which is the global scale)
                            miniObject.transform.localScale = scaleSubdivision;
                            
                            //We set it under the parent object to conserve physic-links for the beginning
                            //This trick avoid us to do the maths of the correct translation, taking rotation into account
                            miniObject.transform.SetParent(gameObj.transform);
                            miniObject.transform.localPosition = positionSubdivision;

                            //If the object is "hidden" in the center of the cube subdivided
                            //Tag "SubdivideTop" : top of the cube, don't display bloody subdivision at the top
                            //Tag "SubdivideBottom" : bottom of the cube, don't display bloody subdivision at the bottom
                            if ((x > 0 && x < maxSubdivision.x - 1 && z > 0 && z < maxSubdivision.z - 1) 
                                && ((y > 0 || miniObject.tag == "SubdivideBottom") && (y < maxSubdivision.y - 1 || miniObject.tag == "SubdivideTop")))
                            {
                                //The Cubes insides are blood
                                BloodManager.instance.registerNewBloodObject(miniObject.AddComponent<BloodObject>());
                            }
                            else
                            {
                                //The cubes are body part
                                BloodManager.instance.registerNewBodyPartObject(miniObject.AddComponent<BloodObject>());
                            }

                            instRigidObject.Add(miniObject.GetComponent<Rigidbody>());
                        }
                    }
                }

                //This is for the parent, we destroy the empty model
                Destroy(gameObjModel);
                parentObjectOfSubdivision.Add(gameObj);
                //The layer of the parent must be in a layer that exclude the subdivisions, to avoid conflict during forces
                gameObj.layer = LayerMask.NameToLayer("AvoidMovables");

                //We enable all the physics. Note that it can be tweaked to have a better rag-doll reaction.
                boxObj.enabled = true;
                rigidObj.useGravity = true;
                rigidObj.isKinematic = false;
                rigidObj.mass = 0.2f;
                rigidObj.drag = 10f;
                rigidObj.angularDrag = 0f;

                //To move the parent object, we had a force relative to the bullet position and the bullet physical force
                rigidObj.AddForceAtPosition((bullet.transform.forward).normalized * bullet.physicalForceBullet ,bullet.transform.position, ForceMode.Force);
                gameObj.GetComponent<MeshRenderer>().enabled = false;
            }
            else //If it's too small, we don't devide and use the object itself
            {
                BloodManager.instance.registerNewBodyPartObject(rigidObj.gameObject.AddComponent<BloodObject>());
                bodyPartRigidObject.Add(rigidObj);
            }
        }

        //If there's any parent of subdivision, we find the biggest one and assume that it's the body.
        if(parentObjectOfSubdivision != null && parentObjectOfSubdivision.Count > 0)
        {
            parentObjectOfSubdivision.Sort(delegate (GameObject go1, GameObject go2)
            {
                return (go1.transform.lossyScale.x * go1.transform.lossyScale.y * go1.transform.lossyScale.z).CompareTo(go2.transform.lossyScale.x * go2.transform.lossyScale.y * go2.transform.lossyScale.z);
            });
            GameObject biggestObject = parentObjectOfSubdivision.FindLast(c => c);

            //We make children every body parts that are not subdivide
            foreach (Rigidbody rbo in bodyPartRigidObject)
            {
                rbo.transform.SetParent(biggestObject.transform);
            }
        }


        //We apply a force into all the small object, like a shockwave, closer the object is from the impact, higher will be the force.
        //Only the object in the shockwave moves. Others stays non-physics for the moment
        foreach(Rigidbody rbo in instRigidObject)
        {
            float rboDistance = Vector3.Distance(rbo.transform.position, bullet.transform.position);
            if (rboDistance < bullet.impactRadius)
            {
                InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Graphics, rbo.gameObject);
                rbo.isKinematic = false;
                rbo.useGravity = true;
                rbo.isKinematic = false;
                rbo.GetComponent<BoxCollider>().enabled = true;
                rbo.AddForce((rbo.transform.position - bullet.transform.position).normalized * bullet.forceImpactInRadius.Evaluate(rboDistance / bullet.impactRadius) * bullet.forceImpact, ForceMode.Impulse);
            }
        }
        instRigidObject.RemoveAll(c => c.useGravity);
        instRigidObject.AddRange(bodyPartRigidObject);

        //We enable physics after a shot period of time
        Invoke("EnableAllRigidbodyPhysics", timeBeforeDivideOnDeath);
    }

    //This enabled all the rigidbody that hasn't moves in the die effect
    void EnableAllRigidbodyPhysics()
    {
        foreach (Rigidbody rbo in instRigidObject)
        {
            //We conserve the velocity of the parent, best effect ;)
            rbo.velocity = rbo.transform.parent.GetComponent<Rigidbody>().velocity;
            rbo.angularVelocity = rbo.transform.parent.GetComponent<Rigidbody>().angularVelocity;

            //Physics activation ! o(-_o)==o
            InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Graphics, rbo.gameObject);
            rbo.isKinematic = false;
            rbo.useGravity = true;
            rbo.isKinematic = false;
            rbo.GetComponent<BoxCollider>().enabled = true;
        }

        //Destoying all empty parents
        foreach(GameObject po in parentObjectOfSubdivision)
        {
            Destroy(po);
        }
        
        //We destroy the root object, to prevent all player/enemy interaction
        Destroy(gameObject);
    }

    //Collecting all the meshes and delete their current physic preperties if it exist
    void SearchAllGameobject(Transform parent)
    {
        if(parent.GetComponent<Animator>())
        {
            parent.GetComponent<Animator>().enabled = false;
        }

        if (parent.GetComponent<Collider>())
        {
            Destroy(parent.GetComponent<Collider>());

            if (parent.GetComponent<Rigidbody>())
            {
                Destroy(parent.GetComponent<Rigidbody>());
            }
        }

        if (parent.GetComponent<MeshRenderer>() != null)
        {
            physicalObjects.Add(parent.gameObject);
        }

        if(parent.childCount > 0)
        {
            for(int i=0; i<parent.childCount; i++)
            {
                SearchAllGameobject(parent.GetChild(i));
            }
        }
    }
}
