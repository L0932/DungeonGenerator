using UnityEngine;
using System.Collections.Generic;

public class TestScript : MonoBehaviour {

	public List<GameObject> planes;

	public int sizeX;
	public int sizeY;

	private List<Vector3> planeSizes;
	private List<GameObject> scenePlanes;

	void Start(){

		//planes = new List<GameObject>();
		planeSizes = new List<Vector3>();
		scenePlanes = new List<GameObject>();

		planeSizes.Add(planes[0].transform.GetComponent<Renderer>().bounds.size);
		planeSizes.Add(planes[1].transform.GetComponent<Renderer>().bounds.size);

		placeStartPlane();
		placePlanes();

	}

	void placeStartPlane(){


		scenePlanes.Add(Instantiate(planes[0], new Vector3(0,0,0), Quaternion.identity) as GameObject);
	}

	private void placePlanes()
	{
		Vector3 scale, worldPos;
		for(int i = 0; i < sizeX; i++)
		{
			for(int j = 0; j < sizeY; j++)
			{
				//scale = new Vector3(Target.x/planeSizes[0].x*j,0,Target.z/planeSizes[1].z * j);
				//worldPos = new Vector3((planeSizes[j].x - (planeSizes[j].x % 10)) * j, 0, (planeSizes[j].z - (planeSizes[j].z % 10)* j));

				int r1 = Random.Range (0, scenePlanes.Count);
				int r2 = Random.Range (0, planes.Count);

				GameObject current = scenePlanes[r1];

				Vector3 localS1 = current.transform.localScale * .5f;
				Vector3 localS2 = planes[r2].transform.localScale * .5f;
				Vector3 scaleFactor = localS1 + localS2;

				
				worldPos = 10 * scaleFactor * j;
				worldPos.y = 0;

				scenePlanes.Add(Instantiate(planes[r2] as GameObject, worldPos, Quaternion.identity) as GameObject);
			}
		}
	}
	/*
	[ExecuteInEditMode]

	#if UNITY_EDITOR
	void Update()
	{
		Debug.Log("Its time: " + Time.time);
	}
	#endif
	*/
}
