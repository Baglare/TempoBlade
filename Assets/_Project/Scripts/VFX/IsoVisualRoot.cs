using UnityEngine;

[DisallowMultipleComponent]
public class IsoVisualRoot : MonoBehaviour
{
    [Header("Roots")]
    public Transform visualRoot;
    public Transform shadowRoot;
    public Transform vfxRoot;
    public Transform worldUIRoot;

    [Header("Renderers")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer weaponRenderer;

    [Header("Setup")]
    public bool autoResolveOnAwake = true;
    public bool applySortingLayers = true;
    public bool warnWhenMissingVisualRoot = true;

    private bool warnedMissingVisualRoot;

    private void Awake()
    {
        if (autoResolveOnAwake)
            Resolve();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && autoResolveOnAwake)
            Resolve();
    }

    [ContextMenu("Resolve Visual References")]
    public void Resolve()
    {
        visualRoot ??= FindNamedChild("VisualRoot") ?? FindNamedChild("Visuals") ?? FindNamedChild("Visual");

        if (bodyRenderer == null)
            bodyRenderer = visualRoot != null
                ? visualRoot.GetComponentInChildren<SpriteRenderer>(true)
                : GetComponentInChildren<SpriteRenderer>(true);

        if (visualRoot == null && bodyRenderer != null)
            visualRoot = bodyRenderer.transform;

        shadowRoot ??= FindNamedChild("Shadow") ?? FindNamedChild("GroundShadow") ?? FindNamedChild("IsoGroundShadow");
        vfxRoot ??= FindNamedChild("VFXRoot") ?? FindNamedChild("VFX");
        worldUIRoot ??= FindNamedChild("WorldUIRoot") ?? FindNamedChild("WorldUI");

        if (weaponRenderer == null)
        {
            Transform weapon = FindNamedChild("WeaponSprite") ?? FindNamedChild("WeaponVisual");
            if (weapon != null)
                weaponRenderer = weapon.GetComponent<SpriteRenderer>();
        }

        if (visualRoot == null && warnWhenMissingVisualRoot && !warnedMissingVisualRoot)
        {
            warnedMissingVisualRoot = true;
            Debug.LogWarning($"[IsoVisualRoot] {name} icin VisualRoot/Visuals/Visual bulunamadi. Ilk SpriteRenderer fallback olarak kullanilacak.", this);
        }

        if (applySortingLayers)
            ApplySortingLayers();
    }

    public void ApplySortingLayers()
    {
        if (bodyRenderer != null)
            bodyRenderer.sortingLayerName = WorldSortingUtility.ResolveLayerName(WorldSortingLayers.Characters);

        if (weaponRenderer != null)
            weaponRenderer.sortingLayerName = WorldSortingUtility.ResolveLayerName(WorldSortingLayers.CharacterVFX);
    }

    private Transform FindNamedChild(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child != transform && child.name == childName)
                return child;
        }

        return null;
    }
}
