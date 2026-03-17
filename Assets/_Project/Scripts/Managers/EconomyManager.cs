using UnityEngine;

/// <summary>
/// Run icinde kazanilan gecici altini yonetir.
/// Oyuncu oldugunde (Game Over) bu "run altını" SaveManager aracılığıyla kalıcı altına cevirilir.
/// DontDestroyOnLoad Singleton.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Run Economy")]
    [Tooltip("Bu run boyunca kazanilan gecici altin miktari.")]
    public int runGold = 0;
    private bool hasDepositedThisRun = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Dusman oldurmekten, oda temizlemekten veya odul kapidan gelen altin.
    /// </summary>
    public void AddRunGold(int amount)
    {
        runGold += amount;

        // TODO: UI guncelleme (HUD'a altin sayaci eklendikten sonra)
    }

    /// <summary>
    /// Oyuncu oldu veya boss'u yendi. Gecici run altinini kalici altina cevir.
    /// GameOverManager veya boss-win akisindan cagirilir.
    /// </summary>
    public void DepositRunGold()
    {
        if (hasDepositedThisRun)
            return;

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[EconomyManager] SaveManager not found! Cannot deposit gold.");
            return;
        }

        hasDepositedThisRun = true;

        if (runGold > 0)
        {
            SaveManager.Instance.AddGold(runGold);
        }

        // Run istatistiklerini guncelle
        SaveManager.Instance.data.totalRunsPlayed++;

        int currentRoom = 0;
        if (RunManager.Instance != null)
            currentRoom = RunManager.Instance.roomsCleared + 1; // +1: mevcut oda dahil

        if (currentRoom > SaveManager.Instance.data.bestRoomReached)
            SaveManager.Instance.data.bestRoomReached = currentRoom;

        SaveManager.Instance.Save();
    }

    /// <summary>
    /// Yeni run baslarken gecici altini sifirla.
    /// RetryRun veya yeni bir run baslayinca cagirilir.
    /// </summary>
    public void ResetRunGold()
    {
        runGold = 0;
        hasDepositedThisRun = false;
    }
}
