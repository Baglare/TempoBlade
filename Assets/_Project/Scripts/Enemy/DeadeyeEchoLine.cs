using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DeadeyeEchoLine : MonoBehaviour
{
    private Vector2 startPoint;
    private Vector2 endPoint;
    private float thickness;
    private float expireTime;
    private float armTime;
    private bool triggered;
    private bool playerHasLeftLine;
    private Action onTriggered;
    private LineRenderer lineRenderer;

    public bool IsActive => !triggered && Time.time < expireTime;

    public void Configure(Vector2 start, Vector2 end, float duration, float lineThickness, Color color, Action callback)
    {
        startPoint = start;
        endPoint = end;
        thickness = lineThickness;
        expireTime = Time.time + duration;
        armTime = Time.time + 0.12f;
        onTriggered = callback;
        playerHasLeftLine = false;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
        lineRenderer.widthMultiplier = Mathf.Max(0.04f, lineThickness);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void Update()
    {
        if (Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        if (triggered)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        float distance = DistancePointToSegment(player.transform.position, startPoint, endPoint);
        if (distance > thickness * 1.2f)
            playerHasLeftLine = true;

        if (Time.time < armTime || !playerHasLeftLine || distance > thickness)
            return;

        triggered = true;
        onTriggered?.Invoke();
        Destroy(gameObject);
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float length = ab.sqrMagnitude;
        if (length <= 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / length);
        return Vector2.Distance(point, a + ab * t);
    }
}
