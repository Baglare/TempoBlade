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
        float wallDuration = Mathf.Max(6.5f, settings.wallDuration);
        float radius = Mathf.Max(1.6f, settings.arcRadius);
        float configuredArc = settings.arcDegrees > 0f ? settings.arcDegrees : 120f;
        float effectiveArcDegrees = Mathf.Clamp(configuredArc * 0.7f, 72f, 96f);
        int segmentCount = Mathf.Max(8, Mathf.RoundToInt(effectiveArcDegrees / 8f));
        EnsureSegments(segmentCount);
        expireTime = Time.time + wallDuration;
        active = true;

        Vector2 normal = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector2.right;
        float centerAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
        float angleStart = centerAngle - effectiveArcDegrees * 0.5f;
        float step = effectiveArcDegrees / Mathf.Max(1, segmentCount - 1);
        float visualWidth = Mathf.Max(0.06f, settings.lineWidth * 0.5f);

        for (int i = 0; i < segmentColliders.Count; i++)
        {
            BoxCollider2D collider = segmentColliders[i];
            LineRenderer line = segmentLines[i];
            if (i >= segmentCount)
            {
                collider.enabled = false;
                line.enabled = false;
                continue;
            }

            float angle = angleStart + step * i;
            Vector2 radial = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            Vector2 center = (Vector2)transform.position + radial * radius;
            Vector2 tangent = new Vector2(-radial.y, radial.x).normalized;
            float segmentArcLength = 2f * radius * Mathf.Tan(Mathf.Deg2Rad * step * 0.5f) * 0.88f;

            collider.enabled = true;
            collider.size = new Vector2(Mathf.Max(0.18f, segmentArcLength), Mathf.Max(0.62f, settings.segmentThickness * 0.75f));
            collider.offset = Vector2.zero;
            collider.transform.position = center;
            collider.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);

            line.enabled = true;
            line.startColor = settings.wallColor;
            line.endColor = settings.wallColor;
            line.startWidth = visualWidth;
            line.endWidth = visualWidth;
            Vector3 start = center - tangent * (segmentArcLength * 0.5f);
            Vector3 end = center + tangent * (segmentArcLength * 0.5f);
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
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
            child.AddComponent<WardenLivingWallSegment>();
            box.enabled = false;
            segmentColliders.Add(box);

            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.startWidth = 0.2f;
            line.endWidth = 0.2f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.material = new Material(shader);
            line.enabled = false;
            segmentLines.Add(line);
        }
    }
}
