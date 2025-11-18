using UnityEngine;

//#if UNITY_EDITOR
using UnityEditor;
//#endif

using System.Collections.Generic;

public class DataRetriever {

	private static DataRetriever _instance;
	private string path = "Assets/_Game/Resources/Scripts/Data/RoomData.asset";

	public static DataRetriever Instance{
		get{
			if (_instance == null)
				_instance = new DataRetriever();

			return _instance;
		}

		private set{
		}
	}

	private DataRetriever(){
	}


	public T GetData<T>() where T : ScriptableObject {

		T asset = (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));

		if (asset != null)
			return asset;

		return CreateDataAsset<T>();
	}


	public T CreateDataAsset<T>() where T : ScriptableObject {
		T dataAsset = (T)ScriptableObject.CreateInstance(typeof(T));
		if (dataAsset != null){
			AssetDatabase.CreateAsset (dataAsset, path);
			AssetDatabase.Refresh();
			AssetDatabase.SaveAssets();
		}
		
		return dataAsset;
	}

	public static void BuildNavMesh(){
		UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
	}

}
