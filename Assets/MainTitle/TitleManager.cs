// Assets/MainTitle/TitleManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("기본 UI 요소")]
    public GameObject newGameButton;
    public GameObject stageSelectButton;
    public GameObject settingsButton;
    public GameObject settingsPanel;
    public Image logoImage;
    public float logoFadeDuration = 1f;
    public string buttonclickSoundName = "button";

    [Header("무한 모드")]
    public GameObject endlessModeTrigger;

    [Header("과일 롤 애니메이션 설정")]
    public List<FruitPrefabChanceForRoll> fruitPrefabsWithChanceForRoll;
    public Transform fruitRollStartPositionLeft;
    public Transform fruitRollStartPositionRight;
    public float fruitRollSpeed = 5f;
    public float fruitSpawnInterval = 0.3f;
    public float[] targetYPositions = new float[] { 4.5f, 3.5f, 2.5f, 1.5f, 0.5f, -0.5f, -1.5f, -2.5f, -3.5f, -4.5f };

    [Header("과일 롤 트레일 상세 설정")]
    public float trailTime = 8f;
    public float fixedTrailWidth = 1.0f;


    [Header("씬 이름 설정")]
    public string prologueSceneName = "StoryScene"; // StoryScene으로 변경
    public string dialogueSceneName = "ShopScene"; // DialogueScene 대신 ShopScene 사용

    [Header("스테이지 선택 UI")]
    public GameObject stageSelectPanel_UI;
    public Button stageButtonPrefab_UI;
    public Transform stageButtonContainer_UI;
    public Sprite lockedStageSprite; // 현재 사용되지 않음 (CustomerOrderData의 customerSprite로 대체됨)

    [Header("오디오 설정 UI")]
    public Toggle bgmToggle;
    public Toggle sfxToggle;

    [Header("스테이지 정보 (CustomerOrderData 에셋들)")]
    public List<CustomerOrderData> customerOrderDataListForTitle;

    private const string GAME_STARTED_KEY = "GameStarted";
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted"; // CustomerOrderManager와 동일하게
    private const string BGM_KEY = "BGM";
    private const string SFX_KEY = "SFX";
    private Color initialLogoColor;

    [Header("새로하기 확인 UI")]
    public GameObject confirmNewGamePanel_UI;

    [Header("배경 애니메이션 관련 변수")]
    public SpriteRenderer background; // 배경 이미지를 표시할 SpriteRenderer 컴포넌트
    public Sprite[] beforeClearSprites; // 클리어 전 배경 이미지 배열
    public Sprite[] afterClearSprites; // 클리어 후 배경 이미지 배열
    public float frameRate = 0.2f; // 이미지 전환 속도

    private KeyCode[] secretCode = { KeyCode.Return, KeyCode.V, KeyCode.K, KeyCode.T, KeyCode.M, KeyCode.X, KeyCode.P, KeyCode.F }; // 비밀번호: VKTMXPF
    private int secretCodeIndex = 0;

    [System.Serializable]
    public struct FruitPrefabChanceForRoll
    {
        public GameObject prefab;
        public int minCount;
        public float chanceWeight;
    }

    void Start()
    {
        GameModeManager.ResetMode(); // 무한 모드 상태를 초기화합니다.
        // 비밀 키 인덱스 초기화
        secretCodeIndex = 0;
        // 타이틀 매니저 시작 시 배경 애니메이션 코루틴 시작
        StartCoroutine(AnimateBackground());

        void PlayClickSound()
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
            {
                AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
            }
        }
        
        Debug.Log($"TitleManager Start: TUTORIAL_COMPLETED_KEY = {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0)}");
        if (AudioManager.Instance != null)
        {
            // AudioManager에 "MainTitleBGM"이라는 이름으로 등록된 사운드를 재생
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.PlayBgm("MainTitleBGM");
        }

        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(true);
            initialLogoColor = logoImage.color;
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 1f);
        }
        else { Debug.LogError("TitleManager: 로고 이미지가 할당되지 않았습니다!"); }

        if (newGameButton != null) newGameButton.SetActive(true);
        if (newGameButton != null)
        {
            Button newGameBtnComponent = newGameButton.GetComponent<Button>();
            if (newGameBtnComponent != null)
            {
                newGameBtnComponent.onClick.AddListener(PlayClickSound);
            }
            else
            {
                Debug.LogWarning("settingsButton에 Button 컴포넌트가 없습니다.");
            }
        }

        if (settingsButton != null) settingsButton.SetActive(true);
        if (settingsButton != null)
        {
            Button settingsBtnComponent = settingsButton.GetComponent<Button>();
            if (settingsBtnComponent != null)
            {
                settingsBtnComponent.onClick.AddListener(PlayClickSound);
            }
            else
            {
                Debug.LogWarning("settingsButton에 Button 컴포넌트가 없습니다.");
            }
        }
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (stageSelectPanel_UI != null) stageSelectPanel_UI.SetActive(false);

        // GameInfoHolder의 플래그에 따라 스테이지 선택 패널 열기
        if (GameInfoHolder.OpenStageSelectPanelOnLoad)
        {
            OpenStageSelectPanel();
            GameInfoHolder.OpenStageSelectPanelOnLoad = false; // 플래그 리셋
        }

        bool tutorialCompletedByPrefs = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        bool justCompletedOverride = false;

        if (GameInfoHolder.TutorialWasJustCompleted)
        {
            Debug.Log("TitleManager Start: GameInfoHolder.TutorialWasJustCompleted is TRUE. 스테이지 버튼 표시를 위해 이 값을 우선합니다.");
            justCompletedOverride = true;
            GameInfoHolder.TutorialWasJustCompleted = false; // 플래그는 한 번 사용 후 리셋
        }

        Debug.Log($"TitleManager Start: TUTORIAL_COMPLETED_KEY (PlayerPrefs) = {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0)}, justCompletedOverride = {justCompletedOverride}");

        bool shouldShowStageButtons = tutorialCompletedByPrefs || justCompletedOverride;

        if (shouldShowStageButtons)
        {
            if (stageSelectButton != null) stageSelectButton.SetActive(true);
            if (stageSelectButton != null)
            {
                Button stageSelectBtnComponent = stageSelectButton.GetComponent<Button>();
                if (stageSelectBtnComponent != null)
                {
                    stageSelectBtnComponent.onClick.AddListener(PlayClickSound);
                }
                else
                {
                    Debug.LogWarning("stageSelectButton Button 컴포넌트가 없습니다.");
                }
            }
            Debug.Log("튜토리얼 완료 상태(또는 방금 완료됨)이므로 스테이지 선택 및 동물도감 버튼을 활성화합니다.");
        }
        else
        {
            if (stageSelectButton != null) stageSelectButton.SetActive(false);
            Debug.Log("튜토리얼 미완료 상태이므로 스테이지 선택 및 동물도감 버튼을 비활성화합니다.");
        }

        UpdateEndlessModeTriggerVisibility();
        
        LoadAudioSettings();
    }

    void Update()
    {
        // 비밀 키 입력 감지
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(secretCode[secretCodeIndex]))
            {
                secretCodeIndex++;
                if (secretCodeIndex >= secretCode.Length)
                {
                    UnlockEverything();
                    secretCodeIndex = 0; // 초기화
                }
            }
            else
            {
                secretCodeIndex = 0; // 순서가 틀리면 초기화
            }
        }
    }
    
    // 모든 것을 클리어 상태로 만드는 함수
    private void UnlockEverything()
    {
        if (StageDataManager.Instance != null)
        {
            Debug.Log("비밀 명령어 발동! 모든 스테이지를 클리어합니다.");

            // 튜토리얼 클리어 상태를 영구 저장
            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
            PlayerPrefs.Save(); // 변경사항을 기기에 즉시 저장
            Debug.Log($"TUTORIAL_COMPLETED_KEY를 1로 설정하고 저장했습니다. 현재 값: {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, -1)}");

            // 모든 스테이지를 클리어 처리
            for (int i = 0; i < StageDataManager.Instance.totalStages; i++)
            {
                StageDataManager.Instance.SetStageCleared(i);
            }

            // 스테이지 선택 버튼 활성화
            if (stageSelectButton != null) stageSelectButton.SetActive(true);


            StopAllCoroutines();
            StartCoroutine(AnimateBackground());

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOneShotSound("Success");
            }
            
            UpdateEndlessModeTriggerVisibility(); 
        }
    }

    private void UpdateEndlessModeTriggerVisibility()
    {
        if (endlessModeTrigger != null && StageDataManager.Instance != null)
        {
            bool allStagesCleared = StageDataManager.Instance.IsGameFullyCleared();
            endlessModeTrigger.SetActive(allStagesCleared);
            Debug.Log("무한 모드 트리거 활성화 상태: " + allStagesCleared);
        }
    }

    // 배경 애니메이션을 위한 코루틴
    IEnumerator AnimateBackground()
    {
        // StageDataManager 인스턴스가 로드될 때까지 잠시 대기
        yield return new WaitUntil(() => StageDataManager.Instance != null);

        Sprite[] selectedSprites = StageDataManager.Instance.IsGameFullyCleared() ? afterClearSprites : beforeClearSprites;
        int currentIndex = 0;

        while (true)
        {
            if (selectedSprites.Length > 0)
            {
                background.sprite = selectedSprites[currentIndex];
                currentIndex = (currentIndex + 1) % selectedSprites.Length;
            }
            yield return new WaitForSeconds(frameRate);
        }
    }


    public void StartNewGame()
    {
        Debug.Log("StartNewGame 버튼 클릭됨");
        // 이미 플레이한 기록(GAME_STARTED_KEY)이 있거나, 또는 튜토리얼을 완료한 기록이 있다면 확인 창을 띄웁니다.
        // 여기서는 GAME_STARTED_KEY를 기준으로 합니다. "새로하기"는 언제든 이전 기록을 지우는 개념이므로.
        if (PlayerPrefs.GetInt(GAME_STARTED_KEY, 0) == 1)
        {
            if (confirmNewGamePanel_UI != null)
            {
                Debug.Log("기존 플레이 기록 확인. 새로하기 확인 창 표시.");
                confirmNewGamePanel_UI.SetActive(true);
            }
            else
            {
                Debug.LogWarning("새로하기 확인 창(confirmNewGamePanel_UI)이 연결되지 않았습니다. 바로 초기화를 진행합니다.");
                ProceedWithNewGame();
            }
        }
        else
        {
            Debug.Log("첫 플레이로 간주하거나 GAME_STARTED_KEY 없음. 바로 새로하기 진행.");
            ProceedWithNewGame();
        }
    }

    public void ConfirmNewGame_Yes()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
        {
            AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
        }
        Debug.Log("새로하기 확인: 예");
        if (confirmNewGamePanel_UI != null) confirmNewGamePanel_UI.SetActive(false);
        ProceedWithNewGame();
    }

    public void ConfirmNewGame_No()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
        {
            AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
        }
        Debug.Log("새로하기 확인: 아니오");
        if (confirmNewGamePanel_UI != null) confirmNewGamePanel_UI.SetActive(false);
    }

    private void ProceedWithNewGame()
    {
        Debug.Log("ProceedWithNewGame 호출됨 - 실제 초기화 진행");
        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.ResetAllStageProgress();
            Debug.Log("스테이지 진행 상황 초기화 완료.");
        }
        else { Debug.LogError("StageDataManager 인스턴스가 없어 새로하기 시 진행 상황 초기화 불가!"); }

        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.InitializeHearts();
            Debug.Log("하트 상태 초기화 완료.");
        }
        else { Debug.LogError("HeartManager 인스턴스가 없어 새로하기 시 하트 초기화 불가!"); }

        // --- PlayerPrefs 초기화 부분 수정 ---
        Debug.Log($"ProceedWithNewGame: PlayerPrefs TUTORIAL_COMPLETED_KEY 현재 값 (변경 전): {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, -1)} (HasKey: {PlayerPrefs.HasKey(TUTORIAL_COMPLETED_KEY)})");

        PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 0); // 튜토리얼 미완료 상태로 명시적 설정
        PlayerPrefs.SetInt(GAME_STARTED_KEY, 1); // 게임 시작 기록
        PlayerPrefs.Save(); // 변경사항 즉시 저장
        Debug.Log($"ProceedWithNewGame: TUTORIAL_COMPLETED_KEY is now {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, -99)} after setting to 0 and saving.");
        Debug.Log($"ProceedWithNewGame: PlayerPrefs TUTORIAL_COMPLETED_KEY 값을 0으로 설정 후 저장. 현재 GetInt: {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, -1)}. GAME_STARTED_KEY: {PlayerPrefs.GetInt(GAME_STARTED_KEY, -1)}");
        // --- PlayerPrefs 초기화 부분 수정 끝 ---

        GameInfoHolder.CustomerIndexToLoad = 0;
        GameInfoHolder.OpenStageSelectPanelOnLoad = false;
        Debug.Log("GameInfoHolder.CustomerIndexToLoad를 0으로 설정.");

        if (newGameButton != null) newGameButton.SetActive(false);
        if (stageSelectButton != null) stageSelectButton.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(false);
        Debug.Log("타이틀 버튼 비활성화됨 (새로하기 진행 중).");

        StartCoroutine(TitleAnimationAndSceneLoad(prologueSceneName));
    }
    IEnumerator TitleAnimationAndSceneLoad(string sceneToLoadAfterAnimation)
    {
        Debug.Log("TitleAnimationAndSceneLoad 코루틴 시작. 로드할 씬: " + sceneToLoadAfterAnimation);

        if (logoImage != null && logoImage.gameObject.activeSelf)
        {
            float currentAlpha = logoImage.color.a;
            float timer = 0f;
            Debug.Log("로고 현재 위치에서 페이드 아웃 시작.");
            while (timer < logoFadeDuration)
            {
                logoImage.color = new Color(logoImage.color.r, logoImage.color.g, logoImage.color.b, Mathf.Lerp(currentAlpha, 0f, timer / logoFadeDuration));
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = new Color(logoImage.color.r, logoImage.color.g, logoImage.color.b, 0f);
            logoImage.gameObject.SetActive(false);
            Debug.Log("로고 현재 위치에서 페이드 아웃 완료.");
        }
        else
        {
            Debug.LogWarning("로고 이미지가 없거나 이미 비활성화되어 있어 페이드 아웃을 건너뜁니다.");
        }

        if (logoImage != null)
        {
            RectTransform logoRectTransform = logoImage.GetComponent<RectTransform>();
            if (logoRectTransform != null)
            {
                logoRectTransform.anchoredPosition = Vector2.zero;
                Debug.Log("로고를 Canvas 중앙으로 이동시킴.");
            }
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 0f);
            logoImage.gameObject.SetActive(true);
        }

        if (logoImage != null)
        {
            float timer = 0f;
            Debug.Log("로고 중앙에서 페이드 인 시작.");
            while (timer < logoFadeDuration)
            {
                logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, Mathf.Lerp(0f, initialLogoColor.a, timer / logoFadeDuration));
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = initialLogoColor;
            Debug.Log("로고 중앙에서 페이드 인 완료.");
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(RollFruitsAnimation());

        Debug.Log(sceneToLoadAfterAnimation + " 씬으로 전환합니다.");
        // SceneSwitcher 사용 권장
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene(sceneToLoadAfterAnimation);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoadAfterAnimation);
        }
    }

    IEnumerator RollFruitsAnimation()
    {
        if (fruitPrefabsWithChanceForRoll == null || fruitPrefabsWithChanceForRoll.Count == 0 ||
            fruitRollStartPositionLeft == null || fruitRollStartPositionRight == null || targetYPositions.Length == 0)
        {
            Debug.LogError("과일 롤 애니메이션에 필요한 설정이 부족합니다!");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        Debug.Log("과일 롤 애니메이션 시작.");
        List<GameObject> fruitsToRoll = GenerateFruitListForRoll(targetYPositions.Length);

        if (fruitsToRoll.Count == 0)
        {
            Debug.LogWarning("롤 애니메이션에 생성할 과일이 없습니다.");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        List<GameObject> generatedRolledFruits = new List<GameObject>();

        for (int i = 0; i < targetYPositions.Length; i++)
        {
            if (i >= fruitsToRoll.Count) break;

            GameObject fruitPrefabToRoll = fruitsToRoll[i];
            Vector3 spawnPos;
            Vector2 rollDirection;
            float currentY = targetYPositions[i];

            if (i % 2 == 0)
            {
                spawnPos = new Vector3(fruitRollStartPositionLeft.position.x, currentY, fruitRollStartPositionLeft.position.z);
                rollDirection = Vector2.right;
            }
            else
            {
                spawnPos = new Vector3(fruitRollStartPositionRight.position.x, currentY, fruitRollStartPositionRight.position.z);
                rollDirection = Vector2.left;
            }

            GameObject rolledFruit = Instantiate(fruitPrefabToRoll, spawnPos, Quaternion.identity);
            Rigidbody2D rb = rolledFruit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0;
                rb.linearVelocity = rollDirection * fruitRollSpeed; // velocity로 변경
            }
            AddTrailToFruit(rolledFruit);
            generatedRolledFruits.Add(rolledFruit);
            yield return new WaitForSeconds(fruitSpawnInterval);
        }

        float screenWorldWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
        float longestRollTime = (screenWorldWidth / fruitRollSpeed) + 2f;
        if (fruitRollSpeed <= 0) longestRollTime = 4f;

        yield return new WaitForSeconds(longestRollTime);

        // 과일 롤 애니메이션 후 생성된 과일들 제거 (선택적)
        // foreach (GameObject fruit in generatedRolledFruits)
        // {
        //     if (fruit != null) Destroy(fruit);
        // }
        Debug.Log("과일 롤 애니메이션 종료 및 생성된 과일 제거 완료.");
    }


    List<GameObject> GenerateFruitListForRoll(int totalFruitsToGenerate)
    {
        List<GameObject> fruitList = new List<GameObject>();
        if (fruitPrefabsWithChanceForRoll == null || fruitPrefabsWithChanceForRoll.Count == 0)
        {
            Debug.LogError("GenerateFruitListForRoll: fruitPrefabsWithChanceForRoll 리스트가 비어있습니다!");
            return fruitList;
        }

        Dictionary<GameObject, int> currentCounts = new Dictionary<GameObject, int>();

        foreach (var fruitInfo in fruitPrefabsWithChanceForRoll)
        {
            if (fruitInfo.prefab == null) continue;
            for (int i = 0; i < fruitInfo.minCount; i++)
            {
                if (fruitList.Count < totalFruitsToGenerate)
                {
                    fruitList.Add(fruitInfo.prefab);
                    if (currentCounts.ContainsKey(fruitInfo.prefab)) currentCounts[fruitInfo.prefab]++;
                    else currentCounts.Add(fruitInfo.prefab, 1);
                }
                else break;
            }
            if (fruitList.Count >= totalFruitsToGenerate) break;
        }

        while (fruitList.Count < totalFruitsToGenerate)
        {
            float totalWeight = 0f;
            List<KeyValuePair<GameObject, float>> weightedList = new List<KeyValuePair<GameObject, float>>();

            foreach (var fruitInfo in fruitPrefabsWithChanceForRoll)
            {
                if (fruitInfo.prefab == null) continue;
                totalWeight += fruitInfo.chanceWeight;
                weightedList.Add(new KeyValuePair<GameObject, float>(fruitInfo.prefab, fruitInfo.chanceWeight));
            }

            if (totalWeight <= 0 || weightedList.Count == 0)
            {
                if (fruitList.Count < totalFruitsToGenerate && fruitPrefabsWithChanceForRoll.Any(f => f.prefab != null))
                {
                    fruitList.Add(fruitPrefabsWithChanceForRoll.First(f => f.prefab != null).prefab);
                    continue;
                }
                break;
            }

            float randomPoint = Random.Range(0, totalWeight);
            float currentCumulativeWeight = 0f;
            GameObject selectedPrefab = null;

            foreach (var item in weightedList)
            {
                currentCumulativeWeight += item.Value;
                if (randomPoint < currentCumulativeWeight)
                {
                    selectedPrefab = item.Key;
                    break;
                }
            }

            if (selectedPrefab != null)
            {
                fruitList.Add(selectedPrefab);
            }
            else if (weightedList.Count > 0)
            {
                fruitList.Add(weightedList[Random.Range(0, weightedList.Count)].Key);
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < fruitList.Count; i++)
        {
            GameObject temp = fruitList[i];
            int randomIndex = Random.Range(i, fruitList.Count);
            fruitList[i] = fruitList[randomIndex];
            fruitList[randomIndex] = temp;
        }

        Debug.Log("생성될 과일 롤 목록 (" + fruitList.Count + "개)");
        return fruitList;
    }

    void AddTrailToFruit(GameObject fruitObject)
    {
        TrailRenderer trailRenderer = fruitObject.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = fruitObject.AddComponent<TrailRenderer>();
        }

        float actualTrailWidth = fixedTrailWidth;

        trailRenderer.time = trailTime;
        trailRenderer.startWidth = actualTrailWidth;
        trailRenderer.endWidth = actualTrailWidth;
        trailRenderer.minVertexDistance = 0.01f;
        trailRenderer.alignment = LineAlignment.View;

        if (trailRenderer.material == null || !trailRenderer.material.shader.name.Equals("Sprites/Default"))
        {
            Shader shaderToUse = Shader.Find("Sprites/Default");
            if (shaderToUse == null) shaderToUse = Shader.Find("Unlit/Color");

            if (shaderToUse != null)
            {
                trailRenderer.material = new Material(shaderToUse);
            }
            else
            {
                Debug.LogError(fruitObject.name + ": 트레일용 기본 쉐이더를 찾을 수 없습니다!");
                trailRenderer.emitting = false;
                return;
            }
        }

        Color trailColorToUse = Color.white;
        FruitColor fruitColorComponent = fruitObject.GetComponent<FruitColor>();

        if (fruitColorComponent != null)
        {
            trailColorToUse = fruitColorComponent.trailColor;
        }
        else
        {
            Debug.LogWarning(fruitObject.name + "에 FruitColor 컴포넌트가 없어 트레일 색상을 흰색으로 설정합니다.");
        }

        trailColorToUse.a = 1f;
        trailRenderer.startColor = trailColorToUse;
        trailRenderer.endColor = trailColorToUse;

        SpriteRenderer fruitSpriteRenderer = fruitObject.GetComponent<SpriteRenderer>();
        if (fruitSpriteRenderer != null)
        {
            trailRenderer.sortingLayerID = fruitSpriteRenderer.sortingLayerID;
            trailRenderer.sortingOrder = fruitSpriteRenderer.sortingOrder - 1;
        }
        else
        {
            trailRenderer.sortingOrder = -1;
        }
        trailRenderer.emitting = true;
    }

    public void OpenStageSelectPanel()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
        {
            AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
        }
        if (stageSelectPanel_UI != null)
        {
            stageSelectPanel_UI.SetActive(true);
            PopulateStageButtons();
        }
        else { Debug.LogError("StageSelectPanel_UI가 연결되지 않았습니다!"); }
    }

    public void CloseStageSelectPanel()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
        {
            AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
        }
        if (stageSelectPanel_UI != null)
        {
            stageSelectPanel_UI.SetActive(false);
        }
        else { Debug.LogError("StageSelectPanel_UI가 연결되지 않아 닫을 수 없습니다!"); }
    }

    void PopulateStageButtons()
    {
        void PlayClickSound()
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
            {
                AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
            }
        }

        if (stageButtonPrefab_UI == null || stageButtonContainer_UI == null || StageDataManager.Instance == null)
        {
            Debug.LogError("스테이지 버튼 생성에 필요한 요소가 설정되지 않았습니다.");
            return;
        }
        if (stageButtonPrefab_UI != null)
        {
            Button stageBtnComponent = stageButtonPrefab_UI.GetComponent<Button>();
            if (stageBtnComponent != null)
            {
                stageBtnComponent.onClick.AddListener(PlayClickSound);
            }
            else
            {
                Debug.LogWarning("settingsButton에 Button 컴포넌트가 없습니다.");
            }
        }

        foreach (Transform child in stageButtonContainer_UI)
        {
            Destroy(child.gameObject);
        }

        int numberOfStages = (customerOrderDataListForTitle != null) ? customerOrderDataListForTitle.Count : StageDataManager.Instance.totalStages;

        if (numberOfStages <= 0)
        {
            Debug.LogWarning("생성할 스테이지가 없습니다.");
            return;
        }

        for (int i = 0; i < numberOfStages; i++)
        {
            Button stageButtonInstance = Instantiate(stageButtonPrefab_UI, stageButtonContainer_UI);
            stageButtonInstance.name = "StageButton_" + (i + 1);

            Image buttonBackgroundImage = stageButtonInstance.GetComponent<Image>();
            Image characterIconImage = stageButtonInstance.transform.Find("CharacterIcon")?.GetComponent<Image>();
            Image lockIconImage = stageButtonInstance.transform.Find("LockIcon")?.GetComponent<Image>();
            TextMeshProUGUI buttonText = stageButtonInstance.GetComponentInChildren<TextMeshProUGUI>();

            int stageIndex = i;
            CustomerOrderData stageSpecificData = (customerOrderDataListForTitle != null && customerOrderDataListForTitle.Count > stageIndex) ? customerOrderDataListForTitle[stageIndex] : null;

            if (StageDataManager.Instance.IsStageUnlocked(stageIndex))
            {
                if (buttonText != null)
                {
                    buttonText.text = (stageSpecificData != null && !string.IsNullOrEmpty(stageSpecificData.customerName)) ? stageSpecificData.customerName : "스테이지 " + (stageIndex + 1);
                }

                if (characterIconImage != null && stageSpecificData != null && stageSpecificData.customerSprite != null)
                {
                    characterIconImage.sprite = stageSpecificData.customerSprite;
                    characterIconImage.color = Color.white;
                    characterIconImage.gameObject.SetActive(true);
                }
                else if (characterIconImage != null)
                {
                    characterIconImage.gameObject.SetActive(false);
                }

                if (lockIconImage != null) lockIconImage.gameObject.SetActive(false);

                if (buttonBackgroundImage != null)
                {
                    buttonBackgroundImage.color = StageDataManager.Instance.IsStageCleared(stageIndex) ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
                }

                stageButtonInstance.interactable = true;
                stageButtonInstance.onClick.AddListener(() => OnStageButtonClicked(stageIndex));
            }
            else // 잠긴 스테이지
            {
                if (buttonText != null) buttonText.text = "???";
                if (characterIconImage != null) characterIconImage.gameObject.SetActive(false);

                if (lockIconImage != null)
                {
                    if (stageSpecificData != null && stageSpecificData.customerSprite != null)
                    {
                        lockIconImage.sprite = stageSpecificData.customerSprite;
                        lockIconImage.color = new Color(0, 0, 0, 1); // 실루엣
                        lockIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                         lockIconImage.gameObject.SetActive(false);
                    }
                }

                if (buttonBackgroundImage != null) buttonBackgroundImage.color = new Color(0.4f, 0.4f, 0.4f, 1);
                stageButtonInstance.interactable = false;
            }
        }
    }

    void OnStageButtonClicked(int stageIndex)
    {
        string stageName = (customerOrderDataListForTitle != null && customerOrderDataListForTitle.Count > stageIndex && customerOrderDataListForTitle[stageIndex] != null)
                            ? customerOrderDataListForTitle[stageIndex].customerName
                            : "스테이지 " + (stageIndex + 1);
        Debug.Log(stageName + " (인덱스: " + stageIndex + ") 선택됨. GameInfoHolder.CustomerIndexToLoad 설정.");
        GameInfoHolder.CustomerIndexToLoad = stageIndex;
        GameInfoHolder.OpenStageSelectPanelOnLoad = false; // 다음 씬에서 바로 열리지 않도록

        if (SceneSwitcher.Instance != null)
        {
            // 선택된 스테이지가 0번(튜토리얼 가능성이 있는 첫 손님)이고, 튜토리얼이 아직 완료되지 않았다면 ShopScene(DialogueScene)으로,
            // 그렇지 않다면 바로 FruitCatchingGameScene으로 보낼 수 있습니다.
            // 또는 항상 ShopScene으로 보내서 CustomerDialogueManager가 튜토리얼 여부를 판단하도록 합니다.
            // 여기서는 항상 dialogueSceneName (ShopScene)으로 보내는 것으로 유지합니다.
            SceneSwitcher.Instance.LoadScene(dialogueSceneName);
        }
        else
        {
            SceneManager.LoadScene(dialogueSceneName);
        }
    }

    public void ToggleSettingsPanel()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonclickSoundName))
        {
            AudioManager.Instance.PlayOneShotSound(buttonclickSoundName);
        }
        if (settingsPanel != null)
        {
            bool isActive = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isActive);
            if (isActive) LoadAudioSettingsToUI();
        }
        else { Debug.LogError("Settings Panel이 할당되지 않았습니다!"); }
    }

    void LoadAudioSettings()
    {
        bool bgmOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        if (bgmToggle != null)
        {
            bgmToggle.onValueChanged.RemoveAllListeners();
            bgmToggle.isOn = bgmOn;
            bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
        }
        ApplyBGMSetting(bgmOn);

        bool sfxOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveAllListeners();
            sfxToggle.isOn = sfxOn;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }
        ApplySFXSetting(sfxOn);
    }

    void LoadAudioSettingsToUI()
    {
        if (bgmToggle != null) bgmToggle.isOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        if (sfxToggle != null) sfxToggle.isOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
    }

    public void OnBgmToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(BGM_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyBGMSetting(isOn);
    }

    public void OnSfxToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(SFX_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySFXSetting(isOn);
    }

    void ApplyBGMSetting(bool isOn)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetBgmEnabled(isOn);
    }

    void ApplySFXSetting(bool isOn)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxEnabled(isOn);
    }
}