using System.Collections;
using UnityEngine;

public static class SupportPulseVisualUtility
{
    public static void SpawnPulse(Vector3 position, float startRadius, float endRadius, float duration, Color color, int segmentCount = 32, float lineWidth = 0.06f)
    {
        GameObject pulseObject = new GameObject("SupportPulse");
        pulseObject.transform.position = position;
        PulseRunner runner = pulseObject.AddComponent<PulseRunner>();
        runner.Play(startRadius, endRadius, duration, color, segmentCount, lineWidth);
    }

    private class PulseRunner : MonoBehaviour
    {
        private LineRenderer lineRenderer;

        public void Play(float startRadius, float endRadius, float duration, Color color, int segmentCount, float lineWidth)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = Mathf.Max(12, segmentCount);
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.numCapVertices = 4;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.sortingOrder = 98;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineRenderer.material = new Material(shader);

            StartCoroutine(PulseRoutine(startRadius, endRadius, duration, color));
        }

        private IEnumerator PulseRoutine(float startRadius, float endRadius, float duration, Color color)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float radius = Mathf.Lerp(startRadius, endRadius, t);
                Color displayColor = color;
                displayColor.a *= 1f - t;
                lineRenderer.startColor = displayColor;
                lineRenderer.endColor = displayColor;
                RebuildCircle(radius);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void RebuildCircle(float radius)
        {
            int count = lineRenderer.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
