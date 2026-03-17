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
    public string gameplaySceneName = "Gameplay";

    [Header("UI References")]
    [Tooltip("Dukkan / Yukseltme panelini buraya surekle (ShopUI Canvas)")]
    public GameObject shopPanel;

    [Tooltip("Demirci panelini buraya surekle (BlacksmithUI Canvas)")]
    public GameObject blacksmithPanel;

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

        // Hub'a girince oyun durumunu guncelle
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Menu);

        // Dukkan panelini baslangicta kapat
        if (shopPanel != null)
            shopPanel.SetActive(false);
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
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            SetPlayerMovement(false);
            HubInteractable.HideAllPrompts(); // Tum promptlari gizle
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
            SetPlayerMovement(true);
            HubInteractable.ShowAllPrompts(); // Promptlari geri ac
        }
    }

    public void ToggleShop()
    {
        if (shopPanel == null) return;

        if (shopPanel.activeSelf)
            CloseShop();
        else
            OpenShop();
    }

    // ===================== DEMİRCİ =====================

    public void OpenBlacksmith()
    {
        if (blacksmithPanel != null)
        {
            blacksmithPanel.SetActive(true);
            SetPlayerMovement(false);
            HubInteractable.HideAllPrompts();
        }
    }

    public void CloseBlacksmith()
    {
        if (blacksmithPanel != null)
        {
            blacksmithPanel.SetActive(false);
            SetPlayerMovement(true);
            HubInteractable.ShowAllPrompts();
        }
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
