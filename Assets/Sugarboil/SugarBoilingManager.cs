// SugarBoilingManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine.SceneManagement; // SceneManager 사용을 위해 추가

public class SugarBoilingManager : MonoBehaviour
{
    [Header("UI Elements - 연결 필수")]
    public Image timingBarIndicator;
    public RectTransform successZone;
    public Button clickButton;
    public Image resultImageDisplay; // 성공(Clear) 또는 실패(Fail) 이미지를 보여줄 UI Image
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

    [Header("Order Display UI - 주문서 표시용")]
    public GameObject orderDisplayPanelInSugarBoil; // 설탕 끓이기 씬 전용 주문서 패널
    public Transform orderFruitsContainerInSugarBoil; // 이 패널 안의 과일 아이콘 컨테이너
    public Image fruitOrderIconPrefabForSugarBoil; // 주문서에 표시될 과일 아이콘 프리팹
    public Image skewerStickIconPrefabForSugarBoil; // 주문서용 꼬치 아이콘 (선택적)

    [Header("Visual Effects - 연결 선택")]
    public Image inductionImage;
    public ParticleSystem panSmokeEffectParticle;   // 파티클 시스템으로 변경
    public ParticleSystem sugarBubblesEffectParticle; // 파티클 시스템으로 변경

    [Header("Asset Sprites - 연결 필수")]
    public Sprite inductionOffSprite;       //
    public Sprite inductionOnSprite;
    public Sprite inductionLevel1Sprite;
    public Sprite inductionLevel2Sprite;    // 2단계 스프라이트

    [Header("Timing Bar Settings")]
    public float indicatorSpeed = 200f;
    public float successZoneBaseWidth = 120f;
    public float timingBarWidth = 500f;

    [Header("Sounds - AudioManager에 등록된 이름")]
    public string successSoundName = "SugarSuccess";
    public string failureSoundName = "SugarFailure";
    public string boilingSoundName = "SugarBoilingLoop";
    public string inductionLevelUpSoundName = "InductionLevelUp"; // 인덕션 단계 변경 시 효과음 (선택)

    // 인덕션 단계 관련 변수
    private enum InductionState { OFF, ON, Level1, Level2, Cooldown }
    private InductionState currentInductionState = InductionState.OFF;
    private int successfulClicksInRow = 0; // 연속 성공 횟수 (0: ON, 1: Level1, 2: Level2 달성)
    private const int MAX_SUCCESSFUL_CLICKS = 3; // 총 3번 성공해야 최종 완료 (ON -> Level1 -> Level2)

    // 내부 변수들
    private bool isGameActive = false;
    private float currentIndicatorPositionX = 0f;
    private int indicatorDirection = 1;
    private float currentSuccessZonePosX;
    private float currentSuccessZoneWidthActual;
    private Coroutine gameLoopCoroutine;
    private AudioSource boilingAudioSource;
    private bool isTutorialMode = false;

    void Start()
    {
        boilingAudioSource = gameObject.AddComponent<AudioSource>();
        boilingAudioSource.playOnAwake = false;
        boilingAudioSource.loop = true;

        // UI 초기화
        if (clickButton != null) clickButton.gameObject.SetActive(false);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (successSparkleImage != null) successSparkleImage.gameObject.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (stageTitleText != null) stageTitleText.gameObject.SetActive(false);
        UpdateInductionVisual(InductionState.OFF); // 인덕션 OFF로 시작

        // 주문서 UI 표시
        DisplayCurrentOrderOnSugarBoilUI();

        // 튜토리얼 모드 확인
        if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.isTutorialActive) //
        {
            isTutorialMode = true;
            ShowTutorialUI();
        }
        else if (CustomerOrderManager.Instance == null && GameInfoHolder.CustomerIndexToLoad == 0) //
        {
            isTutorialMode = true;
            ShowTutorialUI();
            Debug.LogWarning("SugarBoilingManager: CustomerOrderManager.Instance is null. GameInfoHolder를 기반으로 튜토리얼 모드로 가정합니다.");
        }
        else
        {
            isTutorialMode = false;
            StartCoroutine(ShowStageTitleAndPrepareGame());
        }
    }

    void DisplayCurrentOrderOnSugarBoilUI()
    {
        if (CustomerOrderManager.Instance == null || CustomerOrderManager.Instance.CurrentOrderData == null)
        {
            if (orderDisplayPanelInSugarBoil != null) orderDisplayPanelInSugarBoil.SetActive(false);
            Debug.LogWarning("현재 주문 정보를 찾을 수 없어 주문서를 표시할 수 없습니다.");
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

        CustomerOrderData currentOrder = CustomerOrderManager.Instance.CurrentOrderData; //
        List<FruitType> fruitsInOrder = new List<FruitType>();
        if (currentOrder.skewerOrder != null)
        {
            foreach (var item in currentOrder.skewerOrder)
            {
                fruitsInOrder.Add(item.fruit); //
            }
        }
        // 이 단계에서는 토핑까지 보여줄지는 기획에 따라 결정 (현재는 기본 과일만)

        foreach (FruitType fruit in fruitsInOrder)
        {
            Sprite fruitSprite = GetSpriteForFruitTypeFromCustomerManager(fruit);
            if (fruitSprite != null)
            {
                Image icon = Instantiate(fruitOrderIconPrefabForSugarBoil, orderFruitsContainerInSugarBoil);
                icon.sprite = fruitSprite;
                icon.name = fruit.ToString() + "_OrderIcon";
            }
            else
            {
                Debug.LogWarning($"주문서 UI: {fruit}에 대한 스프라이트를 CustomerOrderManager에서 찾을 수 없습니다.");
            }
        }
    }

    // CustomerOrderManager에 있는 스프라이트 딕셔너리에서 스프라이트를 가져오는 헬퍼 함수
    Sprite GetSpriteForFruitTypeFromCustomerManager(FruitType fruitType)
    {
        if (CustomerOrderManager.Instance != null)
        {
            // CustomerOrderManager에 public Dictionary<FruitType, Sprite> fruitSpriteDic; 가 있고,
            // 또는 public Sprite GetSpriteForFruitUI(FruitType type) 같은 함수가 있어야 합니다.
            // 여기서는 CustomerOrderManager의 fruitSpritesForOrderUI 리스트를 직접 순회하는 예시를 보여드립니다.
            // 더 효율적인 방법은 CustomerOrderManager에 딕셔너리를 두고 접근하는 것입니다.
            if (CustomerOrderManager.Instance.fruitSpritesForOrderUI != null) //
            {
                foreach (var mapping in CustomerOrderManager.Instance.fruitSpritesForOrderUI) //
                {
                    if (mapping.fruitType == fruitType) return mapping.sprite; //
                }
            }
        }
        return null; // 못 찾으면 null 반환
    }


    void ShowTutorialUI()
    {
        if (tutorialPanel != null && tutorialStartButton != null && tutorialMessageText != null)
        {
            tutorialPanel.SetActive(true);
            tutorialMessageText.text = "설탕물이 알맞은 온도가 되도록\n타이밍에 맞춰 중앙의 버튼을 여러 번 클릭해 주세요!";
            tutorialStartButton.onClick.RemoveAllListeners();
            tutorialStartButton.onClick.AddListener(StartGameFromTutorialButton);
        }
        else
        {
            Debug.LogWarning("튜토리얼 UI 요소가 Inspector에 제대로 연결되지 않았습니다. 바로 게임을 시작합니다.");
            StartCoroutine(ShowStageTitleAndPrepareGame());
        }
    }

    public void StartGameFromTutorialButton()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        StartCoroutine(ShowStageTitleAndPrepareGame());
    }

    IEnumerator ShowStageTitleAndPrepareGame()
    {
        if (stageTitleText != null)
        {
            string stageNameText = "설탕 끓이기";
            if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.CurrentOrderData != null)
            {
                 // stageNameText = CustomerOrderManager.Instance.CurrentOrderData.customerName + " - 설탕 끓이기";
            }
            stageTitleText.text = stageNameText + " 단계";
            stageTitleText.gameObject.SetActive(true);
            yield return new WaitForSeconds(stageTitleDisplayDuration);
            stageTitleText.gameObject.SetActive(false);
        }
        PrepareAndStartNextTimingChallenge(); // 첫 번째 타이밍 도전 준비
    }

    void PrepareAndStartNextTimingChallenge()
    {
        InitializeGameSettingsForCurrentStage(); // 현재 인덕션 단계에 맞는 난이도 설정

        isGameActive = true;
        if (clickButton != null) clickButton.gameObject.SetActive(true);

        UpdateInductionVisual(currentInductionState); // 현재 인덕션 상태에 맞게 비주얼 업데이트

        if (currentInductionState != InductionState.OFF && currentInductionState != InductionState.Cooldown)
        {
            if (panSmokeEffectParticle != null && !panSmokeEffectParticle.isPlaying) panSmokeEffectParticle.Play();
            if (sugarBubblesEffectParticle != null && !sugarBubblesEffectParticle.isPlaying) sugarBubblesEffectParticle.Play();
            PlayBoilingSoundLoop();
        }
        
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
        gameLoopCoroutine = StartCoroutine(MoveIndicatorCoroutine());
    }

        void PlayBoilingSoundLoop()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(boilingSoundName) && boilingAudioSource != null)
        {
            Sound soundToPlay = AudioManager.Instance.sounds.Find(s => s.name == boilingSoundName);
            if (soundToPlay != null && soundToPlay.clip != null)
            {
                boilingAudioSource.clip = soundToPlay.clip;
                boilingAudioSource.volume = soundToPlay.volume * PlayerPrefs.GetFloat("SFXVolume", 1.0f);
                boilingAudioSource.mute = !AudioManager.Instance.IsSfxEnabled;
                boilingAudioSource.loop = true;
                if (!boilingAudioSource.isPlaying && gameObject.activeInHierarchy) // 오브젝트 활성화 상태에서만 재생
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

        while (isGameActive)
        {
            currentIndicatorPositionX += indicatorDirection * indicatorSpeed * Time.deltaTime;
            if (currentIndicatorPositionX >= barMaxX) { currentIndicatorPositionX = barMaxX; indicatorDirection = -1; }
            else if (currentIndicatorPositionX <= barMinX) { currentIndicatorPositionX = barMinX; indicatorDirection = 1; }
            if (timingBarIndicator != null) timingBarIndicator.rectTransform.anchoredPosition = new Vector2(currentIndicatorPositionX, timingBarIndicator.rectTransform.anchoredPosition.y);
            yield return null;
        }
    }

    void InitializeGameSettingsForCurrentStage()
    {
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (successSparkleImage != null) successSparkleImage.gameObject.SetActive(false);

        // 현재 인덕션 단계(successfulClicksInRow)와 튜토리얼 여부에 따라 난이도 조절
        float difficultyMultiplier = 1.0f; // 높을수록 어려워짐
        if (isTutorialMode)
        {
            difficultyMultiplier = 0.6f; // 튜토리얼은 매우 쉽게
            switch (successfulClicksInRow) { // 튜토리얼에서도 단계별 난이도 살짝 조정
                case 0: indicatorSpeed = 100f; currentSuccessZoneWidthActual = successZoneBaseWidth * 2.0f; break; // ON
                case 1: indicatorSpeed = 110f; currentSuccessZoneWidthActual = successZoneBaseWidth * 1.8f; break; // Level1
                case 2: indicatorSpeed = 120f; currentSuccessZoneWidthActual = successZoneBaseWidth * 1.6f; break; // Level2
                default: indicatorSpeed = 100f; currentSuccessZoneWidthActual = successZoneBaseWidth * 2.0f; break;
            }
        }
        else
        {
            int customerLevel = GameInfoHolder.CustomerIndexToLoad; // 0부터 시작
            switch (successfulClicksInRow)
            {
                case 0: // 첫 번째 타이밍 (ON)
                    indicatorSpeed = 180f + (customerLevel * 10f);
                    currentSuccessZoneWidthActual = successZoneBaseWidth - (customerLevel * 5f);
                    difficultyMultiplier = 1.0f;
                    break;
                case 1: // 두 번째 타이밍 (Level1)
                    indicatorSpeed = 200f + (customerLevel * 15f);
                    currentSuccessZoneWidthActual = successZoneBaseWidth * 0.8f - (customerLevel * 7f);
                    difficultyMultiplier = 1.2f;
                    break;
                case 2: // 세 번째 타이밍 (Level2)
                    indicatorSpeed = 220f + (customerLevel * 20f);
                    currentSuccessZoneWidthActual = successZoneBaseWidth * 0.6f - (customerLevel * 9f);
                    difficultyMultiplier = 1.4f;
                    break;
            }
        }
        currentSuccessZoneWidthActual = Mathf.Max(30f, currentSuccessZoneWidthActual); // 최소 너비 보장
        indicatorSpeed = Mathf.Max(100f, indicatorSpeed); // 최소 속도 보장


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

        // 효과는 PrepareAndStartNextTimingChallenge에서 제어
    }

    void UpdateInductionVisual(InductionState state)
    {
        if (inductionImage == null) return;

        currentInductionState = state; // 현재 인덕션 상태 업데이트
        switch (state)
        {
            case InductionState.OFF:
            case InductionState.Cooldown: // Cooldown 상태도 OFF와 동일하게 표시
                inductionImage.sprite = inductionOffSprite;
                if (panSmokeEffectParticle != null && panSmokeEffectParticle.isPlaying) panSmokeEffectParticle.Stop();
                if (sugarBubblesEffectParticle != null && sugarBubblesEffectParticle.isPlaying) sugarBubblesEffectParticle.Stop();
                if (boilingAudioSource != null && boilingAudioSource.isPlaying) boilingAudioSource.Stop();
                break;
            case InductionState.ON:
                inductionImage.sprite = inductionOnSprite;
                break;
            case InductionState.Level1:
                inductionImage.sprite = inductionLevel1Sprite;
                break;
            case InductionState.Level2:
                inductionImage.sprite = inductionLevel2Sprite;
                break;
        }
    }

    IEnumerator ShowSuccessSparkle()
    {
        if (successSparkleImage != null)
        {
            successSparkleImage.gameObject.SetActive(true);
            // 알파값을 0 -> 1 -> 0 으로 변화시키는 간단한 페이드인/아웃 효과
            float duration = 0.5f;
            float timer = 0;
            Color startColor = successSparkleImage.color;
            startColor.a = 0;
            successSparkleImage.color = startColor;

            // Fade In
            while(timer < duration / 2)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 1, timer / (duration / 2));
                successSparkleImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            // Fade Out
            timer = 0;
             while(timer < duration / 2)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, timer / (duration / 2));
                successSparkleImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            successSparkleImage.gameObject.SetActive(false);
        }
    }

    IEnumerator ProceedAfterDelay(float delay, bool currentStageSuccess, bool allStagesComplete)
    {
        yield return new WaitForSeconds(delay);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (successSparkleImage != null) successSparkleImage.gameObject.SetActive(false); // 반짝임도 여기서 확실히 끔

        if (currentStageSuccess)
        {
            if (allStagesComplete)
            {
                GoToNextMajorStage(); // 게임의 다음 주요 단계로 (예: 설탕 코팅)
            }
            else
            {
                // 다음 인덕션 단계 도전
                PrepareAndStartNextTimingChallenge();
            }
        }
        else // 현재 단계 실패
        {
            bool canRetry = true;
            // if (HeartManager.Instance != null) { canRetry = HeartManager.Instance.GetCurrentHearts() > 0; } // 실제 하트 체크

            if (isTutorialMode || canRetry)
            {
                Debug.Log(isTutorialMode ? "튜토리얼 실패. 현재 설탕 끓이기 단계 처음부터 재시도합니다." : "실패. 현재 설탕 끓이기 단계 처음부터 재시도합니다.");
                successfulClicksInRow = 0; // 실패했으므로 첫 인덕션 단계부터 다시 시작
                UpdateInductionVisual(InductionState.OFF); // 인덕션도 초기화
                StartCoroutine(ShowStageTitleAndPrepareGame()); // 단계 안내부터 다시
            }
            // else 게임 오버는 HeartManager가 처리
        }
    }

    public void GoToNextStage()
    {
        Debug.Log("GoToNextStage 호출됨. 다음 단계(설탕 코팅)로 진행합니다.");
        if (CustomerOrderManager.Instance != null && !string.IsNullOrEmpty(CustomerOrderManager.Instance.sugarCoatingSceneName))
        {
            if (SceneSwitcher.Instance != null)
            {
                SceneSwitcher.Instance.LoadScene(CustomerOrderManager.Instance.sugarCoatingSceneName); //
            }
            else
            {
                Debug.LogError("SceneSwitcher.Instance is null. SceneManager를 사용하여 " + CustomerOrderManager.Instance.sugarCoatingSceneName + " 로드 시도.");
                SceneManager.LoadScene(CustomerOrderManager.Instance.sugarCoatingSceneName);
            }
        }
        else
        {
            Debug.LogError("CustomerOrderManager.Instance 또는 sugarCoatingSceneName을 찾을 수 없습니다. TitleScene으로 이동합니다.");
            LoadTitleScene(); // 별도의 함수로 분리하여 호출
        }
    }

    public void GoToNextMajorStage() // SugarCoatingScene 등 다음 미니게임 씬으로
    {
        Debug.Log("GoToNextMajorStage 호출됨. 다음 주요 게임 단계로 진행합니다.");
        // ... (씬 전환 로직은 이전과 거의 동일, SceneSwitcher 사용) ...
    }

    void LoadTitleScene() // 타이틀 씬 로드 전용 함수
    {
        if (SceneSwitcher.Instance != null)
        {
            // TitleScene 이름은 고정값이거나, 다른 설정 파일/매니저에서 가져올 수 있습니다.
            SceneSwitcher.Instance.LoadScene("TitleScene");
        }
        else
        {
            Debug.LogError("SceneSwitcher.Instance is null. SceneManager를 사용하여 TitleScene 로드 시도.");
            SceneManager.LoadScene("TitleScene");
        }
    }
}