using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerTempoDisruptionController : MonoBehaviour
{
    private readonly List<TempoStaticZone> activeZones = new List<TempoStaticZone>();

    public static PlayerTempoDisruptionController EnsureFor(GameObject player)
    {
        if (player == null)
            return null;

        PlayerTempoDisruptionController controller = player.GetComponent<PlayerTempoDisruptionController>();
        if (controller == null)
            controller = player.AddComponent<PlayerTempoDisruptionController>();

        return controller;
    }

    private void OnDisable()
    {
        activeZones.Clear();
        if (TempoManager.Instance != null)
            TempoManager.Instance.SetSupportZoneTempoMultipliers(1f, 1f);
    }

    public void RegisterZone(TempoStaticZone zone)
    {
        if (zone == null || activeZones.Contains(zone))
            return;

        activeZones.Add(zone);
        Recalculate();
    }

    public void UnregisterZone(TempoStaticZone zone)
    {
        if (zone == null)
            return;

        activeZones.Remove(zone);
        Recalculate();
    }

    private void Update()
    {
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            if (activeZones[i] == null || !activeZones[i].isActiveAndEnabled)
                activeZones.RemoveAt(i);
        }

        Recalculate();
    }

    private void Recalculate()
    {
        float gainMultiplier = 1f;
        float decayMultiplier = 1f;

        for (int i = 0; i < activeZones.Count; i++)
        {
            TempoStaticZone zone = activeZones[i];
            if (zone == null)
                continue;

            gainMultiplier = Mathf.Min(gainMultiplier, zone.tempoGainMultiplier);
            decayMultiplier = Mathf.Max(decayMultiplier, zone.tempoDecayMultiplier);
        }

        if (TempoManager.Instance != null)
            TempoManager.Instance.SetSupportZoneTempoMultipliers(gainMultiplier, decayMultiplier);
    }
}
