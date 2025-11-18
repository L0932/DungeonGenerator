using UnityEngine;
using System.Collections;

[ExecuteInEditMode]

public class CamShade : MonoBehaviour {

	public Shader shader;
	public GameObject player;
	bool activePlayer = false;
	
	// Render tagged objects with a "heat vision" effect.

	private GameObject shaderCamera;

	void OnPostRender(){

		if (!enabled || !gameObject.active || !shader)
			return;
		
		if (!shaderCamera) {
			shaderCamera = new GameObject("ShaderCamera", typeof(Camera)); 
			shaderCamera.GetComponent<Camera>().enabled = false;
			shaderCamera.hideFlags = HideFlags.HideAndDontSave;
		}
		
		Camera cam = shaderCamera.GetComponent<Camera>();
		cam.CopyFrom (GetComponent<Camera>());
		//cam.backgroundColor = new Color(0,0,0,0);
		//cam.clearFlags = CameraClearFlags.SolidColor;
		cam.RenderWithShader (shader, "RenderType");
	}

	void OnDisable(){
		DestroyImmediate(shaderCamera);
	}

	void Awake(){

		//Camera cam = GameObject.Find ("MiniMap").camera;
		//transform.camera.RenderWithShader (heatVisionShader, null);

		//camera.RenderWithShader (heatVisionShader, null);
		//camera.SetReplacementShader(shader, "RenderType");
		//camera.SetReplacementShader (shader_2, "Floor");
	}

	void Start(){
	}

	void LateUpdate(){

		if (player) {

			Vector3 pos = transform.position;

			transform.position = new Vector3(player.transform.position.x, 
			                                 player.transform.position.y + 100,
											 player.transform.position.z);

		}
	}
}
