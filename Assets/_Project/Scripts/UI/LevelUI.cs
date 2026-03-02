using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelUI : MonoBehaviour
{
    public TextMeshProUGUI levelText;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateLevelText(); // Enable olunca da guncelle
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLevelText();
    }

    private void Start()
    {
        if (levelText == null)
        {
            levelText = GetComponent<TextMeshProUGUI>();
            if (levelText == null)
            {
                // Child objelerde ara
                levelText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        UpdateLevelText();
    }

    private void UpdateLevelText()
    {
        if (LevelManager.Instance == null)
        {

            StartCoroutine(WaitForLevelManager());
            return;
        }

        if (levelText == null)
        {

             return;
        }

        levelText.text = "LEVEL " + LevelManager.Instance.currentLevelIndex;
    }

    private System.Collections.IEnumerator WaitForLevelManager()
    {
        while (LevelManager.Instance == null)
        {
            yield return null;
        }
        UpdateLevelText();
    }
}
