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
    }

    [System.Serializable]
    internal struct EnemyPrefab
    {
        public IABrain.IABehaviour enemyType;
        public GameObject prefab;
    }

    [SerializeField]
    List<EnemyPrefab> m_enemyPrefabs;
	[HideInInspector]
    public Transform CurrentOfficer;
	[SerializeField]
    Transform m_enemiesRoot;
    public Zone OfficerZone;
    public Zone SpawnZone;
    public float SpawnSpacement;

    List<string> unitIDs = new List<string>();
    char[] charArray;
	Queue<GameObject> m_spawnQueue = new Queue<GameObject>();
	float m_spawnTimer;

	public readonly LayerManager Layers;

    bool m_isSpawningAgent = false;
	public bool IsSpawningAgent { get { return m_isSpawningAgent; } }

    public void SpawnNewAgent(int agentToSpawn, IABrain.IABehaviour enemyType)
    {
		m_isSpawningAgent = true;

		for(int i=0;i<agentToSpawn;i++)
           m_spawnQueue.Enqueue(m_enemyPrefabs.Find(c => c.enemyType == enemyType).prefab);
    }

	void Update()
	{
		if(m_isSpawningAgent)
		{
			m_spawnTimer += Time.deltaTime;
			if(m_spawnTimer > SpawnSpacement)
			{
				GameObject enemy = Instantiate(m_spawnQueue.Dequeue());
				enemy.transform.SetParent(m_enemiesRoot);
				enemy.transform.position = SpawnZone.transform.position;
				m_spawnTimer = 0f;
				m_isSpawningAgent = m_spawnQueue.Count > 0;
			}
		}
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
