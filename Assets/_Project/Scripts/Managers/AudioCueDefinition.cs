using System;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class AudioCueDefinition
{
    public AudioEventId eventId = AudioEventId.None;
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float pitchRandomRange = 0.05f;
    [Range(0f, 0.5f)] public float volumeRandomRange = 0f;
    [Range(0f, 1f)] public float spatialBlend = 0f;
    public float minDistance = 1f;
    public float maxDistance = 14f;
    public float cooldown = 0f;
    public bool loop = false;
    public bool followTarget = false;
    public AudioMixerGroup outputGroup;

    public bool HasClip => clips != null && clips.Length > 0;

    public AudioClip GetRandomClip()
    {
        if (!HasClip)
            return null;

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }
}
