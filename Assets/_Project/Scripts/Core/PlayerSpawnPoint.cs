using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Sahneye Player prefabini belirtilen noktada olusturur.
/// Hub ve Gameplay sahnelerinin ikisinde de ayni Player prefabini kullanmak icin kullanilir.
/// Player olusturulduktan sonra Cinemachine kamerasini otomatik olarak Player'a atar.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Oyuncunun prefab'i (Assets/Prefabs klasorunden)")]
    public GameObject playerPrefab;

    [Tooltip("Sadece sahnede baska bir Player yoksa olustur (DontDestroyOnLoad korumasi)")]
    public bool onlyIfNoPlayerExists = true;

    private void Awake()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawnPoint] Player Prefab atanmamis! Inspector'dan prefab surekle.");
            return;
        }

        GameObject player = null;

        // Sahnede zaten bir Player var mi kontrol et
        if (onlyIfNoPlayerExists)
        {
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                // Player zaten var (DontDestroyOnLoad ile geldi)
                // POZİSYONUNU DEĞİŞTİRME — oda sistemi (RoomManager) bunu kendi playerStartPoint'iyle yapacak
                player = existingPlayer;
            }
        }

        // Yeni Player olustur (ilk kez sahneye giriyorsa)
        if (player == null)
        {
            player = Instantiate(playerPrefab, transform.position, Quaternion.identity);
        }

        // Cinemachine kamerasini Player'a ata
        AssignCameraTarget(player.transform);
    }

    /// <summary>
    /// Sahnedeki Cinemachine kamerasini bulur ve Follow/LookAt target olarak Player'i atar.
    /// </summary>
    private void AssignCameraTarget(Transform playerTransform)
    {
        CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Follow = playerTransform;

        }
        else
        {
            Debug.LogWarning("[PlayerSpawnPoint] Sahnede CinemachineCamera bulunamadi!");
        }
    }

    // Editorde spawn noktasini gostermeye yarayan gorsel
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawIcon(transform.position, "sv_icon_dot3_pix16_gizmo", true);
    }
}

