using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int currentLevelIndex = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // Sadece scripti sil, objeyi silme
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

        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartGame()
    {
        currentLevelIndex = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
