using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null || !_instance)
                _instance = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            return _instance;
        }
        private set => _instance = value;
    }

    public enum GameState
    {
        Menu,
        Gameplay,
        Paused,
        GameOver
    }

    public GameState CurrentState { get; private set; } = GameState.Gameplay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _instance = null;
    }

    public static GameManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        return FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        
        switch (CurrentState)
        {
            case GameState.Gameplay:
                Time.timeScale = 1f;
                // Cursor.lockState = CursorLockMode.Locked;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                // Trigger Game Over UI
                Time.timeScale = 0.5f; // Slow motion finish?
                break;
        }
    }
}
