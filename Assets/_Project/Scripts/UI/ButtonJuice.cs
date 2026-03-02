using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Settings")]
    [Tooltip("Fare ustune geldiginde butonun buyume orani")]
    public float hoverScaleMultiplier = 1.1f;
    [Tooltip("Buyume ve kuculme hizi")]
    public float animationSpeed = 15f;

    [Header("Click Settings")]
    [Tooltip("Tiklarken butonun kuculme orani")]
    public float clickScaleMultiplier = 0.95f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        // Baslangic boyutunu hafizaya al
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnDisable()
    {
        // Obje kapatilip acildiginda boyutu bozulmasin diye resetle
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // Smooth bir sekilde hedef boyuta ilerle
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Fare ustune geldi -> Büyüt
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Fare ustunden gitti -> Eski haline don
        targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Butona tiklandi -> Biraz küçült (Tok hissetirir)
        targetScale = originalScale * clickScaleMultiplier;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Tiklama birakildi -> Yeniden hover boyutuna gec
        // Eger fare hala ustundeyse hover devam etmeli
        targetScale = originalScale * hoverScaleMultiplier;
    }
}
