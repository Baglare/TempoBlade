using UnityEngine;

public static class EnemyLineOfSightUtility
{
    private static Collider2D[] overlapBuffer = new Collider2D[32];

    public static bool HasLineOfSight(Vector2 origin, Transform target, LayerMask obstacleMask, Transform ignoreRoot = null)
    {
        if (target == null)
            return false;
        return HasLineOfSight(origin, target.position, target.root, ignoreRoot);
    }

    public static bool HasLineOfSight(Vector2 origin, Vector2 targetPoint, Transform targetRoot = null, Transform ignoreRoot = null)
    {
        Vector2 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return true;

        Vector2 start = origin + direction.normalized * 0.05f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction.normalized, distance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (!IsRelevantSolid(col))
                continue;

            Transform hitTransform = col.transform;
            if (ignoreRoot != null && hitTransform.root == ignoreRoot.root)
                continue;
            if (targetRoot != null && hitTransform.root == targetRoot.root)
                return true;

            return false;
        }

        return true;
    }

    public static bool HasProjectileCorridor(Vector2 origin, Vector2 targetPoint, float radius, Transform ignoreRoot = null, Transform targetRoot = null)
    {
        Vector2 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return true;

        Vector2 start = origin + direction.normalized * 0.05f;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(start, radius, direction.normalized, distance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (!IsRelevantSolid(col))
                continue;

            Transform hitTransform = col.transform;
            if (ignoreRoot != null && hitTransform.root == ignoreRoot.root)
                continue;
            if (targetRoot != null && hitTransform.root == targetRoot.root)
                return true;

            return false;
        }

        return true;
    }

    public static bool IsPointNavigable(Vector2 point, float radius, Transform ignoreRoot = null)
    {
        int overlapCount = CombatPhysicsQueryUtility.OverlapCircleAllLayers(point, radius, ref overlapBuffer, 32);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (!IsRelevantSolid(col))
                continue;
            if (ignoreRoot != null && col.transform.root == ignoreRoot.root)
                continue;
            return false;
        }

        return true;
    }

    public static float GetObstacleDensity(Vector2 point, float radius, Transform ignoreRoot = null)
    {
        int overlapCount = CombatPhysicsQueryUtility.OverlapCircleAllLayers(point, radius, ref overlapBuffer, 32);
        float density = 0f;
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D col = overlapBuffer[i];
            if (!IsRelevantSolid(col))
                continue;
            if (ignoreRoot != null && col.transform.root == ignoreRoot.root)
                continue;
            density += 1f;
        }

        return density;
    }

    private static bool IsRelevantSolid(Collider2D col)
    {
        return col != null && col.enabled && !col.isTrigger && col.gameObject.activeInHierarchy;
    }
}
