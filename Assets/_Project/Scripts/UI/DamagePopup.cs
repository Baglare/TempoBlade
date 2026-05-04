using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    private DamagePopupManager owner;

    private const float DISAPPEAR_TIMER_MAX = 1f;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void SetOwner(DamagePopupManager popupOwner)
    {
        owner = popupOwner;
    }

    public void Setup(int damageAmount, bool isCriticalHit)
    {
        textMesh.SetText(damageAmount.ToString());
        transform.localScale = Vector3.one;
        
        if (!isCriticalHit)
        {
            // Normal hit
            textMesh.fontSize = 4;
            textColor = new Color(1f, 1f, 1f, 1f); // White
        }
        else
        {
            // Critical hit
            textMesh.fontSize = 6;
            textColor = new Color(1f, 0.2f, 0.2f, 1f); // Reddish
        }
        
        textMesh.color = textColor;
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // Yukari dogru firlatma hareketi
        moveVector = new Vector3(0.5f, 1f) * 5f; 
    }

    // Overload for Custom Text (like "PARRY!", "BLOCK!")
    public void Setup(string text, Color color, float size = 5f)
    {
        textMesh.SetText(text);
        transform.localScale = Vector3.one;
        textMesh.fontSize = size;
        textColor = color;
        textMesh.color = textColor;
        
        disappearTimer = DISAPPEAR_TIMER_MAX;
        
        // Biraz daha yavas veya farkli firlayabilir
        moveVector = new Vector3(0f, 1f) * 4f; 
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        if (disappearTimer > DISAPPEAR_TIMER_MAX * 0.5f)
        {
            // Ilk yari: Buyume (Pop up)
            float increaseScaleAmount = 1f;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            // Ikinci yari: Kuculme (Fade out)
            float decreaseScaleAmount = 1f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // Start disappearing
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            
            if (textColor.a < 0)
            {
                if (owner != null)
                    owner.Recycle(this);
                else
                    Destroy(gameObject);
            }
        }
    }
}
