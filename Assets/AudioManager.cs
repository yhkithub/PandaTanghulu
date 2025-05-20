// AudioManager.cs
using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필요

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public float volume = 1f;
    public float pitch = 1f;
    public bool loop = false;

    [HideInInspector] public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public List<Sound> sounds;
    private AudioSource textSource; // 텍스트 사운드 전용 AudioSource 참조

    // --- 설정 값 저장을 위한 키 (TitleManager와 동일하게 사용) ---
    private const string BGM_KEY = "BGMOn";
    private const string SFX_KEY = "SFXOn";

    // --- 현재 오디오 설정 상태 ---
    public bool IsBgmEnabled { get; private set; } = true;
    public bool IsSfxEnabled { get; private set; } = true;

    // --- BGM 전용 AudioSource (Inspector에서 할당하거나 특정 이름으로 찾도록 수정 필요) ---
    public AudioSource bgmAudioSource; // 예시: BGM을 위한 별도 AudioSource, Inspector에서 할당
    private Sound bgmSound; // 또는 Sound 리스트에서 BGM으로 사용할 Sound 객체

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- PlayerPrefs에서 오디오 설정 불러오기 ---
        LoadAudioSettings();

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.playOnAwake = false;

            // --- 각 사운드(SFX) 초기 음소거 상태 적용 ---
            s.source.mute = !IsSfxEnabled; // SFX 설정에 따라 초기 음소거

            if (s.name == "Text")
            {
                textSource = s.source;
            }

            // --- BGM으로 사용할 사운드 찾기 (이름 기반 예시) ---
            if (s.name == "BackgroundMusic") // BGM 사운드 이름이 "BackgroundMusic"이라고 가정
            {
                bgmSound = s;
                // BGM 초기 음소거 상태 적용 및 자동 재생 (필요하다면)
                bgmSound.source.mute = !IsBgmEnabled;
                if (IsBgmEnabled && bgmSound.source.playOnAwake) // playOnAwake가 true이고 BGM이 활성화 상태면 재생
                {
                    // bgmSound.source.Play(); // 또는 여기서 직접 Play() 호출
                }
            }
        }

        // --- BGM 전용 AudioSource가 있다면 초기 음소거 상태 적용 ---
        if (bgmAudioSource != null)
        {
            bgmAudioSource.mute = !IsBgmEnabled;
            // if (IsBgmEnabled && bgmAudioSource.playOnAwake) bgmAudioSource.Play();
        }
    }

    void LoadAudioSettings()
    {
        IsBgmEnabled = PlayerPrefs.GetInt(BGM_KEY, 1) == 1; // 기본값 1 (ON)
        IsSfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1; // 기본값 1 (ON)
        Debug.Log($"AudioManager Loaded Settings: BGM Enabled = {IsBgmEnabled}, SFX Enabled = {IsSfxEnabled}");
    }

    public void SetSfxEnabled(bool isEnabled)
    {
        IsSfxEnabled = isEnabled;
        PlayerPrefs.SetInt(SFX_KEY, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"AudioManager: SFX status set to {IsSfxEnabled}");

        // 모든 SFX AudioSource의 mute 상태 업데이트
        foreach (Sound s in sounds)
        {
            // BGM으로 지정된 사운드는 SFX 설정의 영향을 받지 않도록 예외 처리 (선택적)
            if (bgmSound != null && s.name == bgmSound.name)
            {
                continue; // BGM 사운드면 건너뛰기
            }

            if (s.source != null)
            {
                s.source.mute = !IsSfxEnabled;
                if (!IsSfxEnabled && s.source.isPlaying && !s.loop) // SFX가 꺼졌고, 재생 중인 일회성 사운드면 중지
                {
                   // s.source.Stop(); // 일회성 사운드 즉시 중지 (선택적)
                }
            }
        }
    }

    public void SetBgmEnabled(bool isEnabled)
    {
        IsBgmEnabled = isEnabled;
        PlayerPrefs.SetInt(BGM_KEY, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"AudioManager: BGM status set to {IsBgmEnabled}");

        // BGM AudioSource의 mute 상태 업데이트
        if (bgmAudioSource != null) // Inspector에서 직접 할당한 BGM 소스
        {
            bgmAudioSource.mute = !IsBgmEnabled;
            if (IsBgmEnabled && !bgmAudioSource.isPlaying && bgmAudioSource.clip != null) bgmAudioSource.Play(); // 꺼져있던 BGM 다시 켜기
            else if (!IsBgmEnabled && bgmAudioSource.isPlaying) bgmAudioSource.Stop(); // BGM 즉시 중지
        }
        else if (bgmSound != null && bgmSound.source != null) // Sound 리스트에서 찾은 BGM 소스
        {
            bgmSound.source.mute = !IsBgmEnabled;
             if (IsBgmEnabled && !bgmSound.source.isPlaying && bgmSound.source.clip != null) bgmSound.source.Play();
             else if (!IsBgmEnabled && bgmSound.source.isPlaying) bgmSound.source.Stop();
        }
    }

    public void PlaySound(string name)
    {
        Sound s = sounds.Find(sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        // BGM인지 SFX인지 구분하여 설정 확인 (더 정교한 로직 필요 가능)
        // 여기서는 일단 모든 PlaySound 요청을 SFX로 간주하고 IsSfxEnabled를 확인
        // 만약 BGM을 PlaySound로 재생한다면, 아래 IsSfxEnabled 체크를 수정해야 함
        if (bgmSound != null && s.name == bgmSound.name) // 재생하려는 사운드가 BGM인 경우
        {
            if (!IsBgmEnabled) return; // BGM이 꺼져있으면 재생 안함
        }
        else // SFX인 경우
        {
            if (!IsSfxEnabled) return; // SFX가 꺼져있으면 재생 안함
        }

        s.source.Play();
    }

    public void PlayOneShotSound(string name)
    {
        // SFX 전용으로 간주하고 IsSfxEnabled만 확인
        if (!IsSfxEnabled)
        {
            // Debug.Log("SFX is disabled. Not playing oneshot: " + name); // 필요시 로그
            return;
        }

        Sound s = sounds.Find(sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        // PlayOneShot은 해당 AudioSource의 현재 재생 상태에 영향을 주지 않고 독립적으로 재생합니다.
        // AudioSource가 음소거 상태(s.source.mute == true)여도 PlayOneShot은 소리를 냅니다.
        // 따라서, PlayOneShot을 호출하기 전에 s.source의 mute 상태를 직접 제어하거나,
        // PlayOneShot 전용 AudioSource를 사용하고 그 AudioSource의 볼륨/뮤트를 IsSfxEnabled에 따라 조절해야 합니다.
        // 여기서는 간단히 s.source를 사용하므로, s.source.mute 상태가 IsSfxEnabled에 의해 이미 설정되었다고 가정합니다.
        // 하지만 PlayOneShot은 AudioSource의 mute 속성을 직접적으로 따르지 않을 수 있으므로 주의가 필요합니다.
        // 가장 확실한 방법은 PlayOneShot 전에 IsSfxEnabled를 확인하는 것입니다. (이미 위에서 했음)
        s.source.PlayOneShot(s.clip);
    }

    // BGM 재생/중지 메서드 (예시)
    public void PlayBackgroundMusic(string name)
    {
        if (bgmSound != null && bgmSound.name == name) // 기존 bgmSound 객체를 사용
        {
            if (IsBgmEnabled && !bgmSound.source.isPlaying)
            {
                bgmSound.source.Play();
            }
        }
        else // 새로운 BGM 이름으로 찾는 경우
        {
            Sound s = sounds.Find(sound => sound.name == name);
            if (s != null && s.loop) // BGM은 보통 loop
            {
                bgmSound = s; // 새로운 BGM으로 설정
                if (bgmAudioSource != null && bgmAudioSource.isPlaying) bgmAudioSource.Stop(); // 기존 Inspector 할당 BGM 중지

                bgmSound.source.mute = !IsBgmEnabled;
                if (IsBgmEnabled && !bgmSound.source.isPlaying)
                {
                    bgmSound.source.Play();
                }
            }
        }
    }

    public void StopBackgroundMusic()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
        if (bgmSound != null && bgmSound.source != null && bgmSound.source.isPlaying)
        {
            bgmSound.source.Stop();
        }
    }


    public void StopTextSound()
    {
        if (textSource != null && textSource.isPlaying)
        {
            textSource.Stop();
        }
    }
}