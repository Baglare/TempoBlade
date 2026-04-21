using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TrapperTetherLink : MonoBehaviour
{
    private TrapArea trapA;
    private TrapArea trapB;
    private LineRenderer lineRenderer;
    private float expireTime;
    private float miniBurstDamage;
    private float miniBurstRadius;
    private float slowMultiplier;
    private float slowDuration;
    private float touchCooldown;
    private float lastTouchTime;
    private Color tetherColor = Color.white;

    public void Configure(TrapArea first, TrapArea second, EliteTrapperTetherSettings settings)
    {
        trapA = first;
        trapB = second;
        expireTime = Time.time + settings.tetherLifetime;
        miniBurstDamage = settings.miniBurstDamage;
        miniBurstRadius = settings.miniBurstRadius;
        slowMultiplier = settings.slowMultiplier;
        slowDuration = settings.slowDuration;
        touchCooldown = settings.touchCooldown;
        tetherColor = settings.tetherColor;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = 0.06f;
        lineRenderer.numCapVertices = 2;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);
        lineRenderer.startColor = tetherColor;
        lineRenderer.endColor = tetherColor;
    }

    private void Update()
    {
        if (trapA == null || trapB == null || Time.time >= expireTime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 a = trapA.transform.position;
        Vector3 b = trapB.transform.position;
        lineRenderer.SetPosition(0, a);
        lineRenderer.SetPosition(1, b);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || Time.time < lastTouchTime + touchCooldown)
            return;

        Vector2 playerPos = player.transform.position;
        float distance = DistancePointToSegment(playerPos, a, b);
        if (distance > 0.24f)
            return;

        lastTouchTime = Time.time;
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsInvulnerable)
            return;

        player.GetComponent<IDamageable>()?.TakeDamage(miniBurstDamage);
        StartCoroutine(ApplySlow(playerController));
        SupportPulseVisualUtility.SpawnPulse(playerPos, 0.15f, miniBurstRadius, 0.18f, tetherColor);
    }

    private IEnumerator ApplySlow(PlayerController playerController)
    {
        if (playerController == null)
            yield break;

        playerController.speedMultiplier *= slowMultiplier;
        yield return new WaitForSeconds(slowDuration);
        playerController.speedMultiplier /= slowMultiplier;
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
