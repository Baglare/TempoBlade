using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class YSortByPosition : MonoBehaviour
{
    [Header("Sorting")]
    public string sortingLayerName = WorldSortingLayers.Characters;
    public bool applySortingLayer = true;
    public int baseOrder = 0;
    public int orderOffset = 0;
    public int unitsPerOrder = 100;
    public bool staticMode = false;
    public bool includeChildRenderers = true;

    [Header("Debug")]
    public bool debugLog = false;

    private SortingGroup sortingGroup;
    private Renderer[] renderers;
    private int lastAppliedOrder = int.MinValue;

    private void Awake()
    {
        ResolveTargets();
    }

    private void OnEnable()
    {
        ResolveTargets();
        ApplySort();
    }

    private void LateUpdate()
    {
        if (!staticMode)
            ApplySort();
    }

    private void OnValidate()
    {
        unitsPerOrder = Mathf.Max(1, unitsPerOrder);
        ResolveTargets();
        ApplySort();
    }

    private void ResolveTargets()
    {
        sortingGroup = GetComponent<SortingGroup>();
        renderers = includeChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
    }

    public void ApplySort()
    {
        int order = CalculateOrder(transform.position.y);
        string layer = WorldSortingUtility.ResolveLayerName(sortingLayerName);

        if (sortingGroup != null)
        {
            if (applySortingLayer)
                sortingGroup.sortingLayerName = layer;
            sortingGroup.sortingOrder = order;
        }
        else if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer target = renderers[i];
                if (target == null)
                    continue;

                if (applySortingLayer)
                    target.sortingLayerName = layer;
                target.sortingOrder = order;
            }
        }

        if (debugLog && order != lastAppliedOrder)
            Debug.Log($"[YSort] {name} y={transform.position.y:F2} order={order} layer={layer}", this);

        lastAppliedOrder = order;
    }

    public int CalculateOrder(float worldY)
    {
        return baseOrder + orderOffset + Mathf.RoundToInt(-worldY * Mathf.Max(1, unitsPerOrder));
    }
}
