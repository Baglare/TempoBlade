using UnityEngine;

[DisallowMultipleComponent]
public class AudioEmitter : MonoBehaviour
{
    private AudioSource _source;

    public static AudioEmitter EnsureFor(GameObject owner)
    {
        if (owner == null)
            return null;

        AudioEmitter emitter = owner.GetComponent<AudioEmitter>();
        if (emitter == null)
            emitter = owner.AddComponent<AudioEmitter>();

        emitter.EnsureSource();
        return emitter;
    }

    public static AudioEmitter CreateTemporary(Vector3 worldPosition)
    {
        GameObject go = new GameObject("TempAudioEmitter");
        go.transform.position = worldPosition;
        AudioEmitter emitter = go.AddComponent<AudioEmitter>();
        emitter.EnsureSource();
        return emitter;
    }

    private void Awake()
    {
        EnsureSource();
    }

    private void EnsureSource()
    {
        if (_source != null)
            return;

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
    }

    public void Play(AudioCueDefinition cue, AudioClip clip, bool destroyAfterPlayback = false)
    {
        if (cue == null || clip == null)
            return;

        EnsureSource();

        _source.clip = clip;
        _source.loop = cue.loop;
        _source.outputAudioMixerGroup = cue.outputGroup;
        _source.spatialBlend = cue.spatialBlend;
        _source.minDistance = cue.minDistance;
        _source.maxDistance = cue.maxDistance;
        _source.volume = Mathf.Clamp01(cue.volume + Random.Range(-cue.volumeRandomRange, cue.volumeRandomRange));
        _source.pitch = Mathf.Clamp(cue.pitch + Random.Range(-cue.pitchRandomRange, cue.pitchRandomRange), -3f, 3f);
        _source.Play();

        if (destroyAfterPlayback)
            Destroy(gameObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(_source.pitch)) + 0.1f);
    }
}
