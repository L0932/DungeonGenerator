using UnityEngine;

public class Test : MonoBehaviour {

	//public delegate void EnemyDeath();
	//public static event EnemyDeath OnEnemyKilled;

	private DRoom droomScript;


	void Start(){

		/*
		Bounds combinedBounds = new Bounds (Vector3.zero, Vector3.zero);

		//BoxCollider[] boxCols = GetComponentsInChildren<BoxCollider> ();
		Renderer[] renderers = GetComponentsInChildren<Renderer> ();
		
		foreach (Renderer renderer in renderers) {
			if(combinedBounds.size == Vector3.zero)
				combinedBounds = new Bounds(transform.position, Vector3.one);//transform.position;//renderer.bounds;
			else
				combinedBounds.Encapsulate(renderer.bounds);//renderer.bounds);
		}

		BoxCollider finalBC = gameObject.AddComponent<BoxCollider> ();
		finalBC.center = combinedBounds.center - transform.position;//(max + min) / 2;//transform.position;//combinedBounds.center;
		finalBC.size = combinedBounds.size;*/

	}



	void OnDestroy(){

		if (droomScript)
			droomScript.EnemyKilled();
		//if (OnEnemyKilled != null)
			//OnEnemyKilled ();


		//Debug.Log ("Destroyed!!!!");
	}

	public void SetRoomScript(DRoom sc){
		droomScript = sc;
	}
	/*
	public Health health;

	void OnGUI() {
	
		if (GUI.Button(new Rect(Screen.width / 1.5f,Screen.height/4,100,25),"Regain Health")) {
			health.modifyHealth(10);
		}
		if (GUI.Button(new Rect(Screen.width / 1.5f,Screen.height/4 + Screen.height/10,100,25),"Take Damage")) {
			health.modifyHealth(-1);
		}
		if (GUI.Button(new Rect(Screen.width / 1.5f,Screen.height/4 + Screen.height/10 * 2,100,25),"Add Heart")) {
			health.AddHearts(1);
		}
		
	}*/
}
