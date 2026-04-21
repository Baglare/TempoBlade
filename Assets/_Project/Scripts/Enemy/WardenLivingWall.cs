using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WardenLivingWall : MonoBehaviour
{
    private readonly List<BoxCollider2D> segmentColliders = new List<BoxCollider2D>();
    private readonly List<LineRenderer> segmentLines = new List<LineRenderer>();
    private float expireTime;
    private bool active;

    public bool IsActive => active && Time.time < expireTime;

    public void Activate(Vector2 forward, EliteWardenLivingWallSettings settings)
    {
        EnsureSegments(Mathf.Max(1, settings.gapCount + 1));
        expireTime = Time.time + settings.wallDuration;
        active = true;

        Vector2 normal = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector2.right;
        Vector2 tangent = new Vector2(-normal.y, normal.x);
        float totalGapWidth = settings.projectileGapWidth * settings.gapCount;
        float remainingWidth = Mathf.Max(0.4f, settings.wallWidth - totalGapWidth);
        float segmentWidth = remainingWidth / Mathf.Max(1, settings.gapCount + 1);
        float cursor = -settings.wallWidth * 0.5f;

        for (int i = 0; i < segmentColliders.Count; i++)
        {
            float centerOffset = cursor + segmentWidth * 0.5f;
            cursor += segmentWidth + settings.projectileGapWidth;

            Vector2 center = (Vector2)transform.position + tangent * centerOffset;
            BoxCollider2D collider = segmentColliders[i];
            collider.enabled = true;
            collider.size = new Vector2(segmentWidth, settings.segmentHeight);
            collider.offset = Vector2.zero;
            collider.transform.position = center;
            collider.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);

            LineRenderer line = segmentLines[i];
            line.enabled = true;
            line.startColor = settings.wallColor;
            line.endColor = settings.wallColor;
            float halfW = segmentWidth * 0.5f;
            float halfH = settings.segmentHeight * 0.5f;
            Vector3[] corners =
            {
                center + tangent * -halfW + (Vector2)Vector3.Cross(tangent, Vector3.forward) * halfH,
                center + tangent * halfW + (Vector2)Vector3.Cross(tangent, Vector3.forward) * halfH,
                center + tangent * halfW - (Vector2)Vector3.Cross(tangent, Vector3.forward) * halfH,
                center + tangent * -halfW - (Vector2)Vector3.Cross(tangent, Vector3.forward) * halfH,
                center + tangent * -halfW + (Vector2)Vector3.Cross(tangent, Vector3.forward) * halfH
            };
            line.positionCount = corners.Length;
            for (int j = 0; j < corners.Length; j++)
                line.SetPosition(j, corners[j]);
        }
    }

    public void Deactivate()
    {
        active = false;
        for (int i = 0; i < segmentColliders.Count; i++)
        {
            if (segmentColliders[i] != null)
                segmentColliders[i].enabled = false;
            if (segmentLines[i] != null)
                segmentLines[i].enabled = false;
        }
    }

    private void Update()
    {
        if (active && Time.time >= expireTime)
            Deactivate();
    }

    private void EnsureSegments(int count)
    {
        while (segmentColliders.Count < count)
        {
            GameObject child = new GameObject($"WallSegment_{segmentColliders.Count}");
            child.transform.SetParent(transform, false);
            BoxCollider2D box = child.AddComponent<BoxCollider2D>();
            box.enabled = false;
            segmentColliders.Add(box);

            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);
            line.enabled = false;
            segmentLines.Add(line);
        }
    }
}
