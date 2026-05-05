using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;
    public float fadeSpeed = 3f;
    public float maxVisibleLifetime = 0.18f;

    public bool isFading = false;
    private float visibleTimer;

    public void Setup(Sprite sprite, Color ghostColor)
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        
        WorldSortingUtility.ApplySorting(sr, WorldSortingLayers.CharacterVFX, -1);

        color = ghostColor;
        sr.color = color;
        
        fadeSpeed = 8f; // Cok hizli kaybolsun (Cooldown bitince)
        visibleTimer = 0f;
        isFading = false;
    }

    private void Update()
    {
        if (!isFading)
        {
            visibleTimer += Time.deltaTime;
            if (visibleTimer >= maxVisibleLifetime)
                isFading = true;
            else
                return;
        }

        color.a -= fadeSpeed * Time.deltaTime;
        sr.color = color;

        if (color.a <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
