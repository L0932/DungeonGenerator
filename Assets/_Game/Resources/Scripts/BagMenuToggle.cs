using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using plyCommon;
using plyBloxKit;
using plyGame;
using plyRPG;
using DiaQ;

public class BagMenuToggle : MonoBehaviour {

	public plyRPG_UI rpgUI; 
	public GameObject bagMenu;
	public GameObject charSheet;
	string Name;



	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{

		if (charSheet.activeSelf == true) 
		{
			bagMenu.SetActive (false);
		}
		if (bagMenu.activeSelf == true) 
		{
			charSheet.SetActive (false);
		}
	
	}

}
