using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private const float DialogueBgmVolume = 0.3f;
    private const float DialogueBgmFadeDuration = 1f;
    private const string DialogueBgmCommandFadeOut = "FadeOut";
    private const string DialogueBgmCommandQuit = "Quit";
    private const string DialogueBgmCommandMuteEnvironment = "MuteEnvironment";
    private const string DialogueBgmCommandFadeIn = "FadeIn";

    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource AudioSource_Bgm;
    [SerializeField] private AudioSource AudioSource_Ambient;
    [SerializeField] private AudioSource AudioSource_Sfx;
    [SerializeField] private FootstepSoundController _footstepSoundController;

    private AudioSourceSnapshot _dialogueBgmStartSnapshot;
    private AudioSourceSnapshot _dialogueAmbientStartSnapshot;
    private string _currentDialogueBgmPath;
    private bool _isDialogueSoundStateActive;
    private bool _shouldFadeInNextDialogueBgm;
    private int _dialogueBgmRequestVersion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SoundManager Instance already exists. Duplicate SoundManager will be disabled.");
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
        InitializeIndividualSoundControllers();
    }

    public void PlayBgm(string soundDataId)
    {
        PlaySoundData(AudioSource_Bgm, soundDataId).Forget();
    }

    public void StopBgm()
    {
        StopAudioSource(AudioSource_Bgm, "BGM");
    }

    public void BeginDialogueSoundState()
    {
        if (_isDialogueSoundStateActive)
        {
            return;
        }

        _dialogueBgmStartSnapshot = AudioSourceSnapshot.Create(AudioSource_Bgm);
        _dialogueAmbientStartSnapshot = AudioSourceSnapshot.Create(AudioSource_Ambient);
        _currentDialogueBgmPath = string.Empty;
        _shouldFadeInNextDialogueBgm = false;
        _isDialogueSoundStateActive = true;
    }

    public void ApplyDialogueBgmCommand(string commandText)
    {
        if (_isDialogueSoundStateActive == false)
        {
            BeginDialogueSoundState();
        }

        ApplyDialogueBgmCommandAsync(commandText).Forget();
    }

    public void RestoreDialogueSoundState()
    {
        if (_isDialogueSoundStateActive == false)
        {
            return;
        }

        _dialogueBgmRequestVersion++;
        _shouldFadeInNextDialogueBgm = false;
        _currentDialogueBgmPath = string.Empty;

        RestoreAudioSource(AudioSource_Bgm, _dialogueBgmStartSnapshot, "BGM");
        RestoreAudioSource(AudioSource_Ambient, _dialogueAmbientStartSnapshot, "Ambient");

        _dialogueBgmStartSnapshot = default;
        _dialogueAmbientStartSnapshot = default;
        _isDialogueSoundStateActive = false;
    }

    public void PlayAmbient(string soundDataId)
    {
        PlaySoundData(AudioSource_Ambient, soundDataId).Forget();
    }

    public void StopAmbient()
    {
        StopAudioSource(AudioSource_Ambient, "Ambient");
    }

    public void PlaySfx(string soundDataId)
    {
        PlaySoundData(AudioSource_Sfx, soundDataId).Forget();
    }

    public void PlaySfxClip(AudioClip audioClip, float volume = 1f, float pitch = 1f)
    {
        if (AudioSource_Sfx == null)
        {
            Debug.LogWarning("SFX AudioSource is missing, so AudioClip cannot be played.");
            return;
        }

        if (audioClip == null)
        {
            Debug.LogWarning("AudioClip is missing, so SFX cannot be played.");
            return;
        }

        AudioSource_Sfx.loop = false;
        AudioSource_Sfx.pitch = pitch;
        AudioSource_Sfx.PlayOneShot(audioClip, Mathf.Clamp01(volume));
    }

    public void PlayPlayerFootstep()
    {
        if (_footstepSoundController == null)
        {
            Debug.LogWarning("FootstepSoundController is missing, so player footstep sound cannot be played.");
            return;
        }

        _footstepSoundController.PlayFootstep();
    }

    public void ApplyFootstepStageType(StageType stageType)
    {
        if (_footstepSoundController == null)
        {
            Debug.LogWarning("FootstepSoundController is missing, so footstep stage sound cannot be applied.");
            return;
        }

        _footstepSoundController.ApplyStageType(stageType);
    }

    private async UniTask PlaySoundData(AudioSource audioSource, string soundDataId)
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource is missing, so sound cannot be played. SoundDataId: {soundDataId}");
            return;
        }

        if (string.IsNullOrEmpty(soundDataId))
        {
            Debug.LogWarning("SoundDataId is empty, so sound cannot be played.");
            return;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is missing, so sound data cannot be loaded.");
            return;
        }

        SoundData soundData = GameDataManager.Instance.GetSoundData(soundDataId);
        if (soundData == null)
        {
            Debug.LogWarning($"SoundData was not found: {soundDataId}");
            return;
        }

        if (string.IsNullOrEmpty(soundData.AudioPath))
        {
            Debug.LogWarning($"SoundData has no AudioPath: {soundDataId}");
            return;
        }

        if (ResourceManager.Inst == null)
        {
            Debug.LogWarning("ResourceManager.Inst is missing, so AudioClip cannot be loaded.");
            return;
        }

        AudioClip clip = await ResourceManager.Inst.LoadAsset<AudioClip>(soundData.AudioPath);
        if (clip == null)
        {
            Debug.LogError($"{soundData.AudioPath} AudioClip could not be found. Check the Addressables key.");
            return;
        }

        ApplyAudioSourceSetting(audioSource, soundData);

        if (soundData.IsLoop)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
            return;
        }

        audioSource.loop = false;
        audioSource.PlayOneShot(clip);
    }

    private async UniTaskVoid ApplyDialogueBgmCommandAsync(string commandText)
    {
        if (string.IsNullOrEmpty(commandText))
        {
            return;
        }

        string[] commandList = commandText.Split(',');
        for (int i = 0; i < commandList.Length; i++)
        {
            string command = commandList[i].Trim();
            if (string.IsNullOrEmpty(command))
            {
                continue;
            }

            if (string.Equals(command, DialogueBgmCommandFadeOut, StringComparison.OrdinalIgnoreCase))
            {
                await FadeOutCurrentDialogueBgmAsync();
                continue;
            }

            if (string.Equals(command, DialogueBgmCommandQuit, StringComparison.OrdinalIgnoreCase))
            {
                QuitCurrentDialogueBgm();
                continue;
            }

            if (string.Equals(command, DialogueBgmCommandMuteEnvironment, StringComparison.OrdinalIgnoreCase))
            {
                MuteCurrentStageEnvironmentForDialogue();
                continue;
            }

            if (string.Equals(command, DialogueBgmCommandFadeIn, StringComparison.OrdinalIgnoreCase))
            {
                _shouldFadeInNextDialogueBgm = true;
                continue;
            }

            await PlayDialogueBgmByPathAsync(command, _shouldFadeInNextDialogueBgm);
            _shouldFadeInNextDialogueBgm = false;
        }
    }

    private async UniTask PlayDialogueBgmByPathAsync(string audioPath, bool shouldFadeIn)
    {
        if (AudioSource_Bgm == null)
        {
            Debug.LogWarning($"BGM AudioSource is missing, so dialogue BGM cannot be played. AudioPath: {audioPath}");
            return;
        }

        if (string.IsNullOrEmpty(audioPath))
        {
            Debug.LogWarning("Dialogue BGM path is empty, so dialogue BGM cannot be played.");
            return;
        }

        if (ResourceManager.Inst == null)
        {
            Debug.LogWarning("ResourceManager.Inst is missing, so dialogue BGM AudioClip cannot be loaded.");
            return;
        }

        if (string.IsNullOrEmpty(_currentDialogueBgmPath) == false)
        {
            QuitCurrentDialogueBgm();
        }

        int requestVersion = ++_dialogueBgmRequestVersion;
        AudioClip clip = await ResourceManager.Inst.LoadAsset<AudioClip>(audioPath);
        if (requestVersion != _dialogueBgmRequestVersion)
        {
            return;
        }

        if (clip == null)
        {
            Debug.LogError($"{audioPath} Dialogue BGM AudioClip could not be found. Check the Addressables key.");
            return;
        }

        _currentDialogueBgmPath = audioPath;
        AudioSource_Bgm.clip = clip;
        AudioSource_Bgm.loop = true;
        AudioSource_Bgm.pitch = 1f;
        AudioSource_Bgm.volume = shouldFadeIn ? 0f : DialogueBgmVolume;
        AudioSource_Bgm.Play();

        if (shouldFadeIn)
        {
            await FadeAudioSourceVolumeAsync(AudioSource_Bgm, 0f, DialogueBgmVolume, DialogueBgmFadeDuration, requestVersion);
        }
    }

    private async UniTask FadeOutCurrentDialogueBgmAsync()
    {
        if (AudioSource_Bgm == null)
        {
            Debug.LogWarning("BGM AudioSource is missing, so dialogue BGM cannot be faded out.");
            return;
        }

        if (string.IsNullOrEmpty(_currentDialogueBgmPath))
        {
            return;
        }

        int requestVersion = ++_dialogueBgmRequestVersion;
        await FadeAudioSourceVolumeAsync(AudioSource_Bgm, AudioSource_Bgm.volume, 0f, DialogueBgmFadeDuration, requestVersion);

        if (requestVersion != _dialogueBgmRequestVersion)
        {
            return;
        }

        AudioSource_Bgm.Stop();
        AudioSource_Bgm.clip = null;
        _currentDialogueBgmPath = string.Empty;
    }

    private async UniTask FadeAudioSourceVolumeAsync(AudioSource audioSource, float startVolume, float targetVolume, float duration, int requestVersion)
    {
        if (audioSource == null)
        {
            return;
        }

        if (duration <= 0f)
        {
            audioSource.volume = targetVolume;
            return;
        }

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            if (requestVersion != _dialogueBgmRequestVersion)
            {
                return;
            }

            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.SmoothStep(0f, 1f, progress));
            await UniTask.Yield();
        }

        audioSource.volume = targetVolume;
    }

    private void QuitCurrentDialogueBgm()
    {
        _dialogueBgmRequestVersion++;
        _shouldFadeInNextDialogueBgm = false;

        if (AudioSource_Bgm == null)
        {
            Debug.LogWarning("BGM AudioSource is missing, so dialogue BGM cannot be stopped.");
            return;
        }

        AudioSource_Bgm.Stop();
        AudioSource_Bgm.clip = null;
        _currentDialogueBgmPath = string.Empty;
    }

    private void MuteCurrentStageEnvironmentForDialogue()
    {
        // TODO: SoundManager의 스테이지 환경음 시스템이 정리되면,
        // 현재 Stage에 연결된 환경음 AudioSource 또는 Ambient 채널을 찾아
        // 대화가 진행되는 동안만 mute하고 RestoreDialogueSoundState에서 원래 상태로 되돌린다.
    }

    private void ApplyAudioSourceSetting(AudioSource audioSource, SoundData soundData)
    {
        if (audioSource == null || soundData == null)
        {
            return;
        }

        audioSource.volume = GetVolume(soundData);
        audioSource.pitch = GetPitch(soundData);
    }

    private float GetVolume(SoundData soundData)
    {
        if (soundData == null)
        {
            return 1f;
        }

        if (soundData.Volume <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(soundData.Volume);
    }

    private float GetPitch(SoundData soundData)
    {
        if (soundData == null)
        {
            return 1f;
        }

        if (soundData.Pitch <= 0f)
        {
            return 1f;
        }

        return soundData.Pitch;
    }

    private void StopAudioSource(AudioSource audioSource, string audioSourceName)
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"{audioSourceName} AudioSource is missing, so sound cannot be stopped.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = null;
    }

    private void RestoreAudioSource(AudioSource audioSource, AudioSourceSnapshot snapshot, string audioSourceName)
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"{audioSourceName} AudioSource is missing, so sound state cannot be restored.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = snapshot.Clip;
        audioSource.loop = snapshot.IsLoop;
        audioSource.volume = snapshot.Volume;
        audioSource.pitch = snapshot.Pitch;
        audioSource.mute = snapshot.IsMute;

        if (snapshot.Clip == null || snapshot.WasPlaying == false)
        {
            return;
        }

        if (snapshot.Time > 0f && snapshot.Time < snapshot.Clip.length)
        {
            audioSource.time = snapshot.Time;
        }

        audioSource.Play();
    }

    private void InitializeIndividualSoundControllers()
    {
        if (_footstepSoundController == null)
        {
            _footstepSoundController = GetComponentInChildren<FootstepSoundController>(true);
        }

        if (_footstepSoundController == null)
        {
            Debug.LogWarning("FootstepSoundController was not found under SoundManager.");
        }
    }

    private struct AudioSourceSnapshot
    {
        public AudioClip Clip;
        public float Volume;
        public float Pitch;
        public float Time;
        public bool IsLoop;
        public bool IsMute;
        public bool WasPlaying;

        public static AudioSourceSnapshot Create(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                return default;
            }

            return new AudioSourceSnapshot
            {
                Clip = audioSource.clip,
                Volume = audioSource.volume,
                Pitch = audioSource.pitch,
                Time = audioSource.time,
                IsLoop = audioSource.loop,
                IsMute = audioSource.mute,
                WasPlaying = audioSource.isPlaying
            };
        }
    }
}
