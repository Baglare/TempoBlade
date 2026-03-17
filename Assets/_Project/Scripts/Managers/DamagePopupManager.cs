using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    private static DamagePopupManager _instance;
    public static DamagePopupManager Instance
    {
        get
        {
            if (_instance == null || !_instance)
                _instance = FindFirstObjectByType<DamagePopupManager>(FindObjectsInactive.Include);
            return _instance;
        }
        private set => _instance = value;
    }

    [SerializeField] private Transform damagePopupPrefab;
    [SerializeField] private GameObject hitParticlePrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _instance = null;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            bool thisHasRefs = damagePopupPrefab != null || hitParticlePrefab != null;
            bool instanceMissingRefs = _instance.damagePopupPrefab == null && _instance.hitParticlePrefab == null;

            if (instanceMissingRefs && thisHasRefs)
            {
                Destroy(_instance.gameObject);
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (damagePopupPrefab == null || hitParticlePrefab == null)
        {
            Debug.LogWarning("[DamagePopupManager] Prefab referanslari eksik. damagePopupPrefab/hitParticlePrefab kontrol et.");
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
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
