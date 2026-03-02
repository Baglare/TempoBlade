using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float duration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        else
        {
            Debug.LogError($"{gameObject.name}: HitFlash could not find a SpriteRenderer!");
        }
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        // Yontem 1: Materyal Degistirme (Eger materyal atanmissa)
        if (flashMaterial != null)
        {
            spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(duration);
            spriteRenderer.material = originalMaterial;
        }
        // Yontem 2: Renk Degistirme (Daha garanti)
        else
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red; // Kirmizi yanip sonsun ki belli olsun
            yield return new WaitForSeconds(duration);
            spriteRenderer.color = originalColor;
        }

        flashRoutine = null;
    }
}
