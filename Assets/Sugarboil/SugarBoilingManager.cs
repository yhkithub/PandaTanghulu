// C:/Users/user/Documents/GitHub/PandaTanghulu/Assets/Sugarboil/SugarBoilingManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SugarBoilingManager : MonoBehaviour
{
    [Header("UI Elements - 연결 필수")]
    public Image timingBarIndicator;
    public RectTransform successZone;
    public Button clickButton;
    public Image resultImageDisplay;
    public Sprite clearImageSprite;
    public Sprite failImageSprite;
    public Image successSparkleImage;

    [Header("Tutorial UI - 튜토리얼 시 연결 필수")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialMessageText;
    public Button tutorialStartButton;

    [Header("Stage Info UI - 연결 선택")]
    public TextMeshProUGUI stageTitleText;
    public float stageTitleDisplayDuration = 1.5f;
    public TextMeshProUGUI currentInductionStepText;

    [Header("Order Display UI - 주문서 표시용")]
    public GameObject orderDisplayPanelInSugarBoil;
    public Transform orderFruitsContainerInSugarBoil;
    public Image fruitOrderIconPrefabForSugarBoil;
    public Image skewerStickIconPrefabForSugarBoil;

    [Header("Visual Effects - 연결 선택")]
    public Image inductionImage;
    public ParticleSystem panSmokeEffectParticle;
    public ParticleSystem sugarBubblesEffectParticle;

    [Header("Asset Sprites - 연결 필수")]
    public Sprite inductionOffSprite;
    public Sprite inductionOnSprite;
    public Sprite inductionLevel1Sprite;
    public Sprite inductionLevel2Sprite;

    [Header("Timing Bar Settings")]
    public float indicatorSpeed = 200f;
    public float successZoneBaseWidth = 120f;
    public float timingBarWidth = 500f;

    [Header("Sounds - AudioManager에 등록된 이름")]
    public string timingSuccessSoundName = "SugarSuccess";      // 각 타이밍 성공 시
    public string allStagesSuccessSoundName = "SugarFinalSuccess"; // 모든 단계 최종 성공 시
    public string timingFailureSoundName = "SugarFailure";       // 타이밍 실패 시
    public string boilingLoopSoundName = "SugarBoilingLoop";     // 설탕 끓는 소리 (루프)
    public string inductionLevelUpSoundName = "InductionLevelUp";  // 인덕션 단계 변경 시

    [Header("Debug & Testing")]
    public bool enableDebugMode = false; // ★★★ 테스트 시 true로 설정 ★★★
    public CustomerOrderData debugDefaultOrderData;
    public int debugCustomerIndex = 0;

    private enum InductionStep { Initial, Stage1_ON, Stage2_Level1, Stage3_Level2, AllStagesComplete, FailedCurrentStage }
    private InductionStep currentInductionStep = InductionStep.Initial;
    private int currentTimingChallengeNumber = 0;
    private const int TOTAL_TIMING_CHALLENGES = 3;

    private bool isTimingChallengeActive = false;
    private float currentIndicatorPositionX = 0f;
    private int indicatorDirection = 1;
    private float currentSuccessZonePosX;
    private float currentSuccessZoneWidthActual;
    private Coroutine gameLoopCoroutine;
    private AudioSource boilingAudioSource;
    private bool isTutorialMode = false;

    void Start()
    {
        if (GameObject.FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.LogError("씬에 EventSystem이 없습니다! UI 상호작용이 작동하지 않습니다. GameObject > UI > Event System을 추가해주세요.");
        }
        

        boilingAudioSource = gameObject.AddComponent<AudioSource>();
        boilingAudioSource.playOnAwake = false;
        boilingAudioSource.loop = true;

        InitializeBaseUI();

        CustomerOrderData orderDataForCurrentScene = null;

        if (enableDebugMode && CustomerOrderManager.Instance == null)
        {
            Debug.LogWarning($"SUGAR BOILING - DEBUG MODE: CustomerOrderManager.Instance가 없어 Inspector에 연결된 debugDefaultOrderData를 사용합니다. (손님 인덱스: {debugCustomerIndex})");
            GameInfoHolder.CustomerIndexToLoad = debugCustomerIndex;
            orderDataForCurrentScene = debugDefaultOrderData;
            isTutorialMode = (debugCustomerIndex == 0);

            if (orderDataForCurrentScene == null) {
                Debug.LogError("SugarBoilingManager - DEBUG MODE ERROR: debugDefaultOrderData가 Inspector에 연결되지 않았습니다! 테스트를 진행할 수 없습니다.");
                return;
            }
        }
        else if (CustomerOrderManager.Instance != null)
        {
            isTutorialMode = CustomerOrderManager.Instance.isTutorialActive;
            orderDataForCurrentScene = CustomerOrderManager.Instance.CurrentOrderData;
            // GameInfoHolder.CustomerIndexToLoad는 CustomerOrderManager에서 이미 설정되었거나, 이전 씬에서 설정된 값을 유지.
            // 필요하다면 여기서 CustomerOrderManager.Instance.currentCustomerIndex로부터 GameInfoHolder를 업데이트 할 수 있습니다.
            // GameInfoHolder.CustomerIndexToLoad = CustomerOrderManager.Instance.currentCustomerIndex;


             if (orderDataForCurrentScene == null) {
                Debug.LogError("SugarBoilingManager - ERROR: CustomerOrderManager.Instance.CurrentOrderData가 null입니다! 이전 씬에서 정보가 제대로 전달되지 않았거나, CustomerOrderManager 초기화 문제입니다.");
                // LoadTitleScene(); // 예외 처리
                return;
            }
        }
        else
        {
            Debug.LogError("SugarBoilingManager - CRITICAL ERROR: CustomerOrderManager.Instance가 null이고 디버그 모드가 비활성화되어 게임을 진행할 수 없습니다. TitleScene으로 이동합니다.");
            LoadTitleScene();
            return;
        }

        DisplayCurrentOrderOnSugarBoilUI(orderDataForCurrentScene);

        if (isTutorialMode)
        {
            ShowTutorialUI();
        }
        else
        {
            StartCoroutine(ShowStageTitleAndStartFirstChallenge());
        }
    }

    void InitializeBaseUI()
    {
        if (clickButton != null) clickButton.gameObject.SetActive(false);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (successSparkleImage != null) successSparkleImage.gameObject.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (stageTitleText != null) stageTitleText.gameObject.SetActive(false);
        if (currentInductionStepText != null) currentInductionStepText.gameObject.SetActive(false);
        currentInductionStep = InductionStep.Initial;
        UpdateInductionVisual();
    }

    void DisplayCurrentOrderOnSugarBoilUI(CustomerOrderData orderToDisplay)
    {
        if (orderToDisplay == null)
        {
            if (orderDisplayPanelInSugarBoil != null) orderDisplayPanelInSugarBoil.SetActive(false);
            Debug.LogWarning("표시할 주문 데이터가 없습니다. (orderToDisplay is null)");
            return;
        }

        if (orderDisplayPanelInSugarBoil == null || orderFruitsContainerInSugarBoil == null || fruitOrderIconPrefabForSugarBoil == null)
        {
            Debug.LogWarning("설탕 끓이기 씬의 주문서 UI 요소(Order Display Panel, Order Fruits Container, Fruit Order Icon Prefab)가 Inspector에 제대로 연결되지 않았습니다.");
            if (orderDisplayPanelInSugarBoil != null) orderDisplayPanelInSugarBoil.SetActive(false);
            return;
        }

        orderDisplayPanelInSugarBoil.SetActive(true);

        foreach (Transform child in orderFruitsContainerInSugarBoil)
        {
            Destroy(child.gameObject);
        }

        if (skewerStickIconPrefabForSugarBoil != null)
        {
            Instantiate(skewerStickIconPrefabForSugarBoil, orderFruitsContainerInSugarBoil);
        }

        List<FruitType> fruitsInOrder = new List<FruitType>();
        if (orderToDisplay.skewerOrder != null)
        {
            foreach (var item in orderToDisplay.skewerOrder)
            {
                fruitsInOrder.Add(item.fruit);
            }
        }

        foreach (FruitType fruit in fruitsInOrder)
        {
            Sprite fruitSprite = GetSpriteForFruitTypeFromCustomerManager(fruit);
            if (fruitSprite != null)
            {
                Image icon = Instantiate(fruitOrderIconPrefabForSugarBoil, orderFruitsContainerInSugarBoil);
                icon.sprite = fruitSprite;
                icon.name = fruit.ToString() + "_OrderIcon_SugarBoil";
            }
            else
            {
                // 이 로그는 GetSpriteForFruitTypeFromCustomerManager 내부에서도 출력될 수 있습니다.
                // Debug.LogWarning($"주문서 UI (SugarBoil): {fruit}에 대한 스프라이트를 CustomerOrderManager에서 찾을 수 없습니다.");
            }
        }
    }

    Sprite GetSpriteForFruitTypeFromCustomerManager(FruitType fruitType)
    {
        if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.fruitSpritesForOrderUI != null)
        {
            // CustomerOrderManager에 GetSpriteForFruitUI와 같은 public 함수가 있다면 그것을 사용하는 것이 좋습니다.
            // 여기서는 CustomerOrderManager의 fruitSpritesForOrderUI 리스트를 직접 순회하는 예시를 유지합니다.
            foreach (var mapping in CustomerOrderManager.Instance.fruitSpritesForOrderUI) //
            {
                if (mapping.fruitType == fruitType) return mapping.sprite; //
            }
        }
        // 디버그 모드에서 CustomerOrderManager가 없을 때, debugDefaultOrderData에서 직접 스프라이트를 찾아볼 수도 있습니다.
        // 하지만 이는 CustomerOrderData 에셋에 스프라이트 정보가 직접 저장되어 있어야 가능합니다. (현재는 CustomerOrderManager가 관리)
        Debug.LogWarning($"GetSpriteForFruitTypeFromCustomerManager: {fruitType}에 대한 스프라이트를 찾을 수 없습니다. CustomerOrderManager.Instance: {(CustomerOrderManager.Instance != null)}, fruitSpritesForOrderUI: {(CustomerOrderManager.Instance?.fruitSpritesForOrderUI != null)}");
        return null;
    }

    void ShowTutorialUI()
    {
        if (tutorialPanel != null && tutorialStartButton != null && tutorialMessageText != null)
        {
            tutorialPanel.SetActive(true);
            tutorialMessageText.text = "설탕물이 알맞은 온도가 되도록\n타이밍에 맞춰 중앙의 버튼을 여러 번 클릭해 주세요!\n총 3번 성공해야 다음으로 넘어갈 수 있어요.";
            tutorialStartButton.onClick.RemoveAllListeners();
            tutorialStartButton.onClick.AddListener(StartGameFromTutorialButton);
            Debug.Log("튜토리얼 UI 표시됨, 버튼 리스너 추가됨.");
        }
        else
        {
            Debug.LogError("튜토리얼 UI 요소(Panel, MessageText, StartButton)가 Inspector에 제대로 연결되지 않았습니다. 바로 게임을 시작합니다.");
            StartCoroutine(ShowStageTitleAndStartFirstChallenge());
        }
    }

    public void StartGameFromTutorialButton()
    {
        Debug.Log("튜토리얼 시작 버튼 클릭 - 함수 호출됨!");
        Debug.Log("SUGAR BOILING - Tutorial Start Button CLICKED! (함수 호출됨)");
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        StartCoroutine(ShowStageTitleAndStartFirstChallenge());
    }

    IEnumerator ShowStageTitleAndStartFirstChallenge()
    {
        if (stageTitleText != null)
        {
            stageTitleText.text = "설탕 끓이기 단계"; // 필요시 현재 손님 이름 추가
            stageTitleText.gameObject.SetActive(true);
            yield return new WaitForSeconds(stageTitleDisplayDuration);
            stageTitleText.gameObject.SetActive(false);
        }
        currentInductionStep = InductionStep.Initial;
        currentTimingChallengeNumber = 0;
        PrepareAndStartNextTimingChallenge();
    }

    void PrepareAndStartNextTimingChallenge()
    {
        isTimingChallengeActive = false;
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
        if (clickButton != null) clickButton.gameObject.SetActive(false);

        currentTimingChallengeNumber++;
        if (currentTimingChallengeNumber == 1) currentInductionStep = InductionStep.Stage1_ON;
        else if (currentTimingChallengeNumber == 2) currentInductionStep = InductionStep.Stage2_Level1;
        else if (currentTimingChallengeNumber == 3) currentInductionStep = InductionStep.Stage3_Level2;
        else
        {
            Debug.LogError("PrepareAndStartNextTimingChallenge: 잘못된 타이밍 도전 번호입니다: " + currentTimingChallengeNumber);
            currentInductionStep = InductionStep.FailedCurrentStage;
            UpdateInductionVisual();
            StartCoroutine(HandleEndOfGameSequence(1.5f, false));
            return;
        }

        InitializeTimingSettingsForCurrentInductionStep();
        UpdateInductionVisual();
        UpdateCurrentInductionStepText();

        if (currentInductionStep >= InductionStep.Stage1_ON && currentInductionStep <= InductionStep.Stage3_Level2)
        {
            StartVisualEffects();
            PlayBoilingSoundLoop();
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(inductionLevelUpSoundName) && currentInductionStep != InductionStep.Stage1_ON)
            {
                 AudioManager.Instance.PlayOneShotSound(inductionLevelUpSoundName);
            }
        }
        
        StartCoroutine(StartTimingChallengeAfterDelay(0.5f)); // 짧은 딜레이 후 타이밍 게임 시작
    }

    IEnumerator StartTimingChallengeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isTimingChallengeActive = true;
        if (clickButton != null) clickButton.gameObject.SetActive(true);
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
        gameLoopCoroutine = StartCoroutine(MoveIndicatorCoroutine());
    }

    void InitializeTimingSettingsForCurrentInductionStep()
    {
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (successSparkleImage != null) successSparkleImage.gameObject.SetActive(false);

        int customerLevelForDifficulty = enableDebugMode && CustomerOrderManager.Instance == null ? debugCustomerIndex : GameInfoHolder.CustomerIndexToLoad;

        if (isTutorialMode)
        {
            switch (currentInductionStep)
            {
                case InductionStep.Stage1_ON: indicatorSpeed = 100f; currentSuccessZoneWidthActual = successZoneBaseWidth * 1.8f; break;
                case InductionStep.Stage2_Level1: indicatorSpeed = 110f; currentSuccessZoneWidthActual = successZoneBaseWidth * 1.6f; break;
                case InductionStep.Stage3_Level2: indicatorSpeed = 120f; currentSuccessZoneWidthActual = successZoneBaseWidth * 1.4f; break;
                default: indicatorSpeed = 100f; currentSuccessZoneWidthActual = successZoneBaseWidth * 1.8f; break;
            }
        }
        else
        {
            switch (currentInductionStep)
            {
                case InductionStep.Stage1_ON:
                    indicatorSpeed = 180f + (customerLevelForDifficulty * 10f);
                    currentSuccessZoneWidthActual = successZoneBaseWidth - (customerLevelForDifficulty * 8f);
                    break;
                case InductionStep.Stage2_Level1:
                    indicatorSpeed = 200f + (customerLevelForDifficulty * 15f);
                    currentSuccessZoneWidthActual = successZoneBaseWidth * 0.85f - (customerLevelForDifficulty * 10f);
                    break;
                case InductionStep.Stage3_Level2:
                    indicatorSpeed = 220f + (customerLevelForDifficulty * 20f);
                    currentSuccessZoneWidthActual = successZoneBaseWidth * 0.7f - (customerLevelForDifficulty * 12f);
                    break;
                default:
                    indicatorSpeed = 180f; currentSuccessZoneWidthActual = successZoneBaseWidth; break;
            }
        }
        currentSuccessZoneWidthActual = Mathf.Max(40f, currentSuccessZoneWidthActual);
        indicatorSpeed = Mathf.Max(100f, indicatorSpeed);

        if (successZone != null)
        {
            float timingBarHalfWidth = timingBarWidth / 2f;
            float successZoneHalfWidth = currentSuccessZoneWidthActual / 2f;
            float minPosX = -timingBarHalfWidth + successZoneHalfWidth;
            float maxPosX = timingBarHalfWidth - successZoneHalfWidth;
            currentSuccessZonePosX = Random.Range(minPosX, maxPosX);
            successZone.anchoredPosition = new Vector2(currentSuccessZonePosX, successZone.anchoredPosition.y);
            successZone.sizeDelta = new Vector2(currentSuccessZoneWidthActual, successZone.sizeDelta.y);
        }
        
        if (timingBarIndicator != null) {
            currentIndicatorPositionX = -timingBarWidth / 2f;
            timingBarIndicator.rectTransform.anchoredPosition = new Vector2(currentIndicatorPositionX, timingBarIndicator.rectTransform.anchoredPosition.y);
        }
        indicatorDirection = 1;
    }

    void UpdateInductionVisual()
    {
        if (inductionImage == null) return;

        switch (currentInductionStep)
        {
            case InductionStep.Initial:
            case InductionStep.FailedCurrentStage:
                inductionImage.sprite = inductionOffSprite;
                StopVisualEffects();
                break;
            case InductionStep.Stage1_ON:
                inductionImage.sprite = inductionOnSprite;
                break;
            case InductionStep.Stage2_Level1:
                inductionImage.sprite = inductionLevel1Sprite;
                break;
            case InductionStep.Stage3_Level2:
                inductionImage.sprite = inductionLevel2Sprite;
                break;
            case InductionStep.AllStagesComplete:
                inductionImage.sprite = inductionLevel2Sprite;
                break;
        }
    }
    
    void UpdateCurrentInductionStepText()
    {
        if (currentInductionStepText != null)
        {
            if (currentInductionStep >= InductionStep.Stage1_ON && currentInductionStep <= InductionStep.Stage3_Level2)
            {
                currentInductionStepText.gameObject.SetActive(true);
                currentInductionStepText.text = $"불 조절: {currentTimingChallengeNumber} / {TOTAL_TIMING_CHALLENGES} 단계";
            }
            else
            {
                currentInductionStepText.gameObject.SetActive(false);
            }
        }
    }

    void StartVisualEffects() {
        if (panSmokeEffectParticle != null && !panSmokeEffectParticle.isPlaying) panSmokeEffectParticle.Play();
        if (sugarBubblesEffectParticle != null && !sugarBubblesEffectParticle.isPlaying) sugarBubblesEffectParticle.Play();
    }

    void StopVisualEffects() {
        if (panSmokeEffectParticle != null && panSmokeEffectParticle.isPlaying) panSmokeEffectParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sugarBubblesEffectParticle != null && sugarBubblesEffectParticle.isPlaying) sugarBubblesEffectParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (boilingAudioSource != null && boilingAudioSource.isPlaying) boilingAudioSource.Stop();
    }

    void PlayBoilingSoundLoop()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(boilingLoopSoundName) && boilingAudioSource != null)
        {
            Sound soundToPlay = AudioManager.Instance.sounds.Find(s => s.name == boilingLoopSoundName);
            if (soundToPlay != null && soundToPlay.clip != null)
            {
                boilingAudioSource.clip = soundToPlay.clip;
                boilingAudioSource.volume = soundToPlay.volume * PlayerPrefs.GetFloat("SFXVolume", 1.0f); // SFX 볼륨 적용
                boilingAudioSource.mute = !AudioManager.Instance.IsSfxEnabled; // SFX 활성화 여부
                boilingAudioSource.loop = true;
                if (!boilingAudioSource.isPlaying && gameObject.activeInHierarchy)
                {
                     boilingAudioSource.Play();
                }
            }
        }
    }

    IEnumerator MoveIndicatorCoroutine()
    {
        float barMinX = -timingBarWidth / 2f;
        float barMaxX = timingBarWidth / 2f;

        while (isTimingChallengeActive)
        {
            currentIndicatorPositionX += indicatorDirection * indicatorSpeed * Time.deltaTime;
            if (currentIndicatorPositionX >= barMaxX) { currentIndicatorPositionX = barMaxX; indicatorDirection = -1; }
            else if (currentIndicatorPositionX <= barMinX) { currentIndicatorPositionX = barMinX; indicatorDirection = 1; }
            if (timingBarIndicator != null) timingBarIndicator.rectTransform.anchoredPosition = new Vector2(currentIndicatorPositionX, timingBarIndicator.rectTransform.anchoredPosition.y);
            yield return null;
        }
    }

    public void OnClickTimingButton()
    {
        if (!isTimingChallengeActive) return;
        isTimingChallengeActive = false;
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
        if (clickButton != null) clickButton.gameObject.SetActive(false);

        float successMinX = currentSuccessZonePosX - currentSuccessZoneWidthActual / 2f;
        float successMaxX = currentSuccessZonePosX + currentSuccessZoneWidthActual / 2f;
        bool currentClickSuccess = (currentIndicatorPositionX >= successMinX && currentIndicatorPositionX <= successMaxX);

        if (currentClickSuccess)
        {
            Debug.Log($"타이밍 성공! (도전 {currentTimingChallengeNumber}/{TOTAL_TIMING_CHALLENGES})");
            if (successSparkleImage != null) StartCoroutine(ShowSuccessSparkle());
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(timingSuccessSoundName)) AudioManager.Instance.PlayOneShotSound(timingSuccessSoundName);

            if (currentTimingChallengeNumber >= TOTAL_TIMING_CHALLENGES)
            {
                currentInductionStep = InductionStep.AllStagesComplete;
                UpdateInductionVisual(); // 성공 시 인덕션 상태 유지 또는 특별한 완료 상태로 변경
                Debug.Log("설탕 끓이기 모든 단계 최종 성공!");
                if (resultImageDisplay != null && clearImageSprite != null)
                {
                    resultImageDisplay.sprite = clearImageSprite;
                    resultImageDisplay.gameObject.SetActive(true);
                }
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(allStagesSuccessSoundName)) AudioManager.Instance.PlayOneShotSound(allStagesSuccessSoundName);
                StartCoroutine(HandleEndOfGameSequence(1.5f, true));
            }
            else
            {
                // 현재 단계 성공, 다음 인덕션 단계로 (짧은 딜레이 후)
                StartCoroutine(ProceedToNextTimingChallengeAfterDelay(0.5f));
            }
        }
        else
        {
            Debug.Log($"타이밍 실패! (도전 {currentTimingChallengeNumber}/{TOTAL_TIMING_CHALLENGES})");
            currentInductionStep = InductionStep.FailedCurrentStage;
            UpdateInductionVisual(); // 인덕션 OFF 및 효과 중지
            if (resultImageDisplay != null && failImageSprite != null)
            {
                resultImageDisplay.sprite = failImageSprite;
                resultImageDisplay.gameObject.SetActive(true);
            }
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(timingFailureSoundName)) AudioManager.Instance.PlayOneShotSound(timingFailureSoundName);
            
            if (!isTutorialMode)
            {
                if (HeartManager.Instance != null) HeartManager.Instance.LoseHeart();
            } else {
                Debug.Log("튜토리얼 중이므로 하트가 차감되지 않았습니다.");
            }
            StartCoroutine(HandleEndOfGameSequence(1.5f, false)); // 실패 시 재시도 또는 게임오버 처리
        }
    }

    IEnumerator ShowSuccessSparkle()
    {
        if (successSparkleImage != null)
        {
            successSparkleImage.gameObject.SetActive(true);
            float duration = 0.5f;
            float timer = 0;
            Color originalColor = successSparkleImage.color;
            
            // Fade In
            while(timer < duration / 2)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, originalColor.a, timer / (duration / 2));
                successSparkleImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            // Fade Out
            timer = 0;
             while(timer < duration / 2)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(originalColor.a, 0, timer / (duration / 2));
                successSparkleImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            successSparkleImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
            successSparkleImage.gameObject.SetActive(false);
        }
    }

    IEnumerator ProceedToNextTimingChallengeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        PrepareAndStartNextTimingChallenge();
    }

    IEnumerator HandleEndOfGameSequence(float delay, bool wasAllSuccessful)
    {
        yield return new WaitForSeconds(delay);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        
        if (wasAllSuccessful)
        {
            StopVisualEffects(); // 최종 성공 시 모든 끓는 효과 중지
            GoToNextMajorStage();
        }
        else // 실패했고, 재시도 또는 게임오버
        {
            // 실패 시에는 이미 UpdateInductionVisual(InductionStep.FailedCurrentStage)에서 효과 중지됨
            bool canRetry = true; // 실제로는 HeartManager의 현재 하트 수로 판단
            if (HeartManager.Instance != null)
            {
                // 예시: canRetry = HeartManager.Instance.GetCurrentHearts() > 0;
                // HeartManager에 GetCurrentHearts() 같은 public int 함수 구현 필요
            }

            if (isTutorialMode || canRetry)
            {
                Debug.Log(isTutorialMode ? "튜토리얼 전체 실패. 설탕 끓이기 처음부터 재시도합니다." : "전체 실패. 설탕 끓이기 처음부터 재시도합니다.");
                currentTimingChallengeNumber = 0;
                currentInductionStep = InductionStep.Initial; // 초기 상태로 리셋
                UpdateInductionVisual(); // 인덕션 끄기
                UpdateCurrentInductionStepText(); // UI 텍스트도 초기화
                StartCoroutine(ShowStageTitleAndStartFirstChallenge()); // 단계 안내부터 다시 시작
            }
            // else: 하트가 0이면 HeartManager에서 게임 오버 씬으로 자동 전환할 것임
        }
    }

    public void GoToNextMajorStage()
    {
        Debug.Log("GoToNextMajorStage: 다음 주요 게임 단계 (설탕 코팅)로 진행합니다.");
        string nextSceneName = null;
        if (CustomerOrderManager.Instance != null)
        {
            nextSceneName = CustomerOrderManager.Instance.sugarCoatingSceneName; //
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("CustomerOrderManager에서 다음 씬 이름(sugarCoatingSceneName)을 가져올 수 없습니다. TitleScene으로 이동합니다.");
            LoadTitleScene();
            return;
        }

        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene(nextSceneName); //
        }
        else
        {
            Debug.LogError("SceneSwitcher.Instance is null. SceneManager를 사용하여 " + nextSceneName + " 로드 시도.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void LoadTitleScene()
    {
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene("TitleScene");
        }
        else
        {
            Debug.LogError("SceneSwitcher.Instance is null. SceneManager를 사용하여 TitleScene 로드 시도.");
            SceneManager.LoadScene("TitleScene");
        }
    }
}