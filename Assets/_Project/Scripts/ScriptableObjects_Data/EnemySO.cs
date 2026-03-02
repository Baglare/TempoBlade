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

    [Header("AI Behavior")]
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    public float attackCooldown = 2f;
}
