using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsGamePaused { get; private set; }

    private float _prevTimeScale = 1f;
    private int _playerMoveLockCount;
    private int _directStageMoveLockCount;
    private int _inventoryHotkeyLockCount;
    private int _miniMapHotkeyLockCount;
    private int _journalHotkeyLockCount;
    private int _closeUIHotkeyLockCount;
    private bool _isTutorialDirectStageMoveLocked;
    private bool _isTutorialInventoryHotkeyLocked;
    private bool _isTutorialMiniMapHotkeyLocked;
    private bool _isTutorialJournalHotkeyLocked;

    private void Awake()
    {
        Instance = this;
        InitializeGameState();
    }

    private void InitializeGameState()
    {
        IsGamePaused = false;
        _prevTimeScale = 1f;
        _playerMoveLockCount = 0;
        _directStageMoveLockCount = 0;
        _inventoryHotkeyLockCount = 0;
        _miniMapHotkeyLockCount = 0;
        _journalHotkeyLockCount = 0;
        _closeUIHotkeyLockCount = 0;
        _isTutorialDirectStageMoveLocked = false;
        _isTutorialInventoryHotkeyLocked = false;
        _isTutorialMiniMapHotkeyLocked = false;
        _isTutorialJournalHotkeyLocked = false;
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

    public bool StartNewGame()
    {
        bool didPrepareNewGameStart = PrepareNewGameStart();
        if (didPrepareNewGameStart == false)
        {
            return false;
        }

        ResumeGame();
        return true;
    }

    public bool PrepareNewGameStart()
    {
        if (WorldTransitionManager.Instance == null)
        {
            Debug.LogWarning("WorldTransitionManager.Instance가 없어 새 게임을 시작할 수 없습니다.");
            return false;
        }

        return WorldTransitionManager.Instance.StartNewGame();
    }

    public bool PrepareNewGameStartAtEntryPoint(string entryPointId)
    {
        if (string.IsNullOrEmpty(entryPointId))
        {
            Debug.LogWarning("시작 EntryPointId가 비어 있어 새 게임 시작 위치를 지정할 수 없습니다.");
            return false;
        }

        if (WorldTransitionManager.Instance == null)
        {
            Debug.LogWarning("WorldTransitionManager.Instance가 없어 지정된 위치에서 새 게임을 시작할 수 없습니다.");
            return false;
        }

        return WorldTransitionManager.Instance.StartNewGameAtEntryPoint(entryPointId);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool CanPlayerMove()
    {
        return IsGamePaused == false && _playerMoveLockCount <= 0;
    }

    public bool CanWorldInteract()
    {
        return IsGamePaused == false;
    }

    public bool CanDirectStageMove()
    {
        return IsGamePaused == false && _directStageMoveLockCount <= 0;
    }

    public bool CanUseInventoryHotkey()
    {
        return _inventoryHotkeyLockCount <= 0;
    }

    public bool CanUseMiniMapHotkey()
    {
        return _miniMapHotkeyLockCount <= 0;
    }

    public bool CanUseJournalHotkey()
    {
        return _journalHotkeyLockCount <= 0;
    }

    public bool CanUseCloseUIHotkey()
    {
        return _closeUIHotkeyLockCount <= 0;
    }

    public void LockPlayerMove()
    {
        _playerMoveLockCount++;
    }

    public void UnlockPlayerMove()
    {
        if (_playerMoveLockCount <= 0)
        {
            Debug.LogWarning("Player move lock count is already zero.");
            _playerMoveLockCount = 0;
            return;
        }

        _playerMoveLockCount--;
    }

    public void LockDirectStageMove()
    {
        _directStageMoveLockCount++;
    }

    public void UnlockDirectStageMove()
    {
        if (_directStageMoveLockCount <= 0)
        {
            Debug.LogWarning("Direct stage move lock count is already zero.");
            _directStageMoveLockCount = 0;
            return;
        }

        _directStageMoveLockCount--;
    }

    public void LockInventoryHotkey()
    {
        _inventoryHotkeyLockCount++;
    }

    public void UnlockInventoryHotkey()
    {
        if (_inventoryHotkeyLockCount <= 0)
        {
            Debug.LogWarning("Inventory hotkey lock count is already zero.");
            _inventoryHotkeyLockCount = 0;
            return;
        }

        _inventoryHotkeyLockCount--;
    }

    public void LockMiniMapHotkey()
    {
        _miniMapHotkeyLockCount++;
    }

    public void UnlockMiniMapHotkey()
    {
        if (_miniMapHotkeyLockCount <= 0)
        {
            Debug.LogWarning("MiniMap hotkey lock count is already zero.");
            _miniMapHotkeyLockCount = 0;
            return;
        }

        _miniMapHotkeyLockCount--;
    }

    public void LockJournalHotkey()
    {
        _journalHotkeyLockCount++;
    }

    public void UnlockJournalHotkey()
    {
        if (_journalHotkeyLockCount <= 0)
        {
            Debug.LogWarning("Journal hotkey lock count is already zero.");
            _journalHotkeyLockCount = 0;
            return;
        }

        _journalHotkeyLockCount--;
    }

    public void LockCloseUIHotkey()
    {
        _closeUIHotkeyLockCount++;
    }

    public void UnlockCloseUIHotkey()
    {
        if (_closeUIHotkeyLockCount <= 0)
        {
            Debug.LogWarning("Close UI hotkey lock count is already zero.");
            _closeUIHotkeyLockCount = 0;
            return;
        }

        _closeUIHotkeyLockCount--;
    }

    public void ApplyTutorialStartLocks()
    {
        SetTutorialDirectStageMoveLocked(true);
        SetTutorialInventoryHotkeyLocked(true);
        SetTutorialMiniMapHotkeyLocked(true);
        SetTutorialJournalHotkeyLocked(true);
    }

    public void ApplyTutorialSkipStartLocks()
    {
        SetTutorialDirectStageMoveLocked(false);
        SetTutorialInventoryHotkeyLocked(false);
        SetTutorialMiniMapHotkeyLocked(false);
        SetTutorialJournalHotkeyLocked(false);
    }

    public void UnlockTutorialPilotDialogueLocks()
    {
        SetTutorialDirectStageMoveLocked(false);
        SetTutorialInventoryHotkeyLocked(false);
        SetTutorialJournalHotkeyLocked(false);
    }

    public void SetTutorialDirectStageMoveLocked(bool isLocked)
    {
        if (_isTutorialDirectStageMoveLocked == isLocked)
        {
            return;
        }

        _isTutorialDirectStageMoveLocked = isLocked;
        if (isLocked)
        {
            LockDirectStageMove();
            return;
        }

        UnlockDirectStageMove();
    }

    public void SetTutorialInventoryHotkeyLocked(bool isLocked)
    {
        if (_isTutorialInventoryHotkeyLocked == isLocked)
        {
            return;
        }

        _isTutorialInventoryHotkeyLocked = isLocked;
        if (isLocked)
        {
            LockInventoryHotkey();
            return;
        }

        UnlockInventoryHotkey();
    }

    public void SetTutorialMiniMapHotkeyLocked(bool isLocked)
    {
        if (_isTutorialMiniMapHotkeyLocked == isLocked)
        {
            return;
        }

        _isTutorialMiniMapHotkeyLocked = isLocked;
        if (isLocked)
        {
            LockMiniMapHotkey();
            return;
        }

        UnlockMiniMapHotkey();
    }

    public void SetTutorialJournalHotkeyLocked(bool isLocked)
    {
        if (_isTutorialJournalHotkeyLocked == isLocked)
        {
            return;
        }

        _isTutorialJournalHotkeyLocked = isLocked;
        if (isLocked)
        {
            LockJournalHotkey();
            return;
        }

        UnlockJournalHotkey();
    }
}
