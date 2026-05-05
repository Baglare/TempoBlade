using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hub (Guvenli Alan) sahnesinin yonetici scripti.
/// Oyuncu buradan fiziksel objelere yaklasip (HubInteractable) etkilesime girer.
/// Dukkan acikken oyuncunun hareketi kilitlenir.
/// </summary>
public class HubManager : MonoBehaviour
{
    public static HubManager Instance { get; private set; }

    [Header("Scene Settings")]
    [Tooltip("Oyuncuyu gonderecegin ilk gameplay sahnesi")]
    public string gameplaySceneName = "GameScene";

    [Header("UI References")]
    [Tooltip("Dukkan / Yukseltme panelini buraya surekle (ShopUI Canvas)")]
    public GameObject shopPanel;

    [Tooltip("Demirci panelini buraya surekle (BlacksmithUI Canvas)")]
    public GameObject blacksmithPanel;

    private int lastUiToggleFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        ProgressionWalletUI.EnsureInstance();
        HubRuntimeWallBuilder.EnsureWalls(transform);

        // Hub'a girince oyun durumunu guncelle
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Menu);

        // Dukkan panelini baslangicta kapat
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (blacksmithPanel != null)
            blacksmithPanel.SetActive(false);
    }

    // ===================== SAHNE GECISLERI =====================

    /// <summary>
    /// Zindana gonder (HubInteractable -> StartRun tetikler).
    /// </summary>
    public void StartRun()
    {
        // Once dukkan aciksa kapat
        CloseShop();

        if (RunManager.Instance != null)
            RunManager.Instance.ResetRunData();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.ResetRunGold();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeOut(() =>
            {
                SceneManager.LoadScene(gameplaySceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    /// <summary>
    /// Ana menuye don.
    /// </summary>
    public void GoToMainMenu()
    {
        CloseShop();
        SceneManager.LoadScene("MainMenu");
    }

    // ===================== DÜKKAN =====================

    public void OpenShop()
    {
        if (lastUiToggleFrame == Time.frameCount)
            return;

        if (shopPanel != null)
        {
            ShopUI shopUI = shopPanel.GetComponent<ShopUI>();
            if (shopUI != null)
            {
                if (!shopPanel.activeSelf)
                    shopPanel.SetActive(true);
                shopUI.OpenPanel();
            }
            else
                shopPanel.SetActive(true);
        }

        lastUiToggleFrame = Time.frameCount;
    }

    public void CloseShop()
    {
        if (lastUiToggleFrame == Time.frameCount)
            return;

        if (shopPanel != null)
        {
            ShopUI shopUI = shopPanel.GetComponent<ShopUI>();
            if (shopUI != null)
                shopUI.CloseShop();
            else
                shopPanel.SetActive(false);
        }

        lastUiToggleFrame = Time.frameCount;
    }

    public void ToggleShop()
    {
        if (shopPanel == null) return;

        ShopUI shopUI = shopPanel.GetComponent<ShopUI>();
        if (shopUI != null)
        {
            if (shopUI.IsOpen)
                CloseShop();
            else
                OpenShop();

            return;
        }

        if (shopPanel.activeSelf)
            CloseShop();
        else
            OpenShop();
    }

    // ===================== DEMİRCİ =====================

    public void OpenBlacksmith()
    {
        if (lastUiToggleFrame == Time.frameCount)
            return;

        if (blacksmithPanel != null)
        {
            BlacksmithUI blacksmithUI = blacksmithPanel.GetComponent<BlacksmithUI>();
            if (blacksmithUI != null)
            {
                if (!blacksmithPanel.activeSelf)
                    blacksmithPanel.SetActive(true);
                blacksmithUI.OpenPanel();
            }
            else
                blacksmithPanel.SetActive(true);
        }

        lastUiToggleFrame = Time.frameCount;
    }

    public void CloseBlacksmith()
    {
        if (lastUiToggleFrame == Time.frameCount)
            return;

        if (blacksmithPanel != null)
        {
            BlacksmithUI blacksmithUI = blacksmithPanel.GetComponent<BlacksmithUI>();
            if (blacksmithUI != null)
                blacksmithUI.CloseBlacksmith();
            else
                blacksmithPanel.SetActive(false);
        }

        lastUiToggleFrame = Time.frameCount;
    }

    public void ToggleBlacksmith()
    {
        if (blacksmithPanel == null) return;

        BlacksmithUI blacksmithUI = blacksmithPanel.GetComponent<BlacksmithUI>();
        if (blacksmithUI != null)
        {
            if (blacksmithUI.IsOpen)
                CloseBlacksmith();
            else
                OpenBlacksmith();

            return;
        }

        if (blacksmithPanel.activeSelf)
            CloseBlacksmith();
        else
            OpenBlacksmith();
    }

    /// <summary>
    /// Oyuncunun hareketini ac/kapat.
    /// </summary>
    private void SetPlayerMovement(bool enabled)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.canMove = enabled;
        }
    }
}
