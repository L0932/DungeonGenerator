using UnityEngine;
//using UnityEditor;
using System.Collections;


[RequireComponent (typeof (DRoom))]
//[ExecuteInEditMode]

public class RoomComponents : ScriptableObject {

	public Mesh p_Mesh;
	public Material p_Material;

	private Bounds bounds;
	private BoxCollider floorCollider;
	private MeshCollider meshCollider;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;

	/*
	void Start(){
		AddRoomComponents();
	}
	*/
	/*
	public BoxCollider GetFloorCollider(){
		return floorCollider;
	}

	public Transform GetFloor(){

		Transform child = transform.GetChild(0);
		if(child.name == "Floor"){
			return child;
		}

		Debug.Log ("Floor not found");
		return null;

	}

	public Vector3 GetWallDepth(){

		foreach(Transform child in transform){
			if(child.name == "Walls"){

			}
		}

		return new Vector3(-1000,-1000,-1000);
	}

	public void AddRoomComponents(){

		transform.position = new Vector3(0,0,0);
		Transform floor = GetFloor();

		if(floor != null){
			Transform floorChild = floor.GetChild(0);
				

				//Floor must have BoxCollider
			if(!floor.GetComponent<BoxCollider>()){
				meshCollider = floorChild.GetComponent<MeshCollider>();//new Bounds(transform.position, Vector3.one);
					
				if(!meshCollider){
					foreach(Transform child in floor){
						child.gameObject.AddComponent<MeshCollider>();
					}
				}
					
				Renderer[] renderers = floor.GetComponentsInChildren<Renderer>();

				foreach(Renderer renderer in renderers){
					bounds.Encapsulate(renderer.bounds);
				}


				floorCollider = floor.gameObject.AddComponent<BoxCollider>();
				floorCollider.size = bounds.size;
				floorCollider.center = bounds.center;
			}

			//Get static flags
			StaticEditorFlags isNavStatic = GameObjectUtility.GetStaticEditorFlags(floorChild.gameObject);
			
			//Meshes must have a MeshCollider and be navigationally static
			if(!isNavStatic.Equals(null)) {
				foreach(Transform mesh in floor){
					if(!floorChild.GetComponent<MeshCollider>()){
						meshCollider = mesh.gameObject.AddComponent<MeshCollider>();
					}
					
					if(!mesh.GetComponent<MeshFilter>()){
						meshFilter = mesh.gameObject.AddComponent<MeshFilter>();
						meshFilter.mesh = p_Mesh;
					}
					
					if(!mesh.GetComponent<MeshRenderer>()){
						meshRenderer = mesh.gameObject.AddComponent<MeshRenderer>();
						meshRenderer.material = p_Material;
					}
					
					GameObjectUtility.SetStaticEditorFlags(mesh.gameObject, StaticEditorFlags.NavigationStatic);
				}
			}
		}
	}

*/
}
