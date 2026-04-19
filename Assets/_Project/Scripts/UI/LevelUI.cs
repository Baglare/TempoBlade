using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelUI : MonoBehaviour
{
    public TextMeshProUGUI levelText;
    private bool hasHiddenLevelText;

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
        HideLevelText();
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
        HideLevelText();
    }

    private void UpdateLevelText()
    {
        HideLevelText();
    }

    private System.Collections.IEnumerator WaitForLevelManager()
    {
        while (LevelManager.Instance == null)
        {
            yield return null;
        }
        HideLevelText();
    }

    private void HideLevelText()
    {
        if (hasHiddenLevelText)
            return;

        if (levelText == null)
        {
            levelText = GetComponent<TextMeshProUGUI>();
            if (levelText == null)
                levelText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (levelText != null)
        {
            levelText.text = string.Empty;
            levelText.enabled = false;
            hasHiddenLevelText = true;
        }

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }
}
