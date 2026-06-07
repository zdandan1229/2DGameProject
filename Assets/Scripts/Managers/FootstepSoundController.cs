using UnityEngine;

public class FootstepSoundController : MonoBehaviour
{
    [SerializeField] private AudioSource AudioSource_Footstep;
    [SerializeField] private AudioReverbFilter AudioReverbFilter_Footstep;
    [SerializeField] private AudioClip[] _outsideFootstepClipList;
    [SerializeField] private AudioClip[] _roomFootstepClipList;
    [SerializeField] private float _outsideFootstepVolume = 0.5f;
    [SerializeField] private float _roomFootstepVolume = 0.8f;
    [SerializeField] private float _corridorFootstepVolume = 1f;
    [SerializeField] private float _minPitch = 0.94f;
    [SerializeField] private float _maxPitch = 1.06f;
    [SerializeField] private AudioReverbPreset _corridorReverbPreset = AudioReverbPreset.Hallway;

    private AudioClip[] _currentFootstepClipList;
    private float _currentFootstepVolume;
    private int _nextFootstepClipIndex;
    private bool _didWarnMissingFootstepClip;

    private void Awake()
    {
        InitializeAudio();
        ApplyStageType(StageType.Outside);
    }

    public void ApplyStageType(StageType stageType)
    {
        switch (stageType)
        {
            case StageType.Outside:
                SetCurrentFootstepEnvironment(_outsideFootstepClipList, _outsideFootstepVolume, false);
                break;
            case StageType.SmallRoom:
            case StageType.LargeRoom:
                SetCurrentFootstepEnvironment(_roomFootstepClipList, _roomFootstepVolume, false);
                break;
            case StageType.Corridor:
                SetCurrentFootstepEnvironment(_roomFootstepClipList, _corridorFootstepVolume, true);
                break;
            default:
                Debug.LogWarning($"Unsupported StageType for footstep sound: {stageType}");
                SetCurrentFootstepEnvironment(_outsideFootstepClipList, _outsideFootstepVolume, false);
                break;
        }
    }

    public void PlayFootstep()
    {
        if (AudioSource_Footstep == null)
        {
            Debug.LogWarning("Footstep AudioSource is missing, so footstep sound cannot be played.");
            return;
        }

        AudioClip footstepClip = GetNextFootstepClip();
        if (footstepClip == null)
        {
            return;
        }

        AudioSource_Footstep.loop = false;
        AudioSource_Footstep.pitch = Random.Range(_minPitch, _maxPitch);
        AudioSource_Footstep.PlayOneShot(footstepClip, Mathf.Clamp01(_currentFootstepVolume));
    }

    private void InitializeAudio()
    {
        if (AudioSource_Footstep == null)
        {
            AudioSource_Footstep = GetComponentInChildren<AudioSource>();
        }

        if (AudioSource_Footstep == null)
        {
            Debug.LogWarning("Footstep AudioSource is missing from FootstepSoundController.");
            return;
        }

        AudioSource_Footstep.playOnAwake = false;
        AudioSource_Footstep.loop = false;
        AudioSource_Footstep.spatialBlend = 0f;

        if (AudioReverbFilter_Footstep == null)
        {
            AudioReverbFilter_Footstep = AudioSource_Footstep.GetComponent<AudioReverbFilter>();
        }

        if (AudioReverbFilter_Footstep == null)
        {
            AudioReverbFilter_Footstep = AudioSource_Footstep.gameObject.AddComponent<AudioReverbFilter>();
        }

        AudioReverbFilter_Footstep.reverbPreset = _corridorReverbPreset;
        AudioReverbFilter_Footstep.enabled = false;
    }

    private void SetCurrentFootstepEnvironment(AudioClip[] footstepClipList, float volume, bool shouldUseReverb)
    {
        if (footstepClipList == null || footstepClipList.Length <= 0)
        {
            Debug.LogWarning("Footstep clip list is empty, so footstep environment cannot be applied.");
            return;
        }

        _currentFootstepClipList = footstepClipList;
        _currentFootstepVolume = Mathf.Clamp01(volume);
        _nextFootstepClipIndex = 0;
        SetReverbEnabled(shouldUseReverb);
    }

    private void SetReverbEnabled(bool shouldUseReverb)
    {
        if (AudioReverbFilter_Footstep == null)
        {
            if (shouldUseReverb)
            {
                Debug.LogWarning("Footstep AudioReverbFilter is missing, so corridor footstep reverb cannot be applied.");
            }

            return;
        }

        AudioReverbFilter_Footstep.reverbPreset = _corridorReverbPreset;
        AudioReverbFilter_Footstep.enabled = shouldUseReverb;
    }

    private AudioClip GetNextFootstepClip()
    {
        if (_currentFootstepClipList == null || _currentFootstepClipList.Length <= 0)
        {
            LogMissingFootstepClip("Footstep clip list is empty.");
            return null;
        }

        AudioClip footstepClip = _currentFootstepClipList[_nextFootstepClipIndex];
        _nextFootstepClipIndex = (_nextFootstepClipIndex + 1) % _currentFootstepClipList.Length;

        if (footstepClip == null)
        {
            LogMissingFootstepClip("Footstep AudioClip is missing from current footstep clip list.");
            return null;
        }

        return footstepClip;
    }

    private void LogMissingFootstepClip(string message)
    {
        if (_didWarnMissingFootstepClip)
        {
            return;
        }

        Debug.LogWarning(message);
        _didWarnMissingFootstepClip = true;
    }
}
