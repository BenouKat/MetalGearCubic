using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RadarManager))]
public class RadarEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        if (GUILayout.Button("Generate level"))
        {
            ((RadarManager)target).GenerateStaticLevel();
        }
    }
}

#endif

public class RadarManager : MonoBehaviour {

    public static RadarManager instance;
    public Sprite whiteSquareSprite;
    public Transform radarRoot;
    Transform trackerRoot;

    public Color radarColor;
    [Range(0f, 1f)]
    public float wallWidth;
    public float floorScaleOffset;

    [System.Serializable]
    internal class TrackerObject
    {
        public RadarTracker.RadarType type = RadarTracker.RadarType.ENEMY;
        public GameObject model = null;
    }
    [SerializeField]
    List<TrackerObject> trackerObjects;

    List<RadarTracker> trackers = new List<RadarTracker>();

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        if (radarRoot == null) radarRoot = transform.Find("UILevel");

        trackerRoot = (new GameObject("Trackers")).transform;
        SetParentRoot(trackerRoot, radarRoot);
        trackerRoot.position += Vector3.up;
    }

    public void GenerateStaticLevel()
    {
        Sprite wallSprite = Sprite.Create(whiteSquareSprite.texture, new Rect(0f, 0f, whiteSquareSprite.texture.width, whiteSquareSprite.texture.height),
            whiteSquareSprite.pivot, whiteSquareSprite.pixelsPerUnit, 1, SpriteMeshType.Tight, Vector4.one * whiteSquareSprite.texture.width * wallWidth);

        if(radarRoot == null) radarRoot = transform.Find("UILevel");
        if(radarRoot.Find("StaticLevel") != null)
        {
            DestroyImmediate(radarRoot.Find("StaticLevel").gameObject);
        }

        Transform staticLevelRoot = (new GameObject("StaticLevel")).transform;
        SetParentRoot(staticLevelRoot, radarRoot);

        Transform floor = transform.Find("Floor").GetChild(0);
        RectTransform floorSprite = InstanceSprite(wallSprite, staticLevelRoot, floor);
        floorSprite.sizeDelta = new Vector3(floor.localScale.x - floorScaleOffset, floor.localScale.y - floorScaleOffset);

        Transform wallRoot = transform.Find("Walls");
        for(int i=0; i<wallRoot.childCount; i++)
        {
            for(int j=0; j<wallRoot.GetChild(i).childCount; j++)
            {
                InstanceSprite(wallSprite, staticLevelRoot, wallRoot.GetChild(i).GetChild(j));
            }
        }
    }

    void SetParentRoot(Transform obj, Transform root)
    {
        obj.SetParent(root);
        obj.localPosition = Vector3.zero;
        obj.localRotation = Quaternion.identity;
        obj.localScale = Vector3.one;
    }

    public RectTransform InstanceSprite(Sprite wallSprite, Transform radarRoot, Transform cubeTransform)
    {
        GameObject sprite = new GameObject("Wall");
        sprite.transform.SetParent(radarRoot);
        sprite.isStatic = true;

        RectTransform rectTransform = sprite.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(cubeTransform.localScale.x, cubeTransform.localScale.z);
        rectTransform.position = new Vector3(cubeTransform.position.x, sprite.transform.parent.position.y, cubeTransform.position.z);
        rectTransform.Rotate(Vector3.right * 90f, Space.Self);
        rectTransform.Rotate(Vector3.forward * cubeTransform.eulerAngles.y, Space.Self);

        Image image = sprite.AddComponent<Image>();
        image.sprite = wallSprite;
        image.type = Image.Type.Sliced;
        image.fillCenter = false;
        image.color = radarColor;

        return rectTransform;
    }

    public void AddTracker(RadarTracker tracker)
    {
        trackers.Add(tracker);

        GameObject trackModel = Instantiate(trackerObjects.Find(c => c.type == tracker.type).model) as GameObject;
        SetParentRoot(trackModel.transform, radarRoot);
        tracker.SetTrackerModel(trackModel.transform);
        TrackerBehaviour trackerBehaviour = trackModel.GetComponent<TrackerBehaviour>();
        if (trackerBehaviour != null)
        {
            trackerBehaviour.InitBehaviour(tracker);
        }
    }

    public void RemoveTracker(RadarTracker tracker)
    {
        if(tracker.GetTrackerModel() != null) Destroy(tracker.GetTrackerModel().gameObject);
        trackers.Remove(tracker);
    }

    Vector3 tempPosition;
    private void Update()
    {
        foreach(RadarTracker tracker in trackers)
        {
            tempPosition = tracker.transform.position;
            tempPosition.y = trackerRoot.position.y;
            tracker.GetTrackerModel().position = tempPosition;
            tracker.GetTrackerModel().rotation = tracker.transform.rotation;
        }
    }
}
