using UnityEngine;

public struct AttackPresentationContext
{
    public Vector2 origin;
    public Vector2 direction;
    public float range;
    public float arcAngle;
    public bool isAttacking;
    public bool isParrying;
    public bool isPerfectWindow;
}

public class AttackVFXPresenter : MonoBehaviour
{
    [Header("Prefab Hooks")]
    public GameObject attackVfxPrefab;
    public GameObject parryVfxPrefab;
    public GameObject perfectParryVfxPrefab;

    [Header("Sorting")]
    public string sortingLayerName = WorldSortingLayers.CharacterVFX;
    public int sortingOrder = 25;

    [Header("Spawn")]
    public float minSpawnInterval = 0.08f;
    public bool parentSpawnedVfxToPresenter = false;

    private float lastSpawnTime = -999f;
    private int lastState;

    public void UpdatePresentation(AttackPresentationContext context)
    {
        int state = ResolveState(context);
        if (state == 0)
        {
            lastState = 0;
            return;
        }

        bool canSpawn = state != lastState || Time.time - lastSpawnTime >= Mathf.Max(0.01f, minSpawnInterval);
        lastState = state;
        if (!canSpawn)
            return;

        GameObject prefab = ResolvePrefab(state);
        if (prefab == null)
            return;

        Vector2 direction = context.direction.sqrMagnitude > 0.001f ? context.direction.normalized : Vector2.right;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 position = context.origin + direction * Mathf.Max(0f, context.range * 0.5f);
        Transform parent = parentSpawnedVfxToPresenter ? transform : null;

        GameObject instance = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, angle), parent);
        ApplySorting(instance);
        lastSpawnTime = Time.time;
    }

    private int ResolveState(AttackPresentationContext context)
    {
        if (context.isPerfectWindow)
            return 3;
        if (context.isParrying)
            return 2;
        if (context.isAttacking)
            return 1;

        return 0;
    }

    private GameObject ResolvePrefab(int state)
    {
        return state switch
        {
            3 => perfectParryVfxPrefab != null ? perfectParryVfxPrefab : parryVfxPrefab,
            2 => parryVfxPrefab,
            1 => attackVfxPrefab,
            _ => null
        };
    }

    private void ApplySorting(GameObject instance)
    {
        if (instance == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            WorldSortingUtility.ApplySorting(renderers[i], sortingLayerName, sortingOrder);
    }
}
