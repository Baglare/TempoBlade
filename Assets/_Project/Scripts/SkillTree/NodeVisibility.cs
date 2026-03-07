using UnityEngine;

/// <summary>
/// Node'un Hidden → Visible geçiş koşullarını tanımlar.
/// </summary>
[System.Serializable]
public struct NodeVisibility
{
    [Tooltip("Görünürlük kuralı.")]
    public VisibilityRule rule;

    [Tooltip("RequireFormThreshold kuralı için: form overlay ID'si.")]
    public string formOverlayId;

    [Tooltip("RequireFormThreshold kuralı için: gereken yön (Positive/Negative).")]
    public FormDirection requiredDirection;

    [Tooltip("RequireFormThreshold kuralı için: minimum affinity eşiği.")]
    public int formThreshold;

    [Tooltip("RequireAnyUnlockedInRegion kuralı için: hedef region tag.")]
    public string regionTag;

    /// <summary>
    /// Basit "her zaman görünür" visibility döndürür.
    /// </summary>
    public static NodeVisibility AlwaysVisible => new NodeVisibility { rule = VisibilityRule.Always };
}

/// <summary>
/// Node'un görünür olma kuralı.
/// </summary>
public enum VisibilityRule
{
    /// <summary>Her zaman görünür (varsayılan, T1-T2 node'lar için).</summary>
    Always,
    /// <summary>Belirli bir form overlay affinity eşiğine ulaşılması gerekir.</summary>
    RequireFormThreshold,
    /// <summary>Belirtilen region tag'inde en az bir node açılmış olması gerekir.</summary>
    RequireAnyUnlockedInRegion,
    /// <summary>İleride özel mantık için genişletilebilir slot.</summary>
    Custom
}
