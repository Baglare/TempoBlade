using UnityEngine;

public class RoomLayout : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Düşmanların doğacağı noktalar.")]
    public Transform[] enemySpawnPoints;
    
    [Header("Portal Points")]
    [Tooltip("Ödül portallarının çıkacağı noktalar (Maksimum 3 önerilir).")]
    public Transform[] portalSpawnPoints;
    
    [Header("Player Start")]
    [Tooltip("Oyuncunun odaya girdiğinde başlayacağı nokta (Opsiyonel).")]
    public Transform playerStartPoint;
}
