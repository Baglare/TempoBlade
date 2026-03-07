using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tek bir ilerleme ekseni. Karşıt çift, bağımsız eksen veya uzmanlık ailesi olabilir.
/// </summary>
[CreateAssetMenu(menuName = "TempoBlade/Skill Tree/Progression Axis", order = 101)]
public class ProgressionAxisSO : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Benzersiz eksen kimliği.")]
    public string axisId;

    [Tooltip("Oyuncuya gösterilen ad.")]
    public string displayName;

    public Sprite icon;
    public Color axisColor = Color.white;

    [Header("Node'lar")]
    [Tooltip("Bu eksene ait tüm node'lar.")]
    public SkillNodeSO[] nodes;

    // ═══════════ Utility Property'ler ═══════════

    /// <summary>Tier 1 node'ları döndürür.</summary>
    public List<SkillNodeSO> Tier1Nodes => GetNodesByTier(1);

    /// <summary>Tier 2 node'ları döndürür.</summary>
    public List<SkillNodeSO> Tier2Nodes => GetNodesByTier(2);

    /// <summary>Tier 3 node'ları döndürür.</summary>
    public List<SkillNodeSO> Tier3Nodes => GetNodesByTier(3);

    /// <summary>isCommitmentNode == true olan node'ları döndürür.</summary>
    public List<SkillNodeSO> CommitmentNodes
    {
        get
        {
            var result = new List<SkillNodeSO>();
            if (nodes == null) return result;
            foreach (var node in nodes)
            {
                if (node != null && node.isCommitmentNode)
                    result.Add(node);
            }
            return result;
        }
    }

    /// <summary>Belirli bir tier'daki node'ları döndürür.</summary>
    public List<SkillNodeSO> GetNodesByTier(int tier)
    {
        var result = new List<SkillNodeSO>();
        if (nodes == null) return result;
        foreach (var node in nodes)
        {
            if (node != null && node.tier == tier)
                result.Add(node);
        }
        return result;
    }

    /// <summary>Belirli bir regionTag'e sahip node'ları döndürür.</summary>
    public List<SkillNodeSO> GetNodesByRegion(string tag)
    {
        var result = new List<SkillNodeSO>();
        if (nodes == null || string.IsNullOrEmpty(tag)) return result;
        foreach (var node in nodes)
        {
            if (node != null && node.regionTag == tag)
                result.Add(node);
        }
        return result;
    }
}
