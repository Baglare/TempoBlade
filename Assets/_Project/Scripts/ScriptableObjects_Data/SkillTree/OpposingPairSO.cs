using UnityEngine;

/// <summary>
/// İki karşıt eksenin commitment ilişkisi.
/// Kilit tetikleyicisi tier numarasına değil, isCommitmentNode bayrağına bağlıdır.
/// Bir eksendeki commitment node açıldığında, karşı eksenin commitment tier'ı ve üzeri kilitlenir.
/// </summary>
[CreateAssetMenu(menuName = "TempoBlade/Skill Tree/Opposing Pair", order = 102)]
public class OpposingPairSO : ScriptableObject
{
    [Header("Karşıt Eksenler")]
    public ProgressionAxisSO axisA;
    public ProgressionAxisSO axisB;

    /// <summary>
    /// Verilen eksenin karşıtını döndürür. Eşleşme yoksa null.
    /// </summary>
    public ProgressionAxisSO GetOpposite(ProgressionAxisSO axis)
    {
        if (axis == axisA) return axisB;
        if (axis == axisB) return axisA;
        return null;
    }

    /// <summary>
    /// Verilen eksen bu çiftin parçası mı?
    /// </summary>
    public bool Contains(ProgressionAxisSO axis)
    {
        return axis == axisA || axis == axisB;
    }
}
