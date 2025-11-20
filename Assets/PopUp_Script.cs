using UnityEngine;
using System.Collections;

public class PopUp_Script : MonoBehaviour {

	GameObject PopUp;
	float Timer;
	bool TimerActive;
	void Start () 
	{
		TimerActive = false;
		Timer = .7f;
		PopUp = transform.Find("PopUp").gameObject;
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
		if (TimerActive == true) 
		{
			Timer -= Time.deltaTime;
			Debug.Log (Timer);
		}

		if (Timer <= 0) 
		{
			PopUp.SetActive (true);
			TimerActive = false;
		}
	}


	public void ShowPopUp()
	{
		TimerActive = true;
	}

	public void DisablePopUp()
	{
		TimerActive = false;
		Timer = .7f;
		PopUp.SetActive (false);

	}
}
