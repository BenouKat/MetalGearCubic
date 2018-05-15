using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAEyes : MonoBehaviour {

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
    int friendLayer;
    int wallLayer;

    public Transform enemyFocused;
    float currentVisualAcuity;

    private void Start()
    {
        if (brain == null) brain = transform.parent.GetComponent<IABrain>();

        SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
        sphere.radius = viewDistance;
        gameObject.layer = LayerMask.NameToLayer("Detection");
        inFieldOfView = new List<Collider>();
        intruderLayer = LayerMask.NameToLayer("Player");
        friendLayer = LayerMask.NameToLayer("Enemy");
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

    float distanceFromSpotRatio;
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
        if (Vector3.Angle(transform.forward, target.position - transform.position) <= fieldOfView)
        {
            return CanBeSeen(target, distance, 1 << layerTarget | 1 << wallLayer, layerTarget);
        }
        return false;
    }

    public bool CanBeSeen(Transform target, float distance, int layerMask, int layerTarget)
    {
        if (Physics.Raycast(transform.position, target.position - transform.position, out info, distance, layerMask))
        {
            if (layerTarget < 0 || info.collider.gameObject.layer == layerTarget)
            {
                return true;
            }
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

            if (Vector3.Angle(transform.forward, selectedChecker.position - transform.position) <= fieldOfView)
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
