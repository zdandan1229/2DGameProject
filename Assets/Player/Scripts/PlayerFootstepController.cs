using UnityEngine;

public class PlayerFootstepController : MonoBehaviour
{
    private bool _didWarnMissingSoundManager;

    public void PlayFootstep()
    {
        if (SoundManager.Instance == null)
        {
            LogMissingSoundManager();
            return;
        }

        SoundManager.Instance.PlayPlayerFootstep();
    }

    private void LogMissingSoundManager()
    {
        if (_didWarnMissingSoundManager)
        {
            return;
        }

        Debug.LogWarning("SoundManager.Instance is missing, so player footstep sound cannot be played.");
        _didWarnMissingSoundManager = true;
    }
}
