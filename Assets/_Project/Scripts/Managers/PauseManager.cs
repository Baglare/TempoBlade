using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("Eger DontDestroyOnLoad uzerindeyse bu referanslar sahne degisince null olabilir. Asagida otomatik bulunuyor.")]
    public string pauseMenuPanelName = "PausePanel";
    public string settingsPanelName  = "SettingsPanel";

    private GameObject pauseMenuPanel;
    private GameObject settingsPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Eger bu obje zaten DontDestroyOnLoad ise (GameManager objesi gibi) tekrar cagirmaya gerek yok
        // Eger bagimsiz objeyse (Hub'daki gibi) DontDestroyOnLoad yap
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Sahne degisince paneller yeniden olusur — aktif/pasif fark etmeksizin adlarini kullanarak bul
        pauseMenuPanel = FindIncludingInactive(pauseMenuPanelName);
        settingsPanel  = FindIncludingInactive(settingsPanelName);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel  != null) settingsPanel.SetActive(false);

        // Buton referanslari eski (yok edilmis) PauseManager'a baglidir.
        // Her sahne yuklenisinde butonlari mevcut (hayatta kalan) instance'a yeniden bagla.
        RebindPauseButtons();

        string pp = pauseMenuPanel != null ? pauseMenuPanel.name : "NOT FOUND";
        string sp = settingsPanel   != null ? settingsPanel.name  : "NOT FOUND";
    }

    private void RebindPauseButtons()
    {
        // --- PausePanel butonlari ---
        if (pauseMenuPanel != null)
        {
            UnityEngine.UI.Button[] buttons = pauseMenuPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                btn.onClick.RemoveAllListeners();
                string btnName = btn.gameObject.name; // Buyuk/kucuk harf duyarli degil: ToLower ile karsilastir

                switch (btnName.ToLower())
                {
                    case "resumebutton":
                        btn.onClick.AddListener(ResumeGame);
                        break;
                    case "settingsbutton":
                        btn.onClick.AddListener(OpenSettings);
                        break;
                    case "menubutton":
                        btn.onClick.AddListener(QuitToMainMenu);
                        break;
                }
            }
        }

        // --- SettingsPanel butonu (CloseButton) ---
        if (settingsPanel != null)
        {
            UnityEngine.UI.Button[] settingsBtns = settingsPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in settingsBtns)
            {
                if (btn.gameObject.name.ToLower() == "closebutton")
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(CloseSettings);
                }
            }
        }
    }

    private void Start()
    {
        // Ilk sahne icin (OnSceneLoaded oncesi fallback)
        if (pauseMenuPanel == null) pauseMenuPanel = FindIncludingInactive(pauseMenuPanelName);
        if (settingsPanel  == null) settingsPanel  = FindIncludingInactive(settingsPanelName);

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel  != null) settingsPanel.SetActive(false);
    }

    // Aktif ve pasif (SetActive=false) objeleri de bulan yardimci metot
    private GameObject FindIncludingInactive(string objectName)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            // Sadece yuklu sahnedeki objeleri al (asset, prefab vb. degil)
            if (go.scene.isLoaded && go.name == objectName)
                return go;
        }
        return null;
    }

    private void Update()
    {
        // ESC veya P tusuna basilirsa kontrol et
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
        {
            string state = GameManager.Instance != null ? GameManager.Instance.CurrentState.ToString() : "NO GAMEMANAGER";
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // Eger baska bir panel aciksa (Shop, Blacksmith, StatsPanel) pause'a girme
        if (IsAnyOverlayPanelOpen()) return;

        bool isPaused = false;

        if (GameManager.Instance != null)
            isPaused = GameManager.Instance.CurrentState == GameManager.GameState.Paused;
        else
            isPaused = Time.timeScale == 0f; // GameManager yoksa TimeScale ile kontrol

        if (isPaused)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                ResumeGame();
            }
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Shop, Blacksmith veya StatsPanel gibi overlay panellerden biri acik mi?
    /// </summary>
    private bool IsAnyOverlayPanelOpen()
    {
        if (HubManager.Instance != null)
        {
            if (HubManager.Instance.shopPanel != null && HubManager.Instance.shopPanel.activeSelf)
                return true;
            if (HubManager.Instance.blacksmithPanel != null && HubManager.Instance.blacksmithPanel.activeSelf)
                return true;
        }

        StatsPanel sp = FindFirstObjectByType<StatsPanel>();
        if (sp != null && sp.statsPanel != null && sp.statsPanel.activeSelf)
            return true;

        return false;
    }

    public void PauseGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Paused);
        else
            Time.timeScale = 0f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);
        else
            Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // Ana menuye donerken zamani duzelt
        
        if (RunManager.Instance != null)
            RunManager.Instance.ResetRunData();

        // MainMenu scene indeksini veya adini direkt girebiliriz (Burayi proje yapina gore MainMenu olarak degistir)
        SceneManager.LoadScene("MainMenu");
    }
}
