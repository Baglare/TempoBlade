using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private Transform damagePopupPrefab;
    [SerializeField] private GameObject hitParticlePrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // Sadece scripti sil, objeyi silme
            return;
        }
        Instance = this;
    }

    public void Create(Vector3 position, int damageAmount, bool isCriticalHit)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("DamagePopupPrefab is missing in DamagePopupManager!");
            return;
        }

        Transform damagePopupTransform = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
        
        if (damagePopup != null)
        {
            damagePopup.Setup(damageAmount, isCriticalHit);
        }
    }

    public void CreateText(Vector3 position, string text, Color color, float size = 5f)
    {
        if (damagePopupPrefab == null) return;

        Transform damagePopupTransform = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
        
        if (damagePopup != null)
        {
            damagePopup.Setup(text, color, size);
        }
    }

    public void CreateHitParticle(Vector3 position)
    {
        if (hitParticlePrefab == null) return;
        Instantiate(hitParticlePrefab, position, Quaternion.identity);
    }
}
