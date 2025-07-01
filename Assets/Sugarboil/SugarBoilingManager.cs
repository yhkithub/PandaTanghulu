using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class SugarBoilingManager : MonoBehaviour
{
   [Header("핵심 UI 요소")]
    public Image timingBarIndicator;
    public RectTransform successZone;
    public Button clickButton;
    public Image resultImageDisplay;
    public Image successSparkleImage;
    public Sprite clearImageSprite, failImageSprite;

    [Header("스테이지 정보 UI")]
    public TextMeshProUGUI stageTitleText;
    public float stageTitleDisplayDuration = 1.5f;
    public TextMeshProUGUI currentInductionStepText;

    [Header("비주얼 이펙트")]
    public Image inductionImage;
    public ParticleSystem panSmokeEffectParticle;
    // [중요] Inspector에서 배열 순서 확인: [0]=꺼짐, [1]=1단계, [2]=2단계, [3]=3단계
    public Sprite[] inductionSprites; 

    [Header("게임 설정 값")]
    public int totalChallengeCount = 3;
    public float resultDisplayDuration = 1.0f;
    public float[] speedPerStep = new float[3] { 2.0f, 3.0f, 4.0f }; // 스테이지 모드 기본 속도

    [Header("사운드 설정")]
    public string boilingLoopSoundName = "Boiling";
    public string successSoundName = "Success";
    public string failSoundName = "Fail";

    // --- 내부 변수 ---
    private AudioSource boilingAudioSource;
    private AudioSource sfxAudioSource;     
    private float currentIndicatorSpeed;
    private int currentChallengeStep = 1;
    private int currentSuccessCount = 0;
    private bool isIndicatorMoving = false;
    private bool moveRight = true;
    private float minX, maxX;
    private float halfIndicatorWidth;
    private float stageSpeedMultiplier = 1.0f;
    private float stageSuccessZoneWidth = 150f;

    void Awake()
    {
        boilingAudioSource = gameObject.AddComponent<AudioSource>();
        boilingAudioSource.playOnAwake = false;
        boilingAudioSource.loop = true;

        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;
    }

    void Start()
    {
        if (clickButton != null) clickButton.onClick.AddListener(OnTimingClick);
        
        halfIndicatorWidth = timingBarIndicator.rectTransform.rect.width / 2;
        RectTransform parentRect = timingBarIndicator.transform.parent.GetComponent<RectTransform>();
        float halfParentWidth = parentRect.rect.width / 2;
        minX = -halfParentWidth + halfIndicatorWidth;
        maxX = halfParentWidth - halfIndicatorWidth;

        resultImageDisplay.gameObject.SetActive(false);
        successSparkleImage.gameObject.SetActive(false);
        clickButton.interactable = false;

        ResetAndStartGame();
    }
    
    private void ResetAndStartGame()
    {
        StopAllCoroutines();
        StopVisualAndSoundEffects();
        
        currentChallengeStep = 1;
        currentSuccessCount = 0;

        if (GameModeManager.IsEndlessMode && EndlessModeController.Instance != null)
        {
            stageSpeedMultiplier = EndlessModeController.Instance.GetSugarBoilingSpeed();
            stageSuccessZoneWidth = 120f;
        }
        else if (CustomerOrderManager.Instance?.CurrentOrderData != null)
        {
            stageSpeedMultiplier = CustomerOrderManager.Instance.CurrentOrderData.sugarBoilingSpeed;
            stageSuccessZoneWidth = CustomerOrderManager.Instance.CurrentOrderData.sugarBoilingSuccessZoneWidth;
        }

        StartCoroutine(ShowStageTitleAndStartFirstChallenge());
    }


    void Update()
    {
        if (!isIndicatorMoving) return;

        Vector3 position = timingBarIndicator.rectTransform.anchoredPosition;
        position.x += (moveRight ? currentIndicatorSpeed : -currentIndicatorSpeed) * Time.deltaTime * 100f;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        timingBarIndicator.rectTransform.anchoredPosition = position;

        if (position.x >= maxX || position.x <= minX)
        {
            moveRight = !moveRight;
        }
    }

    IEnumerator ShowStageTitleAndStartFirstChallenge()
    {
        UpdateCurrentInductionStepText();
        if (stageTitleText != null)
        {
            stageTitleText.text = "설탕물 끓이기";
            stageTitleText.gameObject.SetActive(true);
            yield return new WaitForSeconds(stageTitleDisplayDuration);
            stageTitleText.gameObject.SetActive(false);
        }
        StartTimingGame();
    }

    public void StartTimingGame()
    {
        float[] baseSpeeds;
        if (GameModeManager.IsEndlessMode)
        {
            // ★★★[핵심 수정] 무한 모드일 때만 다른 기본 속도 배열 사용
            baseSpeeds = new float[] { 4.0f, 5.5f, 7.0f };
        }
        else
        {
            // 스테이지 모드일 때는 Inspector에 설정된 값을 그대로 사용
            baseSpeeds = this.speedPerStep;
        }
        
        currentIndicatorSpeed = baseSpeeds[currentChallengeStep - 1] * stageSpeedMultiplier;

        if (successZone != null)
        {
            successZone.sizeDelta = new Vector2(stageSuccessZoneWidth, successZone.sizeDelta.y);
            float successZoneWidth = successZone.rect.width;
            float randomRangeMin = minX + (successZoneWidth / 2) - halfIndicatorWidth;
            float randomRangeMax = maxX - (successZoneWidth / 2) + halfIndicatorWidth;
            float randomX = UnityEngine.Random.Range(randomRangeMin, randomRangeMax);
            successZone.anchoredPosition = new Vector2(randomX, successZone.anchoredPosition.y);
        }

        StartCoroutine(TimingGameRoutine());
    }

    IEnumerator TimingGameRoutine()
    {
        UpdateCurrentInductionStepText();
        SetInductionVisual(true);
        panSmokeEffectParticle?.Play();
        PlayBoilingSoundLoop();
        isIndicatorMoving = true;
        clickButton.interactable = true;
        resultImageDisplay.gameObject.SetActive(false);
        successSparkleImage.gameObject.SetActive(false);
        yield return null;
    }
    
    void StopVisualAndSoundEffects()
    {
        isIndicatorMoving = false;
        clickButton.interactable = false;
        SetInductionVisual(false);
        panSmokeEffectParticle?.Stop();
        if (boilingAudioSource != null && boilingAudioSource.isPlaying)
        {
            boilingAudioSource.Stop();
        }
    }

    public void OnTimingClick()
    {
        isIndicatorMoving = false;
        clickButton.interactable = false;

        float indicatorPos = timingBarIndicator.rectTransform.anchoredPosition.x;
        float successZoneMin = successZone.anchoredPosition.x - (successZone.rect.width / 2);
        float successZoneMax = successZone.anchoredPosition.x + (successZone.rect.width / 2);
        bool isSuccess = (indicatorPos >= successZoneMin && indicatorPos <= successZoneMax);

        if (isSuccess) currentSuccessCount++;
        
        StartCoroutine(ShowResultAndProceed(isSuccess));
    }

    // 결과 이미지와 사운드를 표시하고 다음 단계로 진행
    IEnumerator ShowResultAndProceed(bool isSuccess)
    {
        // 결과 이미지와 사운드는 공통으로 처리
        resultImageDisplay.sprite = isSuccess ? clearImageSprite : failImageSprite;
        resultImageDisplay.gameObject.SetActive(true);
        if (isSuccess && successSparkleImage != null)
        {
            successSparkleImage.gameObject.SetActive(true);
        }
        PlaySound(isSuccess ? successSoundName : failSoundName);
        
        // 끓는 소리는 결과 표시와 관계없이 바로 멈춤
        if (boilingAudioSource != null && boilingAudioSource.isPlaying)
        {
            boilingAudioSource.Stop();
        }

        yield return new WaitForSeconds(resultDisplayDuration);

        resultImageDisplay.gameObject.SetActive(false);
        successSparkleImage.gameObject.SetActive(false);
        
        yield return new WaitForSeconds(0.2f);

        if (isSuccess)
        {
            // 성공 시에는 인덕션을 끄지 않고 바로 다음 단계로 넘어갑니다.
            currentChallengeStep++;
            if (currentChallengeStep > totalChallengeCount)
            {
                StopVisualAndSoundEffects(); // 모든 단계 완료 후 효과 끄기
                GoToNextMajorStage();
            }
            else
            {
                StartTimingGame(); // 다음 타이밍 게임 시작 (이 안에서 인덕션 이미지가 다음 단계로 교체됨)
            }
        }
        else
        {
            // 실패 시에만 모든 시각/사운드 효과를 확실히 끕니다.
            StopVisualAndSoundEffects();
            HandleFailure();
        }
    }
    
    void UpdateCurrentInductionStepText()
    {
        if (currentInductionStepText != null)
        {
            currentInductionStepText.text = $"{currentChallengeStep}단계";
        }
    }
    
    void SetInductionVisual(bool isOn)
    {
        if (inductionImage == null || inductionSprites.Length <= currentChallengeStep) return;
        inductionImage.sprite = isOn ? inductionSprites[currentChallengeStep] : inductionSprites[0];
    }
    
    void HandleFailure()
    {
        HeartManager.Instance?.LoseHeart();
        
        if (HeartManager.Instance != null && HeartManager.Instance.CurrentHearts > 0)
        {
            ResetAndStartGame();
        }
    }

    void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName) && sfxAudioSource != null)
        {
            Sound soundToPlay = AudioManager.Instance.sounds.Find(s => s.name == soundName);
            if (soundToPlay != null && soundToPlay.clip != null)
            {
                sfxAudioSource.PlayOneShot(soundToPlay.clip, soundToPlay.volume);
            }
        }
    }
    
    void PlayBoilingSoundLoop()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(boilingLoopSoundName) && boilingAudioSource != null)
        {
            Sound soundToPlay = AudioManager.Instance.sounds.Find(s => s.name == boilingLoopSoundName);
            if (soundToPlay != null && soundToPlay.clip != null)
            {
                boilingAudioSource.clip = soundToPlay.clip;
                boilingAudioSource.volume = soundToPlay.volume;
                if (!boilingAudioSource.isPlaying)
                {
                    boilingAudioSource.Play();
                }
            }
        }
    }

    public void GoToNextMajorStage()
    {
        string nextSceneName = CustomerOrderManager.Instance?.sugarCoatingSceneName;
        if (string.IsNullOrEmpty(nextSceneName))
        {
            SceneSwitcher.Instance?.LoadScene("TitleScene");
        }
        else
        {
            SceneSwitcher.Instance?.LoadScene(nextSceneName);
        }
    }
}