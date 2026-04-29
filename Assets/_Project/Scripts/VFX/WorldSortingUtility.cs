using UnityEngine;

public static class WorldSortingLayers
{
    public const string Floor = "Floor";
    public const string GroundVFX = "GroundVFX";
    public const string PropsBack = "PropsBack";
    public const string Default = "Default";
    public const string Characters = "Characters";
    public const string CharacterVFX = "CharacterVFX";
    public const string PropsFront = "PropsFront";
    public const string Projectiles = "Projectiles";
    public const string WorldUI = "WorldUI";
    public const string ScreenUI = "ScreenUI";
}

public static class WorldSortingUtility
{
    public static string ResolveLayerName(string requestedLayer)
    {
        if (string.IsNullOrWhiteSpace(requestedLayer))
            return WorldSortingLayers.Default;

        int layerId = SortingLayer.NameToID(requestedLayer);
        return SortingLayer.IsValid(layerId) ? requestedLayer : WorldSortingLayers.Default;
    }

    public static void ApplySorting(Renderer renderer, string sortingLayerName, int sortingOrder)
    {
        if (renderer == null)
            return;

        renderer.sortingLayerName = ResolveLayerName(sortingLayerName);
        renderer.sortingOrder = sortingOrder;
    }
}
