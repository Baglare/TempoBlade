using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aktif node'lardan derlenen, gameplay kodunun tükettiği salt-okunur yapı.
/// Numeric modifier, boolean flag, feature unlock, runtime tag ve region progress taşır.
/// Gameplay sistemleri raw node listesine değil, bu yapıya bakar.
/// </summary>
public class PlayerBuild
{
    private readonly Dictionary<string, float> _flatBonuses = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _percentBonuses = new Dictionary<string, float>();
    private readonly HashSet<string> _activeFlags = new HashSet<string>();
    private readonly HashSet<string> _unlockedFeatures = new HashSet<string>();
    private readonly HashSet<string> _runtimeTags = new HashSet<string>();
    private readonly Dictionary<string, int> _regionProgress = new Dictionary<string, int>();

    // ═══════════ Derleme (Sadece AxisProgressionManager tarafından çağrılır) ═══════════

    /// <summary>Tüm verileri temizler. Yeniden derleme öncesinde çağrılır.</summary>
    public void Clear()
    {
        _flatBonuses.Clear();
        _percentBonuses.Clear();
        _activeFlags.Clear();
        _unlockedFeatures.Clear();
        _runtimeTags.Clear();
        _regionProgress.Clear();
    }

    /// <summary>Bir node'un tüm effect'lerini build'e uygular.</summary>
    public void ApplyNodeEffects(SkillNodeSO node)
    {
        if (node == null || node.effects == null) return;

        // Region progress sayacına ekle
        if (!string.IsNullOrEmpty(node.regionTag))
        {
            if (!_regionProgress.ContainsKey(node.regionTag))
                _regionProgress[node.regionTag] = 0;
            _regionProgress[node.regionTag]++;
        }

        foreach (var effect in node.effects)
        {
            if (string.IsNullOrEmpty(effect.key)) continue;

            EffectKeyRegistry.WarnIfUnknown(effect.key, $"(Node: '{node.displayName}')");

            switch (effect.type)
            {
                case EffectType.StatModifier:
                    ApplyStatModifier(effect);
                    break;
                case EffectType.Flag:
                    _activeFlags.Add(effect.key);
                    break;
                case EffectType.FeatureUnlock:
                    _unlockedFeatures.Add(effect.key);
                    break;
                case EffectType.RuntimeTag:
                    _runtimeTags.Add(effect.key);
                    break;
            }
        }
    }

    private void ApplyStatModifier(NodeEffect effect)
    {
        switch (effect.modifierOp)
        {
            case ModifierOp.Flat:
                if (!_flatBonuses.ContainsKey(effect.key))
                    _flatBonuses[effect.key] = 0f;
                _flatBonuses[effect.key] += effect.numericValue;
                break;
            case ModifierOp.Percent:
                if (!_percentBonuses.ContainsKey(effect.key))
                    _percentBonuses[effect.key] = 0f;
                _percentBonuses[effect.key] += effect.numericValue;
                break;
        }
    }

    // ═══════════ Okuma API'si (Gameplay kodu bunları kullanır) ═══════════

    /// <summary>Verilen stat key için toplam flat bonus.</summary>
    public float GetFlatBonus(string key)
    {
        return _flatBonuses.TryGetValue(key, out float val) ? val : 0f;
    }

    /// <summary>Verilen stat key için toplam percent bonus (ör: 0.3 = %30).</summary>
    public float GetPercentBonus(string key)
    {
        return _percentBonuses.TryGetValue(key, out float val) ? val : 0f;
    }

    /// <summary>Toplam çarpan: 1 + percent bonus. Gameplay'de base * GetTotalMultiplier olarak kullanılır.</summary>
    public float GetTotalMultiplier(string key)
    {
        return 1f + GetPercentBonus(key);
    }

    /// <summary>Belirtilen flag aktif mi? (ör: "parryHeal")</summary>
    public bool HasFlag(string key)
    {
        return !string.IsNullOrEmpty(key) && _activeFlags.Contains(key);
    }

    /// <summary>Belirtilen feature açılmış mı? (ör: "doubleJump")</summary>
    public bool HasFeature(string key)
    {
        return !string.IsNullOrEmpty(key) && _unlockedFeatures.Contains(key);
    }

    /// <summary>Belirtilen runtime tag mevcut mu?</summary>
    public bool HasTag(string key)
    {
        return !string.IsNullOrEmpty(key) && _runtimeTags.Contains(key);
    }

    /// <summary>Belirtilen regionTag'te kaç node açılmış?</summary>
    public int GetRegionProgress(string regionTag)
    {
        return _regionProgress.TryGetValue(regionTag, out int val) ? val : 0;
    }

    // ═══════════ Debug ═══════════

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== PlayerBuild ===");

        sb.AppendLine("-- Flat Bonuses --");
        foreach (var kv in _flatBonuses) sb.AppendLine($"  {kv.Key}: +{kv.Value}");

        sb.AppendLine("-- Percent Bonuses --");
        foreach (var kv in _percentBonuses) sb.AppendLine($"  {kv.Key}: +{kv.Value * 100f}%");

        sb.AppendLine("-- Flags --");
        foreach (var f in _activeFlags) sb.AppendLine($"  {f}");

        sb.AppendLine("-- Features --");
        foreach (var f in _unlockedFeatures) sb.AppendLine($"  {f}");

        sb.AppendLine("-- Tags --");
        foreach (var t in _runtimeTags) sb.AppendLine($"  {t}");

        sb.AppendLine("-- Region Progress --");
        foreach (var kv in _regionProgress) sb.AppendLine($"  {kv.Key}: {kv.Value} nodes");

        return sb.ToString();
    }
}
