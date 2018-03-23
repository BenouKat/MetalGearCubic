using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour {

    public float maxLife;
    float currentLife;
    public float shieldPercentAtSides;

    public GameObject bloodEffect;

    //On a player (or enemy) enter contact with a bullet
    public void OnTriggerEnter(Collider other)
    {
        Bullet bullet = other.GetComponent<Bullet>();
        if(bullet != null)
        {
            //Loss of life and blood effect
            currentLife -= bullet.damage;
            if(bloodEffect != null)
            {
                InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Graphics, bloodEffect, bullet.transform.position, bullet.transform.rotation);
            }
            
            //If the life is under 0, the player dies
            if(currentLife <= 0f)
            {
                DieEffect(bullet.transform);
            }

            UnityEditor.EditorApplication.isPaused = true;
            //Destroy(bullet.gameObject);
        }
    }

    //The die effect will blow the player (or enemy) up
    List<GameObject> physicalObjects = new List<GameObject>();
    List<Rigidbody> rigidbodyObjects = new List<Rigidbody>();
    public float sizeSubdivision;
    public float radiusOfImpact;
    public float centerForceImpact;
    public AnimationCurve forceImpactInRadius = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

    void DieEffect(Transform originImpact)
    {
        //First we search all the gameObject that compose the player. We don't take the root.
        SearchAllGameobject(transform.GetChild(0));

        //Calculation of the volume represented by the given subdivision
        float volumeSubdivision = Mathf.Pow(sizeSubdivision, 3f);

        foreach(GameObject gameObj in physicalObjects)
        {
            //We just move the object to the instance manager, to clean the scene
            InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Graphics, gameObj);

            //We add box and rigidbody to this object, we set the layer
            BoxCollider boxObj = gameObj.AddComponent<BoxCollider>();
            boxObj.isTrigger = false;
            Rigidbody rigidObj = gameObj.AddComponent<Rigidbody>();
            rigidObj.useGravity = true;
            gameObj.layer = LayerMask.NameToLayer("Default");

            //Here's the touchy part : If the object can contains at least 4 subdivision, we'll devide it
            if (gameObj.transform.lossyScale.x * gameObj.transform.lossyScale.y * gameObj.transform.lossyScale.z >= 4f* volumeSubdivision)
            {
                //First we calculate how many subdivision we are going to have on each side x, y, z. We ceil it to avoid a 0 case.
                Vector3 maxSubdivision = gameObj.transform.lossyScale / sizeSubdivision;
                maxSubdivision = new Vector3(Mathf.Ceil(maxSubdivision.x), Mathf.Ceil(maxSubdivision.y), Mathf.Ceil(maxSubdivision.z));

                //To simply the maths, we'll use the child property to adjust local position. The local position for one of the cube angle is -0.5, -0.5, -0.5
                //We had the half of a subdivision size to avoid a translation for instancing the small object (if not, the subdivision will be center at the corner, which is not good)
                Vector3 positionOriginSubdivision = (Vector3.one * -0.5f) + new Vector3(1f / (maxSubdivision.x * 2f), 1f / (maxSubdivision.y * 2f), 1f / (maxSubdivision.z * 2f));

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
                            GameObject miniObject = Instantiate(gameObj, positionSubdivision, gameObj.transform.rotation);

                            //We just set it under the parent object to do the localTransform maths.
                            //This trick avoid us to do the maths of the correct translation, taking rotation into account
                            miniObject.transform.SetParent(gameObj.transform);
                            miniObject.transform.localPosition = positionSubdivision;

                            //We set it at the same level of everyone
                            miniObject.transform.SetParent(gameObj.transform.parent);
                            miniObject.transform.localScale = scaleSubdivision;
                            rigidbodyObjects.Add(miniObject.GetComponent<Rigidbody>());
                        }
                    }
                }
                Destroy(gameObj);
            }
            else //If it's too small, we don't devide and use the object itself
            {
                rigidbodyObjects.Add(rigidObj);
            }
        }

        //We apply a force into all the small object, like a shockwave, closer the object is from the impact, higher will be the force.
        foreach(Rigidbody rbo in rigidbodyObjects)
        {
            float rboDistance = Vector3.Distance(rbo.transform.position, originImpact.transform.position);
            if (rboDistance > radiusOfImpact) rboDistance = radiusOfImpact;
            rbo.AddForce((originImpact.transform.position - rbo.transform.position).normalized * forceImpactInRadius.Evaluate(rboDistance / radiusOfImpact) * centerForceImpact, ForceMode.Impulse);
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
