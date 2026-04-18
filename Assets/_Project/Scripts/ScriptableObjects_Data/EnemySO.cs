using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "TempoBlade/Enemy")]
public class EnemySO : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public GameObject prefab;
    
    [Header("Combat Stats")]
    public float maxHealth = 100f;
    public float damage = 10f;
    public float moveSpeed = 3.5f;

    [Header("Economy")]
    [Tooltip("Bu dusman oldugunde oyuncuya verilecek altin miktari.")]
    public int goldDrop = 5;

    [Header("Progression")]
    [Tooltip("Kapaliysa bu dusman XP/affinity sayaçlarinda kullanilmaz. Training dummy gibi hedefler icin kapat.")]
    public bool countsForProgression = true;

    [Header("Warden")]
    [Tooltip("Warden bu dusmani koruma adayi olarak ne kadar oncelikli gorsun?")]
    public WardenProtectPriority wardenProtectPriority = WardenProtectPriority.None;

    [Header("AI Behavior")]
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
}

public enum WardenProtectPriority
{
    None,
    RangedSupport
}
