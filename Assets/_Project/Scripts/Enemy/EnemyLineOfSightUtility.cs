using UnityEngine;

public static class EnemyLineOfSightUtility
{
    public static bool HasLineOfSight(Vector2 origin, Transform target, LayerMask obstacleMask, Transform ignoreRoot = null)
    {
        if (target == null)
            return false;

        Vector2 targetPos = target.position;
        Vector2 direction = targetPos - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
            return true;

        Vector2 start = origin + direction.normalized * 0.05f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction.normalized, distance, obstacleMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null)
                continue;

            Transform hitTransform = col.transform;
            if (ignoreRoot != null && hitTransform.root == ignoreRoot.root)
                continue;
            if (hitTransform.root == target.root)
                return true;

            return false;
        }

        return true;
    }
}
