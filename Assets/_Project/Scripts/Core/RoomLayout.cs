using UnityEngine;

public class RoomLayout : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Düşmanların doğacağı noktalar.")]
    public Transform[] enemySpawnPoints;

    [Header("Fiziksel Kapılar")]
    [Tooltip("Oda prefab'ına yerleştirilmiş RewardDoor bileşenleri.")]
    public RewardDoor[] rewardDoors;
    
    [Header("Player Start")]
    [Tooltip("Oyuncunun odaya girdiğinde başlayacağı nokta (Opsiyonel).")]
    public Transform playerStartPoint;
}
