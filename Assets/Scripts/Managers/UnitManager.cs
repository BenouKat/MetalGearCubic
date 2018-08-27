using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour {

    public static UnitManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        charArray = "AZERTYUIOPQSDFGHJKLMWXCVBN".ToCharArray();

        intruderLayer = LayerMask.NameToLayer("Player");
        friendLayer = LayerMask.NameToLayer("Enemy");
        wallLayer = LayerMask.NameToLayer("Unmovable");
    }

    [System.Serializable]
    internal class EnemyPrefab
    {
        public IABrain.IABehaviour enemyType;
        public GameObject prefab;
    }

    [SerializeField]
    List<EnemyPrefab> enemyPrefabs;
    Transform currentOfficer;
    public Transform enemiesRoot;
    Zone officerZone;
    public Zone spawnZone;
    public float spawnSpacement;
    List<string> unitIDs = new List<string>();
    char[] charArray;

    [HideInInspector]
    public int intruderLayer;
    [HideInInspector]
    public int friendLayer;
    [HideInInspector]
    public int wallLayer;

    public bool needToRereshUnit;

    bool spawningAgent = false;
    public void SpawnNewAgent(int agentToSpawn, IABrain.IABehaviour enemyType)
    {
        spawningAgent = true;
        StartCoroutine(SpawnNewAgentRoutine(agentToSpawn, enemyPrefabs.Find(c => c.enemyType == enemyType).prefab));
    }

    IEnumerator SpawnNewAgentRoutine(int agentToSpawn, GameObject enemyPrefab)
    {
        for(int i=0; i<agentToSpawn; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.transform.SetParent(enemiesRoot);
            enemy.transform.position = spawnZone.transform.position;

            yield return new WaitForSeconds(spawnSpacement);
        }
        spawningAgent = false;
    }

    public bool IsSpawningAgent()
    {
        return spawningAgent;
    }

    public void SetOfficer(Transform officer)
    {
        currentOfficer = officer;
    }

    public Transform GetCurrentOfficer()
    {
        return currentOfficer;
    }

    public void SetOfficerZone(Zone zone)
    {
        officerZone = zone;
    }

    public Zone GetOfficerZone()
    {
        return officerZone;
    }

    public List<string> GetAllUnits()
    {
        return unitIDs;
    }

    //Get new ID for unit
    public string GetNewUnitID(string prefix)
    {
        string newUnit = prefix + Random.Range(0, 1000).ToString("000") + "-" + charArray[Random.Range(0, charArray.Length)];
        if(unitIDs.Contains(newUnit))
        {
            return GetNewUnitID(prefix);
        }
        unitIDs.Add(newUnit);
        return newUnit;
    }

    public void RemoveUnitID(string unit)
    {
        unitIDs.Remove(unit);
    }
}
