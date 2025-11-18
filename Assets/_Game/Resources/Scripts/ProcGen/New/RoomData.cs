using UnityEngine;
using System;
//using UnityEditor;
using System.Collections.Generic;

[Serializable]
public class RoomData : ScriptableObject {
	

	[Serializable]
	public class RoomClassList{

		public List<RoomClass> list;
		
		public RoomClassList(){
			list = new List<RoomClass>();
			list.Add(new RoomClass());
		}

		public int Count(){
			return list.Count;
		}

		public void Add(RoomClass obj){
			list.Add(obj);
		}

		public string NameAt(int index){
			return list[index].name;
		}

		public void ReplaceAtWith(int index, RoomClass obj){
			list[index] = obj;
		}


	}
	
	public List<GameObject> doorTypes, torchTypes, trapTypes;
	public List<RoomClassList> data;
	//public GameObject dungeon;
	

	public RoomData(){
		if(data == null) {
			data = new List<RoomClassList>();
			data.Add (new RoomClassList());
		}
	}
	
	public void Add(int index, RoomClass newRoom){

		if(data.Count == 0){ 
			data.Add (new RoomClassList());
		}

		int num = 0;
		try{

			num = data[index].Count();
		}
		catch(ArgumentOutOfRangeException){
			int dif = index - num;
			if(dif < 5){ //Hardcoded max differences
				for(int i = 0; i < dif; i++){
					data.Add(new RoomClassList());
				}
			}
		}

		for(int i = 0; i < data[index].Count(); i++){

			if(newRoom.name == data[index].NameAt(i)){
				data[index].ReplaceAtWith(i, newRoom); // Already on the list, return.
				return;
			}
			if(data[index].NameAt(i) == "Empty"){
				data[index].ReplaceAtWith(i, newRoom);
				return; // Replace the empty instance, return.
			}
		}

		data[index].Add(newRoom);
	}

	public RoomClass GetRoomAt(int idx_1, int idx_2){
		return data[idx_1].list[idx_2];
	}

	public int GetRoomCountAt(int index){
		return data[index].Count(); 
	}
}
