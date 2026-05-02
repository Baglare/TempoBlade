using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Eger TextMeshPro kullaniyorsan, metinler icin
using UnityEngine.UI;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameOverPanel; // Kararan arka plan ve icindeki yazilarin oldugu ana obje

    [Header("UI Text References")]
    public TextMeshProUGUI statsText; // "Ulasilan Oda: 5" yazacak yer

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string hubSceneName = "Scene_Hub";
    public float gameOverScreenDelay = 2f;

    private bool isGameOverTriggered = false;
    private Coroutine gameOverDelayRoutine;

    private void Start()
    {
        // Baslangicta bu paneli gizle ve tetiklenmeyi sifirla
        isGameOverTriggered = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Bug Fix A: Yeni bir sahne yüklendiğinde (veya retry yapıldığında) GameManager 
        // DontDestroyOnLoad olduğu için eski "GameOver" state'inde kalıyordu. 
        // Bu script sahne yüklendiğinde her zaman baştan oluştuğu için state'i kosulsuz Gameplay'e zorluyoruz.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameManager.GameState.Gameplay);
        }
    }

    private void Update()
    {
        // GameManager'in durumu GameOver'a gecerse ve panel henuz acilmamissa ac
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
        {
            if (!isGameOverTriggered)
            {
                isGameOverTriggered = true;
                if (gameOverDelayRoutine != null)
                    StopCoroutine(gameOverDelayRoutine);
                gameOverDelayRoutine = StartCoroutine(ShowGameOverScreenAfterDelay());
            }
        }
    }

    private IEnumerator ShowGameOverScreenAfterDelay()
    {
        float delay = Mathf.Max(0f, gameOverScreenDelay);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        gameOverDelayRoutine = null;
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
        
        if (gameOverPanel == null)
        {
            Debug.LogWarning("[GameOverManager] Canvas is missing/destroyed. Searching for a new one in the scene...");
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                Transform panelTransform = canvas.transform.Find("GameOverPanel");
                if (panelTransform != null)
                {
                    gameOverPanel = panelTransform.gameObject;
                    break;
                }
            }
        }

        if (gameOverPanel != null)
        {
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
        }

        if (statsText == null && gameOverPanel != null)
        {
            Transform textTransform = gameOverPanel.transform.Find("StatsText");
            if (textTransform != null) statsText = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
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
        if (gameOverDelayRoutine != null)
        {
            StopCoroutine(gameOverDelayRoutine);
            gameOverDelayRoutine = null;
        }
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

    /// <summary>
    /// Ana Menuye Don Butonu
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        isGameOverTriggered = false;
        if (gameOverDelayRoutine != null)
        {
            StopCoroutine(gameOverDelayRoutine);
            gameOverDelayRoutine = null;
        }
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
