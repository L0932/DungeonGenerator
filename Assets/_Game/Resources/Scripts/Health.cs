using UnityEngine;
using System.Collections;
using plyCommon;
using plyBloxKit;
using plyGame;
using plyRPG;
using DiaQ;

public class Health : MonoBehaviour {

	
	private float startHealth;
	private float healthPerHeart = 2;
	
	private float maxHealth;
	private float currentHealth;
	
	public Texture[] heartImages;
	public GUITexture heartGUI;

	
	private ArrayList hearts = new ArrayList();
	
	// Spacing:
	public float maxHeartsOnRow;
	private float spacingX;
	private float spacingY;
	
	
	void Start () {
		ActorClass c = Player.Instance.actor.actorClass;
		ActorAttribute HP = c.GetAttribute("Health", plyGameObjectIdentifyingType.screenName);

		spacingX = heartGUI.pixelInset.width;
		spacingY = -heartGUI.pixelInset.height;
		startHealth = c.HP.Value;
		maxHealth = c.HP.Value;
		currentHealth = c.HP.Value - c.HP.ConsumableValue;
		AddHearts(startHealth/healthPerHeart);
	}
	
	public void AddHearts(float n) {
		for (int i = 0; i <n; i ++) { 
			Transform newHeart = ((GameObject)Instantiate(heartGUI.gameObject,this.transform.position,Quaternion.identity)).transform; // Creates a new heart
			newHeart.parent = transform;
			
			float y = Mathf.FloorToInt((hearts.Count / maxHeartsOnRow));
			float x = (hearts.Count - y * maxHeartsOnRow);

			newHeart.GetComponent<GUITexture>().pixelInset = new Rect(x * spacingX,y * spacingY,58,58);
			newHeart.GetComponent<GUITexture>().texture = heartImages[0];
			hearts.Add(newHeart);

		}
		maxHealth += n * healthPerHeart;
		currentHealth = maxHealth;
		UpdateHearts();
	}

	
	public void modifyHealth(float amount) {
		currentHealth += amount;
		currentHealth = Mathf.Clamp(currentHealth,0,maxHealth);
		UpdateHearts();
	}

	void UpdateHearts() {
	/*	bool restAreEmpty = false;
		int i =0;
		
		foreach (Transform heart in hearts) {
			
			if (restAreEmpty) {
				heart.guiTexture.texture = heartImages[0]; // heart is empty
			}
			else {
				i += 1; // current iteration
				if (currentHealth >= i * healthPerHeart) {
					heart.guiTexture.texture = heartImages[heartImages.Length-1]; // health of current heart is full
				}
				else {
					float currentHeartHealth = (healthPerHeart - (healthPerHeart * i - currentHealth));
					float healthPerImage = healthPerHeart / heartImages.Length; // how much health is there per image
					float imageIndex = (int)currentHeartHealth / (int)healthPerImage;

					
					if (imageIndex == 0f && currentHeartHealth > 0f) {
						imageIndex = 1f;
					}

					heart.guiTexture.texture = heartImages[imageIndex];
					restAreEmpty = true; 
				}
			}
			
		}*/
	}
}
