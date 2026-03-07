using UnityEngine;

/// <summary>
/// Normal progression eksenlerinden farklı çalışan üst-katman form sistemi.
/// İki kutuplu affinity ile ilerler. Diğer eksenlerin T3 bölgelerini şekillendirir.
/// </summary>
[CreateAssetMenu(menuName = "TempoBlade/Skill Tree/Form Overlay", order = 103)]
public class FormOverlaySO : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Benzersiz form kimliği.")]
    public string formId;

    [Header("Kutuplar")]
    [Tooltip("Pozitif yönün adı.")]
    public string positiveName = "Form-A";
    [Tooltip("Negatif yönün adı.")]
    public string negativeName = "Form-B";

    public Sprite positiveIcon;
    public Sprite negativeIcon;

    [Header("Affinity")]
    [Tooltip("Pozitif/negatif yönde ulaşılabilecek maksimum değer.")]
    [Min(1)]
    public int maxAffinity = 5;

    [Header("T3 Bölge Kapıları")]
    [Tooltip("Form yönüne göre hangi regionTag'lerin erişilebilir olacağını belirleyen kurallar.")]
    public RegionGate[] regionGates;

    /// <summary>
    /// Verilen affinity ve regionTag için ilgili gate'in koşullarını kontrol eder.
    /// Gate yoksa true döner (gate tanımlanmamış = serbestçe erişilebilir).
    /// </summary>
    public bool IsRegionAccessible(string regionTag, int currentAffinity)
    {
        if (regionGates == null || string.IsNullOrEmpty(regionTag))
            return true;

        foreach (var gate in regionGates)
        {
            if (gate.regionTag != regionTag)
                continue;

            // Bu region için bir gate kuralı var — eşik kontrolü yap
            switch (gate.direction)
            {
                case FormDirection.Positive:
                    return currentAffinity >= gate.minimumAffinity;
                case FormDirection.Negative:
                    return currentAffinity <= -gate.minimumAffinity;
                case FormDirection.Either:
                    return Mathf.Abs(currentAffinity) >= gate.minimumAffinity;
            }
        }

        // Bu regionTag için hiç gate tanımlanmamış → serbest
        return true;
    }
}

/// <summary>
/// Bir regionTag'in erişilebilir olması için gereken form affinity eşiği.
/// </summary>
[System.Serializable]
public struct RegionGate
{
    [Tooltip("Hedef T3 bölge etiketi.")]
    public string regionTag;

    [Tooltip("Gereken yön.")]
    public FormDirection direction;

    [Tooltip("Minimum affinity eşiği (pozitif sayı olarak verilmeli).")]
    [Min(1)]
    public int minimumAffinity;
}
