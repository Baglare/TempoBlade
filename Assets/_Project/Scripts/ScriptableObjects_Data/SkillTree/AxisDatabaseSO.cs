using UnityEngine;

/// <summary>
/// Tüm eksen, karşıt çift ve form overlay verilerini tek bir yerden sunan üst registry.
/// Projede tek bir AxisDatabaseSO asset'i olmalıdır.
/// </summary>
[CreateAssetMenu(menuName = "TempoBlade/Skill Tree/Axis Database", order = 110)]
public class AxisDatabaseSO : ScriptableObject
{
    [Header("Eksenler")]
    [Tooltip("Projedeki tüm progression eksenleri.")]
    public ProgressionAxisSO[] allAxes;

    [Header("Karşıt Çiftler")]
    [Tooltip("Birbirine zıt çalışan eksen çiftleri.")]
    public OpposingPairSO[] opposingPairs;

    [Header("Form Overlay'leri")]
    [Tooltip("Diğer eksenlerin T3 bölgelerini şekillendiren üst-katman form sistemleri.")]
    public FormOverlaySO[] formOverlays;

    // ═══════════ Utility Metotlar ═══════════

    /// <summary>Verilen ID'ye sahip ekseni döndürür. Bulunamazsa null.</summary>
    public ProgressionAxisSO GetAxisById(string axisId)
    {
        if (allAxes == null || string.IsNullOrEmpty(axisId)) return null;
        foreach (var axis in allAxes)
        {
            if (axis != null && axis.axisId == axisId)
                return axis;
        }
        return null;
    }

    /// <summary>Verilen eksenin karşıtını döndürür. Karşıt çift yoksa null.</summary>
    public ProgressionAxisSO GetOpposingAxis(ProgressionAxisSO axis)
    {
        var pair = GetPairForAxis(axis);
        return pair != null ? pair.GetOpposite(axis) : null;
    }

    /// <summary>Verilen eksenin dahil olduğu karşıt çifti döndürür. Yoksa null.</summary>
    public OpposingPairSO GetPairForAxis(ProgressionAxisSO axis)
    {
        if (opposingPairs == null || axis == null) return null;
        foreach (var pair in opposingPairs)
        {
            if (pair != null && pair.Contains(axis))
                return pair;
        }
        return null;
    }

    /// <summary>Verilen ID'ye sahip form overlay'ı döndürür. Bulunamazsa null.</summary>
    public FormOverlaySO GetFormOverlayById(string formId)
    {
        if (formOverlays == null || string.IsNullOrEmpty(formId)) return null;
        foreach (var overlay in formOverlays)
        {
            if (overlay != null && overlay.formId == formId)
                return overlay;
        }
        return null;
    }

    /// <summary>Verilen node'un hangi eksene ait olduğunu bulur. Bulunamazsa null.</summary>
    public ProgressionAxisSO GetAxisForNode(SkillNodeSO node)
    {
        if (allAxes == null || node == null) return null;
        foreach (var axis in allAxes)
        {
            if (axis == null || axis.nodes == null) continue;
            foreach (var n in axis.nodes)
            {
                if (n == node)
                    return axis;
            }
        }
        return null;
    }
}
