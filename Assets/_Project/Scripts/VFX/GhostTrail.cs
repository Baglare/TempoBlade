using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;
    public float fadeSpeed = 3f;

    public bool isFading = false;

    public void Setup(Sprite sprite, Color ghostColor)
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        
        WorldSortingUtility.ApplySorting(sr, WorldSortingLayers.CharacterVFX, -1);

        color = ghostColor;
        sr.color = color;
        
        fadeSpeed = 8f; // Cok hizli kaybolsun (Cooldown bitince)
    }

    private void Update()
    {
        if (!isFading) return;

        color.a -= fadeSpeed * Time.deltaTime;
        sr.color = color;

        if (color.a <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
