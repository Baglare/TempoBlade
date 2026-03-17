using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Eger TextMeshPro kullaniyorsan, metinler icin
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameOverPanel; // Kararan arka plan ve icindeki yazilarin oldugu ana obje

    [Header("UI Text References")]
    public TextMeshProUGUI statsText; // "Ulasilan Oda: 5" yazacak yer

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string hubSceneName = "Hub";

    private bool isGameOverTriggered = false;

    public static GameOverManager EnsureInstance()
    {
        GameOverManager mgr = FindFirstObjectByType<GameOverManager>(FindObjectsInactive.Include);
        if (mgr != null) return mgr;

        if (GameManager.Instance != null)
        {
            mgr = GameManager.Instance.GetComponent<GameOverManager>();
            if (mgr != null) return mgr;
        }

        GameOverManager[] all = Resources.FindObjectsOfTypeAll<GameOverManager>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (!all[i].gameObject.scene.IsValid() || !all[i].gameObject.scene.isLoaded) continue;
            return all[i];
        }
        return null;
    }

    private void Start()
    {
        // Baslangicta bu paneli gizle ve tetiklenmeyi sifirla
        isGameOverTriggered = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Not: Burada state'i zorla Gameplay'e çekmiyoruz.
        // Retry/Menu akışları zaten state'i doğru yerde set ediyor.
    }

    private void Update()
    {
        // GameManager'in durumu GameOver'a gecerse ve panel henuz acilmamissa ac
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
        {
            if (!isGameOverTriggered)
            {
                isGameOverTriggered = true;
                ShowGameOverScreen();
            }
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.GameOver);

        ShowGameOverScreen();
    }

    private void ShowGameOverScreen()
    {
        // Zaman yavas yavas dursun (dramatik etki)
        Time.timeScale = 0f; 

        // --- EKONOMI: Run altinini kalici altina cevir ---
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.DepositRunGold();
        }
        
        if (gameOverPanel == null || !gameOverPanel || !gameOverPanel.scene.IsValid() || !gameOverPanel.scene.isLoaded)
        {
            gameOverPanel = FindGameObjectInLoadedScenes("GameOverPanel");
        }

        if (gameOverPanel == null)
        {
            Debug.LogWarning("[GameOverManager] Canvas is missing/destroyed. Searching for a new one in the scene...");
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < allChildren.Length; i++)
                {
                    if (allChildren[i] != null && allChildren[i].name == "GameOverPanel")
                    {
                        gameOverPanel = allChildren[i].gameObject;
                        break;
                    }
                }
                if (gameOverPanel != null) break;
            }
        }

        if (gameOverPanel != null)
        {
            Canvas parentCanvas = gameOverPanel.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
                parentCanvas.gameObject.SetActive(true);

            gameOverPanel.SetActive(true);

            // BUTTON FIX: Sahneler arası geçişte butonların eski referansları yok olan GameManager'lara işaret eder.
            Button[] buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                string btnName = btn.gameObject.name.ToLower();
                btn.onClick.RemoveAllListeners();

                if (btnName.Contains("retry") || btnName.Contains("restart") || btnName.Contains("tekrar") || btnName.Contains("play"))
                {
                    btn.onClick.AddListener(RetryRun);
                }
                else if (btnName.Contains("menu") || btnName.Contains("home") || btnName.Contains("quit") || btnName.Contains("ana"))
                {
                    btn.onClick.AddListener(GoToMainMenu);
                }
            }
        }
        else
        {
            Debug.LogError("[GameOverManager] GameOverPanel bulunamadi! Sahnede Siyah Panel eksik.");
            Time.timeScale = 1f;
            if (IsSceneInBuildSettings(hubSceneName))
                SceneManager.LoadScene(hubSceneName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if ((statsText == null || !statsText) && gameOverPanel != null)
        {
            Transform textTransform = gameOverPanel.transform.Find("StatsText");
            if (textTransform == null)
            {
                Transform[] children = gameOverPanel.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null && children[i].name == "StatsText")
                    {
                        textTransform = children[i];
                        break;
                    }
                }
            }

            if (textTransform != null)
                statsText = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
        }

        // Istatistikleri goster (oda + kazanilan altin)
        if (statsText != null && RunManager.Instance != null)
        {
            int rooms = RunManager.Instance.roomsCleared;
            int goldEarned = EconomyManager.Instance != null ? EconomyManager.Instance.runGold : 0;
            int totalGold = SaveManager.Instance != null ? SaveManager.Instance.data.totalGold : 0;
            statsText.text = $"Ulaşılan Oda: {rooms + 1}\nKazanılan Altın: {goldEarned}\nToplam Altın: {totalGold}";
        }
    }

    /// <summary>
    /// Yeniden Dene Butonu. RunManager verilerini sifirlayip ilk odayi tekrar yukler.
    /// </summary>
    public void RetryRun()
    {
        Time.timeScale = 1f;

        isGameOverTriggered = false;
        gameOverPanel = null;
        statsText = null;

        if (RunManager.Instance != null)
            RunManager.Instance.ResetRunData();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.ResetRunGold();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);

        // Hub sahnesi Build Settings'te varsa oraya git, yoksa mevcut sahneyi yeniden yukle
        if (IsSceneInBuildSettings(hubSceneName))
        {
            SceneManager.LoadScene(hubSceneName);
        }
        else
        {
            Debug.LogWarning("[GameOverManager] Hub sahnesi Build Settings'te bulunamadi! Mevcut sahne yeniden yukleniyor.");
            if (LevelManager.Instance != null)
                LevelManager.Instance.RestartGame();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>
    /// Bir sahnenin Build Settings'e eklenip eklenmedigini kontrol eder.
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    private static GameObject FindGameObjectInLoadedScenes(string objectName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t == null) continue;
            if (t.name != objectName) continue;
            GameObject go = t.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded) continue;
            return go;
        }
        return null;
    }

    /// <summary>
    /// Ana Menuye Don Butonu
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        isGameOverTriggered = false;
        gameOverPanel = null;
        statsText = null;

        if (RunManager.Instance != null)
            RunManager.Instance.ResetRunData();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.ResetRunGold();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Menu);

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
