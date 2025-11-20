using DiaQ;
using plyBloxKit;
using plyCommon;
using plyGame;
using plyRPG;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using UIImage = UnityEngine.UI.Image;

public class Health : MonoBehaviour
{
    private float startHealth;
    private float healthPerHeart = 2f;

    private float maxHealth;
    private float currentHealth;

    // Use Sprite instead of Texture for UI.Image
    public Sprite[] heartImages;
    public UIImage heartGUI;     // This is your heart prefab in the Canvas

    private readonly List<UIImage> hearts = new List<UIImage>();

    // Spacing:
    public float maxHeartsOnRow = 10f;
    private float spacingX;
    private float spacingY;

    void Start()
    {
        ActorClass c = Player.Instance.actor.actorClass;
        ActorAttribute HP = c.GetAttribute("Health", plyGameObjectIdentifyingType.screenName);

        // Use RectTransform instead of pixelInset
        RectTransform rt = heartGUI.rectTransform;
        spacingX = rt.rect.width;
        spacingY = rt.rect.height;

        startHealth = c.HP.Value;
        maxHealth = c.HP.Value;
        currentHealth = c.HP.Value - c.HP.ConsumableValue;

        AddHearts(startHealth / healthPerHeart);
    }

    public void AddHearts(float n)
    {
        for (int i = 0; i < n; i++)
        {
            // Instantiate a new Image under this object (which should be under a Canvas)
            UIImage newHeart = Instantiate(heartGUI, transform);
            RectTransform rt = newHeart.rectTransform;

            float y = Mathf.FloorToInt(hearts.Count / maxHeartsOnRow);
            float x = hearts.Count - y * maxHeartsOnRow;

            // Position using anchoredPosition (y negative to go down)
            rt.anchoredPosition = new Vector2(x * spacingX, -y * spacingY);

            // Optional: force the heart size if needed
            rt.sizeDelta = new Vector2(58f, 58f);

            // Start as empty heart (index 0)
            if (heartImages != null && heartImages.Length > 0)
            {
                newHeart.sprite = heartImages[0];
            }

            hearts.Add(newHeart);
        }

        maxHealth += n * healthPerHeart;
        currentHealth = maxHealth;
        UpdateHearts();
    }

    public void modifyHealth(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHearts();
    }

    void UpdateHearts()
    {
        if (hearts.Count == 0 || heartImages == null || heartImages.Length == 0)
            return;

        bool restAreEmpty = false;
        int i = 0;

        foreach (UIImage heart in hearts)
        {
            if (restAreEmpty)
            {
                // heart is empty
                heart.sprite = heartImages[0];
            }
            else
            {
                i += 1; // current iteration (1-based)
                if (currentHealth >= i * healthPerHeart)
                {
                    // current heart is full
                    heart.sprite = heartImages[heartImages.Length - 1];
                }
                else
                {
                    float currentHeartHealth = (healthPerHeart - (healthPerHeart * i - currentHealth));
                    float healthPerImage = healthPerHeart / heartImages.Length;
                    float rawIndex = currentHeartHealth / healthPerImage;
                    int imageIndex = Mathf.FloorToInt(rawIndex);

                    if (imageIndex == 0 && currentHeartHealth > 0f)
                        imageIndex = 1;

                    imageIndex = Mathf.Clamp(imageIndex, 0, heartImages.Length - 1);
                    heart.sprite = heartImages[imageIndex];

                    restAreEmpty = true;
                }
            }
        }
    }
}
