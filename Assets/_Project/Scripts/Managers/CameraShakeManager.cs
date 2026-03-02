using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    private CinemachineCamera activeCam;
    private CinemachineBasicMultiChannelPerlin perlinNoise;

    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // Sadece scripti sil, objeyi silme
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.unscaledDeltaTime; // TimeScale=0 olsa bile (Hit Stop vb) sarsinti calissin diye

            if (perlinNoise != null)
            {
                // Intensitiyi zamana gore lineer olarak azaltiyoruz (Yavas yavas durmasi icin)
                perlinNoise.AmplitudeGain = Mathf.Lerp(startingIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
            }

            if (shakeTimer <= 0f)
            {
                StopShake();
            }
        }
    }

    /// <summary>
    /// Kamerayi sarsmak icin kullanilir.
    /// </summary>
    /// <param name="intensity">Sarsintinin gucu (Agir vuruslar icin yuksek deger)</param>
    /// <param name="time">Sarsintinin suresi (Saniye)</param>
    public void ShakeCamera(float intensity, float time)
    {
        // 1) Aktif kamerayi bulalim (Eger referans yoksa veya bozulduysa)
        if (activeCam == null || !activeCam.isActiveAndEnabled)
        {
            activeCam = Object.FindFirstObjectByType<CinemachineCamera>();
            if (activeCam != null)
            {
                perlinNoise = activeCam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            }
        }

        // 2) Perlin Noise eklentisi yoksa veya hala kamera bulamadiysak cik (Hata verdirmektense iptal et)
        if (activeCam == null || perlinNoise == null)
            return;

        // 3) Eger su an halihazirda eskisinden DAHA GUCLU bir shake geliyorsa ustune yaz
        // Eger daha zayif bir shake geldiyse, mevcut guclu sarsiyi bozma
        if (intensity >= perlinNoise.AmplitudeGain)
        {
            perlinNoise.AmplitudeGain = intensity;
            startingIntensity = intensity;
            shakeTimer = time;
            shakeTimerTotal = time;
        }
    }

    private void StopShake()
    {
        if (perlinNoise != null)
        {
            perlinNoise.AmplitudeGain = 0f;
        }
        shakeTimer = 0f;
    }
}
