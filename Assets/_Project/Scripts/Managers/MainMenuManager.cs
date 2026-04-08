using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Navigation Settings")]
    [Tooltip("Hub sahnesinin adi (oyuncu buraya yonlendirilir)")]
    public string hubSceneName = "Scene_Hub";

    [Tooltip("Eger Hub henuz yoksa dogrudan gameplay sahnesine gonder (fallback)")]
    public string firstGameplaySceneName = "GameScene";

    [Header("UI Panels")]
    public GameObject settingsPanel;
    public GameObject howToPlayPanel;

    private void Start()
    {
        // Menudeyken oyun zamaninin normal aktigindan emin ol
        Time.timeScale = 1f;
        
        // Eger ayarlari acik unutursak kapatarak basla
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);

        // Eger GameManager varsa durumunu Menu olarak guncelle
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Menu)
        {
            GameManager.Instance.SetState(GameManager.GameState.Menu);
        }
    }

    /// <summary>
    /// "Play" veya "Start" butonuna tıklandığında çalışır.
    /// </summary>
    public void StartGame()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.ResetRunData();
        }

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.ResetRunGold();
        }

        // Hub sahnesi varsa oraya git, yoksa dogrudan gameplay'e
        string targetScene = hubSceneName;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FadeOut(() =>
            {
                SceneManager.LoadScene(targetScene);
            });
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    /// <summary>
    /// "Settings" veya "Ayarlar" butonuna tıklandığında çalışır.
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("MainMenuManager: Settings Panel is not assigned in the Inspector.");
            return;
        }
        
        // Eger panel zaten aciksa kapat, kapaliysa ac
        bool isActive = settingsPanel.activeSelf;
        settingsPanel.SetActive(!isActive);
    }
    
    /// <summary>
    /// Settings panelini dogrudan kapatmak icin kullanilir (Close butonu)
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// "Nasıl Oynanır" butonuna tıklandığında çalışır.
    /// </summary>
    public void ToggleHowToPlay()
    {
        if (howToPlayPanel == null)
        {
            Debug.LogWarning("MainMenuManager: How To Play Panel is not assigned in the Inspector.");
            return;
        }
        
        // Eger panel zaten aciksa kapat, kapaliysa ac
        bool isActive = howToPlayPanel.activeSelf;
        howToPlayPanel.SetActive(!isActive);
    }
    
    /// <summary>
    /// Nasıl Oynanır panelini dogrudan kapatmak icin kullanilir ("X" veya "Kapat" butonu)
    /// </summary>
    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    /// <summary>
    /// "Quit" butonuna tıklandığında çalışır.
    /// </summary>
    public void QuitGame()
    {
        
        #if UNITY_EDITOR
            // Editor'de test ederken calisir
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Build alinmis gercek oyunda calisir
            Application.Quit();
        #endif
    }
}
