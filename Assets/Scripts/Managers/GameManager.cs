using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGamePaused { get; private set; }

    private float _prevTimeScale = 1f;

    private void Awake()
    {
        Instance = this;
        InitializeGameState();
    }

    private void InitializeGameState()
    {
        IsGamePaused = false;
        _prevTimeScale = 1f;
    }

    public void PauseGame()
    {
        if (IsGamePaused == true)
        {
            return;
        }

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsGamePaused = true;
    }

    public void ResumeGame()
    {
        if (IsGamePaused == false)
        {
            return;
        }

        Time.timeScale = _prevTimeScale;
        IsGamePaused = false;
    }

    public bool CanPlayerMove()
    {
        return IsGamePaused == false;
    }

    public bool CanWorldInteract()
    {
        return IsGamePaused == false;
    }
}
