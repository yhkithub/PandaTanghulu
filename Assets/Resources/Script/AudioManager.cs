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
    public bool isBgm = false; // BGM 여부를 나타내는 플래그 추가

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

    public string bgmSoundName; // Inspector에서 BGM으로 사용할 Sound의 이름 지정
    private AudioSource currentBgmSource;
    private Sound currentBgmSoundObject; // 현재 BGM의 Sound 객체 참조

    private const string BGM_VOLUME_KEY = "BGMVolume"; // BGM 볼륨 저장 키
    private float masterBgmVolume = 1.0f; // 마스터 BGM 볼륨 (0.0 ~ 1.0)

    private const string SFX_VOLUME_KEY = "SFXVolume";
    private float masterSfxVolume = 1.0f;

    private void Awake()
    {
        Debug.Log("AudioManager Awake() called");

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

            if (!string.IsNullOrEmpty(bgmSoundName) && s.name == bgmSoundName)
            {
                currentBgmSoundObject = s; // Sound 객체 저장
                currentBgmSource = s.source;
                // ... (기존 BGM 초기화 코드) ...
                ApplyBgmVolume(); // BGM 볼륨 적용
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
        IsBgmEnabled = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        IsSfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
        masterBgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f); // BGM 볼륨 로드, 기본값 1.0
        // masterSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f); // SFX 볼륨도 유사하게 로드
        masterBgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
        masterSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f); // SFX 볼륨 로드
        Debug.Log($"AudioManager Loaded: ... SFX Vol={masterSfxVolume}");
    }

    public void SetMasterSfxVolume(float volume)
    {
        masterSfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, masterSfxVolume);
        PlayerPrefs.Save();
        Debug.Log($"AudioManager: Master SFX Volume set to {masterSfxVolume}");
        // 현재 재생중인 모든 SFX source의 볼륨을 업데이트 하기는 어려움 (특히 PlayOneShot)
        // 루프 SFX가 있다면 여기서 볼륨 갱신 필요
    }

    public void PlayOneShotSound(string name)
    {
        if (!IsSfxEnabled) return;
        Sound s = sounds.Find(sound => sound.name == name);
        if (s == null) { /* ... 오류 ... */ return; }

        // PlayOneShot은 AudioSource의 볼륨을 직접 변경하는 것이 아니라, 재생 시 볼륨 스케일을 인자로 받음
        // s.source.PlayOneShot(s.clip, s.volume * masterSfxVolume);
        // 단, 이렇게 하려면 PlayOneShot을 호출하는 s.source 자체의 볼륨이 1이어야 왜곡이 없음
        // 또는, PlayOneShot을 위한 전용 AudioSource를 두고 그 볼륨을 masterSfxVolume으로 설정하는 방법도 있음
        // 현재 구조에서는 s.source.volume을 미리 설정하고 PlayOneShot(s.clip)을 호출하는 것이 일관성 있음:
        s.source.volume = s.volume * masterSfxVolume; // 재생 직전 볼륨 설정
        s.source.PlayOneShot(s.clip);
    }

    // BGM 활성화/비활성화 (볼륨에도 영향)
    public void SetBgmEnabled(bool isEnabled)
    {
        IsBgmEnabled = isEnabled;
        PlayerPrefs.SetInt(BGM_KEY, IsBgmEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"AudioManager: BGM status set to {IsBgmEnabled}");
        ApplyBgmSettings();
    }

    // 새로운 마스터 BGM 볼륨 설정 메서드
    public void SetMasterBgmVolume(float volume)
    {
        masterBgmVolume = Mathf.Clamp01(volume); // 0과 1 사이로 값 제한
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, masterBgmVolume);
        PlayerPrefs.Save();
        Debug.Log($"AudioManager: Master BGM Volume set to {masterBgmVolume}");
        ApplyBgmVolume(); // 현재 BGM에 새 볼륨 적용
    }

    // 현재 BGM AudioSource에 볼륨 적용
    void ApplyBgmVolume()
    {
        if (currentBgmSource != null && currentBgmSoundObject != null)
        {
            // Sound 객체에 설정된 기본 볼륨과 마스터 볼륨을 곱함
            currentBgmSource.volume = currentBgmSoundObject.volume * masterBgmVolume;
        }
    }

    // BGM 재생 및 상태 전체 적용 (내부 헬퍼 함수)
    void ApplyBgmSettings() // BGM 재생 및 상태 전체 적용
    {
        if (currentBgmSource != null && currentBgmSoundObject != null)
        {
            if (!currentBgmSoundObject.isBgm) // 재생하려는 사운드가 BGM으로 설정되어 있지 않으면 경고
            {
                Debug.LogWarning($"AudioManager: BGM으로 재생하려는 사운드 '{currentBgmSoundObject.name}'는 isBgm 플래그가 설정되지 않았습니다.");
                // return; // 또는 재생을 막음
            }

            currentBgmSource.mute = !IsBgmEnabled;
            currentBgmSource.volume = currentBgmSoundObject.volume * masterBgmVolume;

            if (IsBgmEnabled && !currentBgmSource.isPlaying && currentBgmSource.clip != null)
            {
                currentBgmSource.Play();
                Debug.Log($"AudioManager: BGM \"{currentBgmSoundObject.name}\" 재생 시작.");
            }
            else if (!IsBgmEnabled && currentBgmSource.isPlaying)
            {
                currentBgmSource.Stop();
                Debug.Log($"AudioManager: BGM \"{currentBgmSoundObject.name}\" 중지 (BGM 비활성화됨).");
            }
        }
    }

    public void PlayBgm(string nameOfBgmSound)
    {
        Sound bgmToPlay = sounds.Find(sound => sound.name == nameOfBgmSound);
        if (bgmToPlay == null)
        {
            Debug.LogWarning("AudioManager: BGM 사운드 \"" + nameOfBgmSound + "\"을(를) 찾을 수 없습니다!");
            return;
        }

        // 현재 재생 중인 BGM과 요청된 BGM이 동일하고, 이미 재생 중이라면 아무것도 하지 않음
        if (currentBgmSource != null && currentBgmSoundObject == bgmToPlay && currentBgmSource.isPlaying)
        {
            Debug.Log("AudioManager: 요청된 BGM \"" + nameOfBgmSound + "\"이(가) 이미 재생 중입니다.");
            return; // 이미 재생 중이므로 변경 없음
        }

        // 기존에 다른 BGM이 재생 중이었다면 중지
        if (currentBgmSource != null && currentBgmSource.isPlaying)
        {
            currentBgmSource.Stop();
        }

        // 새로운 BGM 설정 및 재생
        currentBgmSoundObject = bgmToPlay;
        currentBgmSource = bgmToPlay.source;

        if (currentBgmSource != null)
        {
            currentBgmSource.clip = currentBgmSoundObject.clip; // 클립 재할당 (필수는 아닐 수 있으나 명확성을 위해)
            currentBgmSource.loop = true; // 루프 보장
            ApplyBgmSettings(); // 볼륨 적용 및 재생 (IsBgmEnabled 상태에 따라)
        }
        else
        {
            Debug.LogError("AudioManager: BGM 재생을 위한 AudioSource가 없습니다.");
        }
    }

    public void SetSfxEnabled(bool isEnabled)
    {
        IsSfxEnabled = isEnabled;
        PlayerPrefs.SetInt(SFX_KEY, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"AudioManager: SFX status set to {IsSfxEnabled}");

        foreach (Sound s in sounds)
        {
            if (s.isBgm) // isBgm 플래그가 true이면 BGM이므로 건너뛰기
            {
                continue;
            }

            if (s.source != null)
            {
                s.source.mute = !IsSfxEnabled; // SFX가 아니면 SFX 설정에 따라 음소거
                if (!IsSfxEnabled && s.source.isPlaying && !s.loop)
                {
                    // s.source.Stop(); // 일회성 효과음 즉시 중지 (선택적)
                }
            }
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