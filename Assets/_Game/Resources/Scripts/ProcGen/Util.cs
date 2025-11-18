using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using UnityEditor;


public static class Util {
	
	public static T[] GetAtPath<T> (string path) {
		ArrayList al = new ArrayList();
		string fullPath = Application.dataPath + "/" + path;

		string[] fileEntries = Directory.GetFiles(fullPath);
		
		foreach(string fileName in fileEntries)
		{
			int assetPathIndex = fileName.IndexOf("Assets");
			string localPath = fileName.Substring(assetPathIndex);
			
			Object t = UnityEditor.AssetDatabase.LoadAssetAtPath(localPath, typeof(T));
			
			if(t != null)
				al.Add (t);
		}
		
		T[] result = new T[al.Count];
		for(int i = 0; i < al.Count; i++)
			result[i] = (T)al[i];
		
		return result;
	}


	public static Transform GetChildWithTag(Transform t, string tag){


		foreach(Transform child in t)
		{
			if(child.gameObject.tag == tag)
				return child;
		}

		return null;
	}

	public static Vector3 GetFloorSize(Transform t){

		return Util.GetChildWithTag(t.transform, "RoomFloor").GetComponent<Renderer>().bounds.size;
	}

	public static void Assert(bool condition) {
		if(!condition){
			throw new IOException();
		}
	}
}
