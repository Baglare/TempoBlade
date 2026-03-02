using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance { get; private set; }

    [Tooltip("Hit-stop sirasinda oyun hizi. 0.05 - 0.2 arasi iyi.")]
    [Range(0.01f, 1f)]
    public float hitStopScale = 0.12f;

    [Tooltip("Hit-stop suresi (saniye). 0.04 - 0.09 arasi iyi.")]
    public float hitStopDuration = 0.06f;

    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayHitStop()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HitStopRoutine(hitStopDuration, hitStopScale));
    }

    public void PlayHitStop(float duration, float scale)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HitStopRoutine(duration, scale));
    }

    public void PlayHeavyHitStop()
    {
        // 0.12s donma + %0.05 hiz (Daha uzun ve derin)
        PlayHitStop(0.12f, 0.05f);
    }

    private IEnumerator HitStopRoutine(float duration, float scale)
    {
        Time.timeScale = scale;
        // Realtime: timeScale'dan etkilenmez
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f; // Her zaman saglikli reset icin prevScale yerine 1f (eger boss slowdown aktif degilse)
        routine = null;
    }
}
