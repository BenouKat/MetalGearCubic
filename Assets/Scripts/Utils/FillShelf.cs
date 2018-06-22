using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(FillShelf))]
public class FillShelfEditor : Editor{
	
	public override void OnInspectorGUI()
	{
        DrawDefaultInspector();

		if(GUILayout.Button("Fill"))
		{
			((FillShelf)target).Fill();
		}
		
	}
}

public class FillShelf : MonoBehaviour {
	
	public List<Transform> plate;
	BoxCollider shape;
	public float percentSideOffset;
	public List<Material> materials;
	public Mesh meshBox;
	[Range(1f, 20f)]
	public float density;
	public float randomRotation;
	public float percentDeepOffset;
	
	public Vector3 boxMinSize;
	Vector3 boxMinSizeLocal;
	public Vector3 boxMaxSize;
	Vector3 boxMaxSizeLocal;

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("refresh");
            Fill();
        }
    }

#endif

    public void Fill()
	{
        shape = GetComponent<BoxCollider>();
		Transform shelfContent = transform.Find("ShelfContent");
		if(shelfContent != null)
		{
			for(int i=shelfContent.childCount-1; i>=0; i--)
			{
				DestroyImmediate(shelfContent.GetChild(i).gameObject);
			}
		}else{
			shelfContent = (new GameObject("ShelfContent")).transform;
			shelfContent.SetParent(transform);
			shelfContent.localPosition = Vector3.zero;
			shelfContent.localRotation = Quaternion.identity;
			shelfContent.localScale = Vector3.one;
		}

		boxMinSizeLocal.x = boxMinSize.x / transform.localScale.x;
		boxMinSizeLocal.y = boxMinSize.y / transform.localScale.y;
		boxMinSizeLocal.z = boxMinSize.z / transform.localScale.z;
		boxMaxSizeLocal.x = boxMaxSize.x / transform.localScale.x;
		boxMaxSizeLocal.y = boxMaxSize.y / transform.localScale.y;
		boxMaxSizeLocal.z = boxMaxSize.z / transform.localScale.z;
		
		float groundPos;
		float length = shape.size.x * (1f-percentSideOffset);
		
		for(int i=-1; i<plate.Count; i++)
		{
			if(i == -1)
			{
				groundPos =  -(shape.size.y/2f) + shape.center.y;
			}else{
				groundPos = plate[i].localPosition.y + (plate[i].localScale.y/2f);
			}
			
			float currentLength = -length/2f;
			Transform oldBox = null;
            bool topBox = false;
			Vector3 randomScale;
			do{
				randomScale = new Vector3(Random.Range(boxMinSize.x, boxMaxSize.x), Random.Range(boxMinSize.y, boxMaxSize.y), Random.Range(boxMinSize.z, boxMaxSize.z));;
                randomScale.y = Mathf.Lerp(boxMinSizeLocal.y, boxMaxSizeLocal.y, ((randomScale.x / boxMaxSizeLocal.x) + (randomScale.z / boxMaxSizeLocal.z)) / 2f);


				if(!topBox && oldBox != null && oldBox.localScale.x > randomScale.x && oldBox.localScale.z > randomScale.z 
				&& ((i == plate.Count - 1) || ((groundPos + oldBox.localScale.y + randomScale.y) < (plate[i+1].localPosition.y - (plate[i+1].localScale.y/2f)))))
				{
					Transform box = InstanceBox(shelfContent);
					box.localScale = randomScale;
					box.localPosition = new Vector3(oldBox.localPosition.x, oldBox.localPosition.y + (oldBox.localScale.y/2f) + (randomScale.y/2f), oldBox.localPosition.z + Random.Range(-percentDeepOffset, percentDeepOffset));
                    topBox = true;
				}else{
                    topBox = false;
                    currentLength += Random.Range(oldBox == null ? 0f : oldBox.localScale.x / 2f, (oldBox == null ? 0f : (oldBox.localScale.x / 2f)) + (length / density));
                    currentLength += randomScale.x/2f;
				
					if(oldBox == null && currentLength - (randomScale.x/2f) < -length/2f)
					{
						currentLength += -length/2f - (currentLength - (randomScale.x/2f));
					}else if(oldBox != null && currentLength + (randomScale.x/2f) > length/2f)
					{
						currentLength -= (currentLength + (randomScale.x/2f)) - (length/2f);
						
						if((oldBox.localPosition.x + (oldBox.localScale.x/2f) + (randomScale.x/2f)) > currentLength)
						{
							break;
						}
					}
					
					Transform box = InstanceBox(shelfContent);
					box.localScale = randomScale;
					box.localPosition = new Vector3(currentLength, groundPos + box.localScale.y/2f, Random.Range(-percentDeepOffset, percentDeepOffset));
					oldBox = box;
				}
				
				
				
			}while(currentLength < length/2f);
		}
	}
	
	public Transform InstanceBox(Transform root)
	{
		GameObject box = new GameObject("Box");
		Transform boxTrans = box.transform;
		boxTrans.SetParent(root);
		boxTrans.localRotation = Quaternion.identity;
		boxTrans.Rotate(Vector3.up, Random.Range(-randomRotation, randomRotation));
		
		MeshFilter meshFilter = box.AddComponent<MeshFilter>();
		meshFilter.mesh = meshBox;
		
		MeshRenderer meshRenderer = box.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = materials[Random.Range(0, materials.Count)];

        box.isStatic = true;
		
		return boxTrans;
	}
}
