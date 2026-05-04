using UnityEngine;

public static class CombatPhysicsQueryUtility
{
    public const int DefaultBufferSize = 64;

    public static Collider2D[] EnsureBuffer(Collider2D[] buffer, int minimumSize = DefaultBufferSize)
    {
        int size = Mathf.Max(1, minimumSize);
        if (buffer == null || buffer.Length < size)
            return new Collider2D[size];

        return buffer;
    }

    public static int OverlapCircle(Vector2 center, float radius, LayerMask layerMask, ref Collider2D[] buffer, int minimumSize = DefaultBufferSize)
    {
        buffer = EnsureBuffer(buffer, minimumSize);
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, radius, buffer, layerMask);
        while (hitCount >= buffer.Length)
        {
            System.Array.Resize(ref buffer, buffer.Length * 2);
            hitCount = Physics2D.OverlapCircleNonAlloc(center, radius, buffer, layerMask);
        }

        return hitCount;
    }

    public static int OverlapCircleAllLayers(Vector2 center, float radius, ref Collider2D[] buffer, int minimumSize = DefaultBufferSize)
    {
        return OverlapCircle(center, radius, Physics2D.DefaultRaycastLayers, ref buffer, minimumSize);
    }
}
