using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(IAEyes))]
class IAEyesEditor : Editor
{
    public void OnSceneGUI()
    {
        IAEyes eyes = ((IAEyes)target);

        eyes.DrawEyesEditor();
    }
}
#endif

public class IAEyes : MonoBehaviour {

    #region Editor Getters
#if UNITY_EDITOR
    public void DrawEyesEditor()
    {
        Handles.color = new Color(1f, 1f, 0f, 0.05f);
        Handles.DrawSolidArc(transform.position, transform.up, transform.forward, fieldOfView/2f, viewDistance);
        Handles.DrawSolidArc(transform.position, transform.up, transform.forward, -fieldOfView/2f, viewDistance);

        Handles.color = new Color(1f, 0f, 0f, 0.3f);
        Handles.DrawSolidArc(transform.position, transform.up, transform.forward, fieldOfView/2f, spotDistance);
        Handles.DrawSolidArc(transform.position, transform.up, transform.forward, -fieldOfView/2f, spotDistance);
    }
#endif
    #endregion
    
    public IABrain brain;

    [Range(0f, 360f)]
    public float fieldOfView;
    [Range(0.1f, 50f)]
    public float viewDistance;
    [Range(0.1f, 50f)]
    public float spotDistance;
    public AnimationCurve visualAcuityByDistance;
    public int checkersPerFrame;

    List<Collider> inFieldOfView;
    int intruderLayer;
    //int friendLayer;
    int wallLayer;

    Transform enemyFocused;
    float currentVisualAcuity;

    private void Start()
    {
        if (brain == null) brain = transform.parent.GetComponent<IABrain>();

        SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
        sphere.radius = viewDistance / brain.transform.localScale.x;
        gameObject.layer = LayerMask.NameToLayer("Detection");
        inFieldOfView = new List<Collider>();
        intruderLayer = LayerMask.NameToLayer("Player");
        //friendLayer = LayerMask.NameToLayer("Enemy");
        wallLayer = LayerMask.NameToLayer("Unmovable");
    }

    private void OnTriggerEnter(Collider col)
    {
        inFieldOfView.Add(col);
    }

    private void OnTriggerExit(Collider col)
    {
        inFieldOfView.Remove(col);
    }
    
    RaycastHit info;
    public void LookToEnemy()
    {
        if(enemyFocused != null)
        {
            KeepEnemy();
        }
        else
        {
            SearchEnemy();
        }
    }

    float distanceFromSpotRatio; // < 0 : Not in range, 0 : View distance, 1 : Spot distance
    public void SearchEnemy()
    {
        foreach (Collider col in inFieldOfView)
        {
            if (IsOnViewSight(col.transform, intruderLayer))
            {
                ProcessVisualAcuity(col.transform);
                if(currentVisualAcuity > 0f)
                {
                    enemyFocused = info.collider.transform;
                }
            }
        }
    }

    public void KeepEnemy()
    {
        if (!IsOnViewSight(enemyFocused, intruderLayer))
        {
            currentVisualAcuity = 0f;
            enemyFocused = null;
        }
        else
        {
            ProcessVisualAcuity(enemyFocused);
        }
    }

    public void ProcessVisualAcuity(Transform target)
    {
        distanceFromSpotRatio = 1f - ((Vector3.Distance(transform.position, target.position) - spotDistance) / (viewDistance - spotDistance));
        if (distanceFromSpotRatio >= 0f)
        {
            if (currentVisualAcuity < 1f)
            {
                currentVisualAcuity += (Time.deltaTime * visualAcuityByDistance.Evaluate(distanceFromSpotRatio)) / brain.GetInternalStateDecision(IABrain.IAState.SPOT);
            }
            else
            {
                currentVisualAcuity = 1f;
            }
        }
    }

    public bool IsOnViewSight(Transform target, int layerTarget)
    {
        return IsOnSight(target, viewDistance, layerTarget);
    }

    public bool IsOnSpotSight(Transform target, int layerTarget)
    {
        return IsOnSight(target, spotDistance, layerTarget);
    }

    public bool IsOnSight(Transform target, float distance, int layerTarget)
    {
        if (Vector3.Angle(transform.forward, target.position - transform.position) <= fieldOfView/2f)
        {
            return CanBeSeen(target, distance, layerTarget);
        }
        return false;
    }

    int layerRaycast;
    float distanceToTarget;
    bool raycastTest;
    public bool CanBeSeen(Transform target, float distance, int layerTarget)
    {
        layerRaycast = 1 << layerTarget | 1 << wallLayer;
        if (layerTarget <= 0)
        {
            layerRaycast = 1 << wallLayer;
            distanceToTarget = Vector3.Distance(target.position, transform.position);
            if (distance > distanceToTarget)
            {
                distance = distanceToTarget;
            }
        }
        
        if (Physics.Raycast(transform.position, target.position - transform.position, out info, distance, layerRaycast))
        {
            if ((layerTarget <= 0 && info.collider.tag == "Floor") || info.collider.gameObject.layer == layerTarget)
            {
                return true;
            }
        }
        else if(layerTarget <= 0 && distanceToTarget <= distance)
        {
            return true;
        }
        return false;
    }

    public bool HasTargetOnSight()
    {
        return enemyFocused != null && currentVisualAcuity >= 1f;
    }

    public Transform GetEyesTarget()
    {
        return enemyFocused;
    }

    int memoryIndex = 0;
    Transform selectedChecker;
    public void ProcessCheckers(ref List<Transform> checkers)
    {
        for(int i=0; i<checkersPerFrame; i++)
        {
            if (checkers.Count == 0) break;
            if (memoryIndex >= checkers.Count)
            {
                memoryIndex = 0;
            }

            selectedChecker = checkers[memoryIndex];

            if (Vector3.Angle(transform.forward, selectedChecker.position - transform.position) <= fieldOfView/2f)
            {
                if (Physics.Raycast(transform.position, selectedChecker.position - transform.position, out info, spotDistance, 1 << wallLayer))
                {
                    if (info.collider.tag == "Floor")
                    {
                        checkers.Remove(selectedChecker);
                    }
                }
            }

            memoryIndex++;
        }
        
    }
}
