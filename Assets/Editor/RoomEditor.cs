using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0219

//[CustomEditor(typeof(RoomComponents))]
[CustomEditor(typeof(DRoom))]
public class RoomEditor : Editor {

	//private Transform floor;
	enum FolderNames {
		StartRooms,
		NormalRooms,
		SpecialRooms
	}
	private static RoomEditor _instance;
	public static RoomEditor Instance {
		get{
			if(!_instance){
				_instance = (RoomEditor) ScriptableObject.CreateInstance(typeof(RoomEditor));
			}
			return _instance;
		}
	}

	public GameObject gObject;
	private Transform tFloor, tWalls, tEntries, tTorches;
	private RoomClass rcRoom;

	private Transform tCells;

	private float wallHeight;
	//private GameObject Entries;

	override public void OnInspectorGUI(){
		//RoomComponents rcScript = (RoomComponents)target;

		DRoom roomScript = (DRoom)target;

		//DrawDefaultInspector ();
	
		roomScript.roomType = (ObjectType)EditorGUILayout.EnumPopup("Room Type", roomScript.roomType);
		roomScript.roomData = (RoomData)EditorGUILayout.ObjectField ("Room Data", roomScript.roomData, typeof(RoomData));
		roomScript.traps = EditorGUILayout.Toggle ("Traps", roomScript.traps);

		//EditorGUILayout.LabelField ("Add something");

		if (roomScript.roomType == ObjectType.Boss) {
			roomScript.boss = (GameObject)EditorGUILayout.ObjectField ("Boss", roomScript.boss, typeof(GameObject));
		}

		if (roomScript.roomType == ObjectType.Special) {
			roomScript.itemDisplay = (GameObject) EditorGUILayout.ObjectField("Item Display", roomScript.itemDisplay, typeof(GameObject));
		}

		serializedObject.Update ();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("Enemies"), true);
		serializedObject.ApplyModifiedProperties();

		/*
		if(GUILayout.Button ("Add Required Components")){
			//rcScript.AddRoomComponents();
		}*/
	}

	/*
	[MenuItem("Assets/Create/RoomData")]
	static void NewRoomComponent(){
		//ScriptableObjectUtility.CreateAsset<RoomData>();
		ScriptableObject.CreateInstance(typeof(RoomData));
	}
	*/

	[MenuItem ("RoomEditor/RoomPrefab From Selected")]
	static void CreatePrefabFromSelected(){
		Instance.ComposeRoomPrefab();
	}

	[MenuItem ("RoomEditor/Fix Transforms of Selected")]
	static void FixSelectedRoom(){
		Instance.DoSomething();
	}

	/*
	[MenuItem ("RoomEditor/Add Torches")]
	static void AddTorches(){

		if (Instance.InitVariables()) {
			Instance.PlaceTorches(Instance.gObject);
		}
	}*/


	private bool InitVariables(){

		gObject = Selection.activeObject as GameObject;
		
		if (gObject == null) {
			EditorUtility.DisplayDialog(
				"Select Room",
				"You Must Select a room first!",
				"Ok");
			return false;
		}

		foreach(Transform child in gObject.transform){ 
			if(child.name == "Floor")
				tFloor = child;
			else if
				(child.name == "Walls")
					tWalls = child;
			else if
				(child.name == "Entries")
					tEntries = child;
			else if
				(child.name == "Torches")
					tTorches = child;
		}

		if(tFloor == null){
			EditorUtility.DisplayDialog(
				"Room Floor",
				"Room must contain a 'Floor' child object!",
				"Ok");
			return false;;
		}

		if(tWalls == null){
			EditorUtility.DisplayDialog(
				"Room Walls",
				"Room must contain a 'Walls' child object!",
				"Ok");
			return false;
		}

		if (tTorches != null) {
			DestroyImmediate(tTorches.gameObject);
		}

		return true;
	}
	
	private void ComposeRoomPrefab(){

		if (!InitVariables())
			return;

		ValidateRoom(gObject);
		SaveRoom(gObject);

	}

	private void DoSomething(){

		if (!InitVariables ()) {
			return;
		}


		GameObject  gCells = new GameObject ();
		gCells.name = "Cells";

		//Change parent of each cell
		Transform[] floorMeshes = tFloor.GetComponentsInChildren<Transform> ();

		foreach(Transform child in floorMeshes){
			if(child.name != "Floor"){
				child.parent = gCells.transform;
			}
		}

		gCells.transform.parent = tFloor;

		//Grab children of gObject and parent them to floor 
		//Due to a Unity Bug, an extra 'for loop' is used to assign to each parent..

		//Loop Group 1
		List<Transform> children = new List<Transform>();

		foreach(Transform child in gObject.transform){ 
			if(child.name != "Floor"){
				children.Add (child);
			}
		}

		foreach (Transform child in children) {
			child.parent = tFloor;
		}
		//EOF Loop Group 1

		//Reset transformation and Clear list
		tFloor.position = Vector3.zero;
		children.Clear();

		//Reparent former children of gObject back to gObject


		//Loop Group 2
		foreach(Transform child in tFloor.transform){ 
			if(child.name != "Cells"){
				children.Add(child);
			}
		}

		foreach (Transform child in children) {
			child.parent = gObject.transform;
		}
		//EOF Loop Group 2
	}

	private void ValidateRoom(GameObject room) {
	
		Transform floorChild = tFloor.GetChild(0);
		Transform[] children = tFloor.GetComponentsInChildren<Transform>();
		MeshCollider meshCollider = null; MeshRenderer meshRenderer = null; MeshFilter meshFilter = null;

	
		if(!room.GetComponent<DRoom>()){
			room.AddComponent<DRoom>();
		}
	
		GameObject tObj = tFloor.gameObject; 
		if (!tObj.GetComponent<RoomTrigger>()){
			tObj.AddComponent<RoomTrigger>();
		}

		if(tWalls){

			bool first = false;
			foreach(Transform child in tWalls){
				child.gameObject.layer = LayerMask.NameToLayer("Obstacle");

				MeshCollider mshCollider = child.GetComponent<MeshCollider>();
				if(!mshCollider)
					mshCollider = child.gameObject.AddComponent<MeshCollider>();

				if(!first) // All walls should be the same height, get information from only one.
					wallHeight = mshCollider.bounds.size.y;
			}
		}

		if(tFloor){

			BoxCollider floorCollider = tFloor.GetComponent<BoxCollider>();

			if(floorCollider)
				DestroyImmediate(floorCollider);

			foreach(Transform child in tFloor){
				if(!child.GetComponent<MeshCollider>()){
					child.gameObject.AddComponent<MeshCollider>();
				}
				//child.gameObject.layer = LayerMask.NameToLayer("Floor");;
			}

			Renderer[] renderers = tFloor.GetComponentsInChildren<Renderer>();
			Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
			
			foreach(Renderer renderer in renderers){
				if(bounds.size == Vector3.zero)
					bounds = renderer.bounds;
				else
					bounds.Encapsulate(renderer.bounds);
			}

			if(!tEntries)
				PlaceEntries(room, bounds.size);

			//if(!tTorches)
			//	PlaceTorches(room);



			float newSizeY = wallHeight - bounds.size.y;

			floorCollider = tFloor.gameObject.AddComponent<BoxCollider>();
			floorCollider.center = Vector3.zero + new Vector3(0, wallHeight/2, 0); //adjust value in order for the collider to trigger properly.
			floorCollider.size = bounds.size + new Vector3(0, newSizeY,0);

			floorCollider.isTrigger = true;

			rcRoom = new RoomClass(room, floorCollider.size);										
		}
	
		//Old Navmesh Code - For future reference
		//Get static flags. 
		//StaticEditorFlags isNavStatic = GameObjectUtility.GetStaticEditorFlags(floorChild.gameObject);
		
		//Meshes must have a MeshCollider and be navigationally static 
		/*
		if(if(!isNavStatic.Equals(null)) )
		{   
			foreach(Transform mesh in tFloor){
				if(!mesh.GetComponent<MeshCollider>()){
					meshCollider = mesh.gameObject.AddComponent<MeshCollider>();
				}
				if(!mesh.GetComponent<MeshFilter>()){
					 meshFilter = mesh.gameObject.AddComponent<MeshFilter>() as MeshFilter;
					//meshFilter.mesh = p_Mesh;
				}
				
				if(!mesh.GetComponent<MeshRenderer>()){
					meshRenderer = mesh.gameObject.AddComponent<MeshRenderer>();
					//meshRenderer.material = p_Material;
				}

				//Old Navmesh Code. Saving for future reference.
				//GameObjectUtility.SetStaticEditorFlags(mesh.gameObject, StaticEditorFlags.NavigationStatic);
			}
		}
		*/ 
	}

	private void PlaceEntries(GameObject room, Vector3 floorSize){

		//TODO PA01

		float latitude = floorSize.z/2;
		float longitude = floorSize.x/2;
		float Y = floorSize.y;

		GameObject Entry = new GameObject("Entries");
		Entry.transform.position = room.transform.position;
		Vector3 entryPos = Entry.transform.position;
		Transform entryTrans = Entry.transform;
		
		entryPos = room.transform.position;
		entryTrans.parent = room.transform;
		
		GridCoord[] d = Direction.Instance.dirs;
		
		for(int i = 0; i < 4; i++){
			GameObject entryChild = new GameObject("Entry" + i);

			float offset = (i == 0 || i == 2) ? latitude : longitude;
			entryChild.transform.position = entryPos + new Vector3(d[i].x * -1 * offset, Y, d[i].y * -1 * offset);
			entryChild.transform.Rotate(0, i * 90, 0);
			entryChild.transform.parent = entryTrans;
		}
	}

	private void PlaceTorches(GameObject room){

		GameObject torches = Instantiate(Resources.Load("Rooms/Room Features/4Torches", typeof(GameObject))) as GameObject;

		tTorches = torches.transform;
		tTorches.name = "Torches";

		tTorches.position = room.transform.position;
		tTorches.parent = room.transform;



	}
	
	private void SaveRoom(GameObject room) {

		string fileName = room.name + ".prefab";
		string path = EditorUtility.SaveFilePanel("Create A Room Asset", "Assets/_Game/Resources/Rooms/Room Layouts/", fileName, "prefab");

		if(path == "")
			return;

		path = FileUtil.GetProjectRelativePath(path);


		Object prefab = PrefabUtility.CreateEmptyPrefab(path);
		AssetDatabase.Refresh();
		PrefabUtility.ReplacePrefab(room, prefab, ReplacePrefabOptions.ConnectToPrefab);


		/////////////////////////
		//Update RoomData file
		/////////////////////////

		//Get Index Value of folder the file was placed in. 

		string[] sPath = path.Split('/');
		string folderName = sPath[sPath.Length-2];
		FolderNames folder = (FolderNames)System.Enum.Parse (typeof(FolderNames), folderName);

		int index = (int)folder;

		Debug.Log("folder name: " + folderName + ", index: " + index);
	
		//rcRoom.room = room;


		//Update RoomData 
		RoomData roomData = DataRetriever.Instance.GetData<RoomData>();
		//roomData.AddTest(index, roomVars);
		
		roomData.Add(index, rcRoom);

		EditorUtility.SetDirty(roomData);
	}


}


