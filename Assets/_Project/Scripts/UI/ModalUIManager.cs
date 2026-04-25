using UnityEngine;

/// <summary>
/// Tracks large modal UIs so gameplay pause/input gating can use one source of truth.
/// This is intentionally small: it only owns modal state and previous game state restore.
/// </summary>
public class ModalUIManager : MonoBehaviour
{
    public static ModalUIManager Instance
    {
        get
        {
            if (_instance == null)
                EnsureInstance();

            return _instance;
        }
    }

    public static bool HasOpenModal => _instance != null && _instance.hasOpenModal;
    public static string CurrentModalId => _instance != null ? _instance.currentModalId : string.Empty;

    private static ModalUIManager _instance;

    private bool hasOpenModal;
    private string currentModalId = string.Empty;
    private GameObject currentPanelRoot;
    private GameManager.GameState previousGameState = GameManager.GameState.Gameplay;
    private float previousTimeScale = 1f;

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        _instance = FindFirstObjectByType<ModalUIManager>();
        if (_instance != null)
            return;

        GameObject go = new GameObject("ModalUIManager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ModalUIManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryOpenModal(string modalId, GameObject panelRoot)
    {
        if (string.IsNullOrWhiteSpace(modalId) || panelRoot == null)
            return false;

        if (hasOpenModal)
        {
            return currentModalId == modalId && currentPanelRoot == panelRoot;
        }

        hasOpenModal = true;
        currentModalId = modalId;
        currentPanelRoot = panelRoot;
        previousTimeScale = Time.timeScale;

        if (GameManager.Instance != null)
        {
            previousGameState = GameManager.Instance.CurrentState;
            GameManager.Instance.SetState(GameManager.GameState.Paused);
        }
        else
        {
            Time.timeScale = 0f;
        }

        return true;
    }

    public void CloseModal(string modalId)
    {
        if (!hasOpenModal || currentModalId != modalId)
            return;

        hasOpenModal = false;
        currentModalId = string.Empty;
        currentPanelRoot = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(previousGameState);
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }
    }
}
