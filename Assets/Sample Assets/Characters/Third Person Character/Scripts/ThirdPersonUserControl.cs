using UnityEngine;

[RequireComponent(typeof(ThirdPersonCharacter))]
public class ThirdPersonUserControl : MonoBehaviour {
	
	public bool walkByDefault = false;                  // toggle for walking state
	public bool lookInCameraDirection = true;           // should the character be looking in the same direction that the camera is facing
	
	private Vector3 lookPos;                            // The position that the character should be looking towards
    private ThirdPersonCharacter character;             // A reference to the ThirdPersonCharacter on the object
	private Transform cam;                              // A reference to the main camera in the scenes transform
	private Vector3 camForward;							// The current forward direction of the camera
	private Vector3 move;								// the world-relative desired move direction, calculated from the camForward and user input.

	// Use this for initialization
	void Start ()
	{
        // get the transform of the main camera
		if (Camera.main != null)
		{
			cam = Camera.main.transform;
		} else {
			Debug.LogWarning("Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.");
			// we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
		}

        // get the third person character ( this should never be null due to require component )
		character = GetComponent<ThirdPersonCharacter>();
	}

	// Fixed update is called in sync with physics
	void FixedUpdate ()
	{
		// read inputs
		bool crouch = Input.GetKey(KeyCode.C);

	
	}


}
