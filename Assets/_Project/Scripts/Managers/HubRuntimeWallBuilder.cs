using UnityEngine;

public static class HubRuntimeWallBuilder
{
    private const string RootName = "HubRuntimeWalls";

    public static void EnsureWalls(Transform parent, float margin = 1.25f, float thickness = 1.2f)
    {
        if (parent == null)
            return;

        Bounds bounds;
        if (!TryCollectSceneBounds(out bounds))
            return;

        Transform existingRoot = parent.Find(RootName);
        if (existingRoot != null)
            Object.Destroy(existingRoot.gameObject);

        GameObject root = new GameObject(RootName);
        root.transform.SetParent(parent, false);
        root.transform.position = Vector3.zero;

        Vector2 expandedSize = new Vector2(bounds.size.x + margin * 2f, bounds.size.y + margin * 2f);
        Vector2 center = bounds.center;

        CreateWall(root.transform, "TopWall", new Vector2(center.x, center.y + expandedSize.y * 0.5f + thickness * 0.5f), new Vector2(expandedSize.x + thickness * 2f, thickness));
        CreateWall(root.transform, "BottomWall", new Vector2(center.x, center.y - expandedSize.y * 0.5f - thickness * 0.5f), new Vector2(expandedSize.x + thickness * 2f, thickness));
        CreateWall(root.transform, "LeftWall", new Vector2(center.x - expandedSize.x * 0.5f - thickness * 0.5f, center.y), new Vector2(thickness, expandedSize.y));
        CreateWall(root.transform, "RightWall", new Vector2(center.x + expandedSize.x * 0.5f + thickness * 0.5f, center.y), new Vector2(thickness, expandedSize.y));
    }

    private static bool TryCollectSceneBounds(out Bounds bounds)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            if (!ShouldUseRenderer(renderer))
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static bool ShouldUseRenderer(Renderer renderer)
    {
        if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            return false;

        Transform target = renderer.transform;
        if (target.CompareTag("Player") || target.root.CompareTag("Player"))
            return false;

        if (target.GetComponentInParent<Canvas>() != null)
            return false;

        return true;
    }

    private static void CreateWall(Transform parent, string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }
}
