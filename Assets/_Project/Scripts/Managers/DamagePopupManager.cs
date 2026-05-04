using System.Collections.Generic;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [SerializeField] private Transform damagePopupPrefab;
    [SerializeField] private GameObject hitParticlePrefab;
    [SerializeField] private int maxPooledDamagePopups = 32;

    private readonly Queue<DamagePopup> popupPool = new Queue<DamagePopup>();

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

        DamagePopup damagePopup = GetDamagePopup(position);
        
        if (damagePopup != null)
        {
            damagePopup.SetOwner(this);
            damagePopup.Setup(damageAmount, isCriticalHit);
        }
    }

    public void CreateText(Vector3 position, string text, Color color, float size = 5f)
    {
        if (damagePopupPrefab == null) return;

        DamagePopup damagePopup = GetDamagePopup(position);
        
        if (damagePopup != null)
        {
            damagePopup.SetOwner(this);
            damagePopup.Setup(text, color, size);
        }
    }

    public void CreateHitParticle(Vector3 position)
    {
        if (hitParticlePrefab == null) return;
        Instantiate(hitParticlePrefab, position, Quaternion.identity);
    }

    public void Recycle(DamagePopup popup)
    {
        if (popup == null)
            return;

        if (popupPool.Count >= Mathf.Max(0, maxPooledDamagePopups))
        {
            Destroy(popup.gameObject);
            return;
        }

        popup.gameObject.SetActive(false);
        popup.transform.SetParent(transform, false);
        popupPool.Enqueue(popup);
    }

    private DamagePopup GetDamagePopup(Vector3 position)
    {
        DamagePopup popup = null;
        while (popupPool.Count > 0 && popup == null)
            popup = popupPool.Dequeue();

        if (popup != null)
        {
            Transform popupTransform = popup.transform;
            popupTransform.SetParent(null, true);
            popupTransform.SetPositionAndRotation(position, Quaternion.identity);
            popup.gameObject.SetActive(true);
            return popup;
        }

        Transform damagePopupTransform = Instantiate(damagePopupPrefab, position, Quaternion.identity);
        popup = damagePopupTransform.GetComponent<DamagePopup>();
        if (popup == null)
            Destroy(damagePopupTransform.gameObject);

        return popup;
    }
}
