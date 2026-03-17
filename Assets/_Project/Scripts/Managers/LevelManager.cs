using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int currentLevelIndex = 1;
    public string gameplaySceneName = "Gameplay";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {

    }

    public void LoadNextLevel()
    {
        currentLevelIndex++;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void RestartGame()
    {
        currentLevelIndex = 1;
        SceneManager.LoadScene(gameplaySceneName);
    }
}
