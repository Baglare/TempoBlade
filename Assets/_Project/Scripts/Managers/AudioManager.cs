using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Catalog")]
    [Tooltip("Bos birakilirsa Resources/Audio/DefaultAudioCueCatalog yuklenir.")]
    public AudioCueCatalogSO catalog;

    private const string DefaultCatalogPath = "Audio/DefaultAudioCueCatalog";

    private readonly Dictionary<AudioEventId, AudioCueDefinition> _cueMap = new Dictionary<AudioEventId, AudioCueDefinition>();
    private readonly Dictionary<string, float> _cooldownMap = new Dictionary<string, float>();

    public static AudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        AudioManager existing = FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            Instance = existing;
            existing.InitializeCatalog();
            return existing;
        }

        GameObject go = new GameObject("AudioManager");
        Instance = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
        Instance.InitializeCatalog();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeCatalog();
    }

    private void InitializeCatalog()
    {
        if (catalog == null)
            catalog = Resources.Load<AudioCueCatalogSO>(DefaultCatalogPath);

        _cueMap.Clear();
        if (catalog == null || catalog.cues == null)
            return;

        foreach (AudioCueDefinition cue in catalog.cues)
        {
            if (cue == null || cue.eventId == AudioEventId.None)
                continue;

            _cueMap[cue.eventId] = cue;
        }
    }

    public static bool Play(AudioEventId eventId, GameObject source = null, Vector3? worldPosition = null)
    {
        AudioManager mgr = EnsureInstance();
        return mgr.PlayInternal(eventId, source, worldPosition);
    }

    private bool PlayInternal(AudioEventId eventId, GameObject source, Vector3? worldPosition)
    {
        if (!_cueMap.TryGetValue(eventId, out AudioCueDefinition cue) || cue == null || !cue.HasClip)
            return false;

        string cooldownKey = BuildCooldownKey(eventId, source);
        if (cue.cooldown > 0f &&
            _cooldownMap.TryGetValue(cooldownKey, out float lastPlayTime) &&
            Time.time - lastPlayTime < cue.cooldown)
        {
            return false;
        }

        _cooldownMap[cooldownKey] = Time.time;

        AudioClip clip = cue.GetRandomClip();
        if (clip == null)
            return false;

        if (cue.followTarget && source != null)
        {
            AudioEmitter emitter = AudioEmitter.EnsureFor(source);
            emitter.Play(cue, clip);
            return true;
        }

        Vector3 spawnPos = worldPosition ?? (source != null ? source.transform.position : Vector3.zero);
        AudioEmitter tempEmitter = AudioEmitter.CreateTemporary(spawnPos);
        tempEmitter.Play(cue, clip, destroyAfterPlayback: !cue.loop);
        return true;
    }

    private static string BuildCooldownKey(AudioEventId eventId, GameObject source)
    {
        int sourceId = source != null ? source.GetInstanceID() : 0;
        return $"{eventId}:{sourceId}";
    }
}
