using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,
        Gameplay,
        Paused,
        GameOver
    }

    public GameState CurrentState { get; private set; } = GameState.Gameplay;

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
                Time.timeScale = 1f;
                break;
        }
    }
}
