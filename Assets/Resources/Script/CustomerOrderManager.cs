// Assets/Resources/Script/CustomerOrderManager.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class CustomerOrderManager : MonoBehaviour
{
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    public static CustomerOrderManager Instance { get; private set; }

    [Header("게임 상태 및 튜토리얼")]
    public GameState currentGameState = GameState.Playing;
    public bool isTutorialActive = false;

    [Header("씬 이름 설정")]
    public string sugarBoilingSceneName = "SugarBoilingScene";
    public string sugarCoatingSceneName = "SugarCoatingScene";
    public string toppingPlacementSceneName = "ToppingPlacementScene";
    public string stageSelectSceneName = "TitleScene";
    public string customerPresentationSceneName = "CustomerPresentationScene";
    public string fruitCatchingSceneName = "FruitCatchingGameScene"; // FruitCatchingGameScene 이름 명시

    [Header("배경음악 이름 (AudioManager 등록)")]
    public string normalStageBgmName = "MainGameBGM"; // 일반 스테이지 BGM 이름
    public string finalStageBgmName = "FinalStage"; // 마지막 스테이지 BGM 이름


    [Header("데이터 에셋")]
    public List<CustomerOrderData> allCustomerOrders;
    public List<FruitSpriteMapping> fruitSpritesForOrderUI;

    // { get; private set; } -> 외부에서는 읽기만 가능하고, 이 클래스 내부에서만 값을 바꿀 수 있습니다.
    public CustomerOrderData CurrentOrderData { get; private set; }

    public List<FruitType> CurrentRequiredSkewerFruits { get; private set; } = new List<FruitType>();
    public int currentCustomerIndex { get; private set; } = 0;

    private Dictionary<FruitType, Sprite> fruitSpriteDic;
    public event Action OnOrderLoaded;
    public event Action<GameState, bool> OnGameStateChanged;

    private const string TUTORIAL_COMPLETED_PREF_KEY = "PandaTanghulu_TutorialCompleted";

    [System.Serializable]
    public struct FruitSpriteMapping
    {
        public FruitType fruitType;
        public Sprite sprite;
    }

    public bool IsGamePaused
    {
        get
        {
            // 튜토리얼이 활성화 상태이고, 현재 게임 상태가 '튜토리얼 표시' 상태일 때 true를 반환합니다.
            return isTutorialActive && currentGameState == GameState.TutorialDisplay;
        }
    }

    void Awake()
    {
        Debug.Log("CustomerOrderManager Awake() 호출됨");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSpriteDictionary();
            SceneManager.sceneLoaded += OnSceneLoaded; // Awake에서 구독
            Debug.Log("CustomerOrderManager: Awake에서 sceneLoaded 이벤트 구독.");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("이미 CustomerOrderManager 인스턴스가 존재하여 현재 오브젝트를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("CustomerOrderManager: OnDisable에서 sceneLoaded 이벤트 구독 해제.");
    }

    // Start 메서드는 비워두거나 간단한 초기화만 남깁니다.
    void Start()
    {
        Debug.Log("CustomerOrderManager Start() 호출됨. 현재 씬: " + SceneManager.GetActiveScene().name);
        // OnEnable에서 초기화 로직을 옮겼으므로, Start는 비워두거나
        // 정말 첫 실행 시에만 필요한 로직이 있다면 유지합니다.
        // 현재로서는 OnEnable에서 TitleScene일 때 초기화하도록 했으므로 대부분의 경우 여기서 추가 작업 불필요.
    }


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ★★★ [핵심 수정] 무한 모드일 때는 씬 로드 시 스테이지 데이터를 불러오지 않도록 막습니다.
        if (GameModeManager.IsEndlessMode)
        {
            Debug.Log("무한 모드이므로, OnSceneLoaded에서 스테이지 데이터 로딩을 건너뜁니다.");
            return;
        }

        // 스테이지 모드일 때만 이 로직을 실행합니다.
        currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad;
        SetupInitialGameState();
    }

    public void SetEndlessModeOrder(CustomerOrderData order)
    {
        CurrentOrderData = order;
        CurrentRequiredSkewerFruits.Clear();
        if (CurrentOrderData != null && CurrentOrderData.skewerOrder != null)
        {
            CurrentRequiredSkewerFruits.AddRange(order.skewerOrder.Select(item => item.fruit));
        }
        OnOrderLoaded?.Invoke();
        Debug.Log("무한 모드용 랜덤 주문 설정 완료: " + order.customerName);
    }


    void InitializeSpriteDictionary()
    {
        fruitSpriteDic = new Dictionary<FruitType, Sprite>();
        if (fruitSpritesForOrderUI != null)
        {
            foreach (var mapping in fruitSpritesForOrderUI)
            {
                if (mapping.sprite != null && !fruitSpriteDic.ContainsKey(mapping.fruitType))
                {
                    fruitSpriteDic.Add(mapping.fruitType, mapping.sprite);
                }
                else if (mapping.sprite == null)
                {
                    Debug.LogWarning($"FruitSpriteMapping: {mapping.fruitType}에 대한 Sprite가 할당되지 않았습니다.");
                }
            }
        }
        else
        {
            Debug.LogWarning("CustomerOrderManager: fruitSpritesForOrderUI 리스트가 할당되지 않았습니다.");
        }
    }

    void SetupInitialGameState()
    {
        Debug.Log($"CustomerOrderManager SetupInitialGameState - Current Scene: {SceneManager.GetActiveScene().name}, currentCustomerIndex: {currentCustomerIndex}");

        if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다! 게임을 진행할 수 없습니다.");
            SceneSwitcher.Instance?.LoadScene("TitleScene");
            return;
        }

        int tutorialCompletedPrefValue = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0);
        bool hasTutorialKey = PlayerPrefs.HasKey(TUTORIAL_COMPLETED_KEY);
        Debug.Log($"SetupInitialGameState - PlayerPrefs Check: TUTORIAL_COMPLETED_KEY HasKey = {hasTutorialKey}, Raw Value = {tutorialCompletedPrefValue}");

        bool isTutorialPermanentlyCompleted = tutorialCompletedPrefValue == 1;
        Debug.Log($"SetupInitialGameState - Interpreted: isTutorialPermanentlyCompleted = {isTutorialPermanentlyCompleted} (for customerIndex: {currentCustomerIndex})");

        // isTutorialActive 상태 설정
        if (currentCustomerIndex == 0 && !isTutorialPermanentlyCompleted)
        {
            isTutorialActive = true; // 현재 세션이 튜토리얼임을 명시
            SetGameState(GameState.TutorialDisplay); // 초기 상태는 튜토리얼 UI 표시
            Debug.Log($"SetupInitialGameState: TUTORIAL ACTIVATED for customer 0. isTutorialActive: {isTutorialActive}, GameState: {currentGameState}");
        }
        else
        {
            isTutorialActive = false; // 첫 번째 손님이 아니거나, 영구적으로 튜토리얼 완료됨
            SetGameState(GameState.Playing);
            if (currentCustomerIndex == 0 && isTutorialPermanentlyCompleted)
            {
                Debug.Log("SetupInitialGameState: Tutorial previously completed for customer 0. Starting normal play.");
            }
            else if (currentCustomerIndex != 0)
            {
                Debug.Log("SetupInitialGameState: Not customer 0, starting normal play.");
            }
        }
        LoadOrderForCurrentCustomer();
    }

    public void EndTutorialAndStartGame()
    {
        // 튜토리얼 UI의 "시작" 버튼 클릭 시 호출
        // isTutorialActive는 변경하지 않고 GameState만 Playing으로 변경하여 튜토리얼의 게임 플레이 부분 시작
        if (currentGameState == GameState.TutorialDisplay && isTutorialActive)
        {
            Debug.Log("튜토리얼 UI의 시작 버튼 클릭됨. GameState를 Playing으로 변경합니다. isTutorialActive는 계속 true 입니다.");
            SetGameState(GameState.Playing);
            // LoadOrderForCurrentCustomer(); // 필요시 여기서 주문 재로드 또는 스포너 시작 등을 명시적 호출 가능
            // 현재는 SetGameState 후 LoadOrderForCurrentCustomer가 이미 SetupInitialGameState의 일부로 호출됨.
            // 만약 FruitSpawner 로직이 GameState.Playing 상태에만 반응한다면, 여기서 추가 호출이 필요 없을 수 있음.
            // 명확성을 위해 FruitSpawner 시작을 여기서 직접 제어할 수도 있음.
            if (SceneManager.GetActiveScene().name == fruitCatchingSceneName && FruitSpawner2D.Instance != null)
            {
                FruitSpawner2D.Instance.StartSpawning(); // 튜토리얼 게임 플레이 시작 시 스포너 가동
            }
        }
        else
        {
            Debug.LogWarning($"EndTutorialAndStartGame 호출되었으나, 조건 불일치. currentGameState: {currentGameState}, isTutorialActive: {isTutorialActive}");
        }
    }

    void LoadOrderForCurrentCustomer()
    {
        if (currentCustomerIndex < 0 || currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.LogError($"CustomerOrderManager: 유효하지 않은 손님 인덱스({currentCustomerIndex})입니다. 최대 인덱스: {allCustomerOrders.Count - 1}");
            LoadTitleScene();
            return;
        }

        CurrentOrderData = allCustomerOrders[currentCustomerIndex];
        CurrentRequiredSkewerFruits.Clear();

        if (CurrentOrderData != null && CurrentOrderData.skewerOrder != null)
        {
            foreach (OrderItem item in CurrentOrderData.skewerOrder)
            {
                CurrentRequiredSkewerFruits.Add(item.fruit);
            }
            Debug.Log($"{CurrentOrderData.customerName} 손님의 주문 로드 완료. 필요한 꼬치 과일: {string.Join(", ", CurrentRequiredSkewerFruits.Select(f => f.ToString()))}");

        }
        else
        {
            Debug.LogError($"CustomerOrderManager: 손님 인덱스 {currentCustomerIndex}에 대한 주문 데이터를 찾을 수 없습니다!");
            LoadTitleScene(); // 예외 처리
            return;
        }

        if (CurrentOrderData.skewerOrder == null || CurrentOrderData.skewerOrder.Count == 0)
        {
            Debug.LogError($"CustomerOrderManager: 손님 '{CurrentOrderData.customerName}'의 꼬치 주문(skewerOrder) 데이터가 비어있습니다! 게임을 정상적으로 진행할 수 없습니다. TitleScene으로 이동합니다.");
            // 대화가 없는 손님이라도 최소한의 주문은 있어야 게임 진행이 가능합니다.
            // 또는, 이런 경우 특정 기본 주문으로 대체하거나 다른 처리를 할 수 있습니다.
            LoadTitleScene();
            return;
        }


        OnOrderLoaded?.Invoke();

        // FruitCatchingGameScene이고, 튜토리얼이 아니거나, 튜토리얼이 끝나고 게임 플레이 상태일 때 스포너 시작
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == fruitCatchingSceneName)
        { // FruitCatchingGameScene의 원래 파일 이름을 사용
            if (!isTutorialActive && currentGameState == GameState.Playing && FruitSpawner2D.Instance != null)
            {
                FruitSpawner2D.Instance.StartSpawning();
                Debug.Log("FruitSpawner2D 스폰 시작됨 (LoadOrderForCurrentCustomer).");
            }
            else if (isTutorialActive && FruitSpawner2D.Instance != null)
            {
                FruitSpawner2D.Instance.StopSpawningCompletely(); // 튜토리얼 중에는 확실히 스폰 중지
                Debug.Log("FruitSpawner2D 스폰 중지됨 (튜토리얼 중, LoadOrderForCurrentCustomer).");
            }
        }
    }

    public void LoadNextCustomerOrder()
    {
        currentCustomerIndex++;
        GameInfoHolder.CustomerIndexToLoad = currentCustomerIndex;

        if (currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.Log("모든 손님의 주문을 완료했습니다! 게임 완료 또는 다음 단계로...");
            // 현재는 타이틀 씬으로 돌아가도록 설정
            GameInfoHolder.OpenStageSelectPanelOnLoad = true; // 모든 손님 완료 후 스테이지 선택 창 자동 열기
            LoadTitleScene();
            return;
        }

        SetTutorialState(false); // 다음 손님은 무조건 튜토리얼 아님
        SetGameState(GameState.Playing);
        // LoadOrderForCurrentCustomer(); // OnSceneLoaded에서 호출됨
        // 다음 손님 주문 로드를 위해 ShopScene(DialogueScene)으로 먼저 이동
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadDialogueScene(allCustomerOrders[currentCustomerIndex].dialogueSequence.Count > 0 ? "ShopScene" : fruitCatchingSceneName); // 대화가 있으면 ShopScene, 없으면 바로 과일잡기로
        }
        else
        {
            SceneManager.LoadScene(allCustomerOrders[currentCustomerIndex].dialogueSequence.Count > 0 ? "ShopScene" : fruitCatchingSceneName);
        }
    }

    public bool CheckSkewerOrder(List<FruitType> collectedPlayerFruits)
    {
        if (CurrentOrderData == null || CurrentRequiredSkewerFruits.Count == 0)
        {
            Debug.LogWarning("CustomerOrderManager: 현재 유효한 주문이 없어 꼬치 순서를 확인할 수 없습니다.");
            return false;
        }

        bool orderMatch = collectedPlayerFruits.SequenceEqual(CurrentRequiredSkewerFruits);

        if (orderMatch)
        {
            Debug.Log($"과일 꽂기 성공! ({CurrentOrderData.customerName})");
            if (FruitSpawner2D.Instance != null) FruitSpawner2D.Instance.StopSpawningCompletely();

            Debug.Log($"{CurrentOrderData.customerName} 손님의 [{sugarBoilingSceneName}] 단계로 넘어갑니다.");
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(sugarBoilingSceneName);
            else SceneManager.LoadScene(sugarBoilingSceneName);
        }
        else
        {
            // ▼▼▼ 로그 추가 ▼▼▼
            Debug.Log($"과일 꽂기 실패! ({CurrentOrderData.customerName}). 현재 isTutorialActive 상태: {isTutorialActive} / currentGameState: {currentGameState}");
            // ▲▲▲ 로그 추가 ▲▲▲
            if (!isTutorialActive)
            {
                if (HeartManager.Instance != null) HeartManager.Instance.LoseHeart();
            }
            else
            {
                Debug.Log("튜토리얼 중이므로 과일 꽂기 실패 시 하트가 차감되지 않습니다.");
            }
        }
        return orderMatch;
    }

    public void AllMiniGamesCompletedForCurrentCustomer()
    {
        if (CurrentOrderData == null)
        {
            Debug.LogError("AllMiniGamesCompletedForCurrentCustomer: CurrentOrderData가 null입니다. 오류 발생.");
            LoadTitleScene();
            return;
        }

        Debug.Log($"{CurrentOrderData.customerName} 손님의 모든 탕후루 제작 단계 완료!");

        // ★★★ 무한 모드에서는 이 로직을 실행하지 않음 ★★★
        if (GameModeManager.IsEndlessMode)
        {
            if (EndlessModeController.Instance != null)
            {
                // CustomerPresentationScene으로 이동하는 대신, EndlessModeController에게 클리어 신호를 보냄
                EndlessModeController.Instance.CustomerCleared();
            }
            return; 
        }

        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.SetStageCleared(currentCustomerIndex);
        }

        if (currentCustomerIndex == 0) // 첫 번째 손님(튜토리얼) 완료 시
        {
            Debug.Log($"AllMiniGamesCompletedForCurrentCustomer: About to set TUTORIAL_COMPLETED_KEY. Current PlayerPrefs value: {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, -99)}");
            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1); // 튜토리얼 완료로 저장
            PlayerPrefs.Save();                           // 변경사항 저장

            int 확인용값 = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, -99);
            Debug.Log($"첫 번째 손님(튜토리얼) 모든 미니게임 완료. TUTORIAL_COMPLETED_KEY를 1로 설정하고 저장했습니다. PlayerPrefs 저장 직후 확인된 값: {확인용값}");

            GameInfoHolder.TutorialWasJustCompleted = true; // 정적 플래그 설정

            if (확인용값 != 1)
            {
                Debug.LogError("CRITICAL PREFS ERROR: TUTORIAL_COMPLETED_KEY가 1로 저장되지 않았습니다! GameInfoHolder.TutorialWasJustCompleted 플래그에 의존합니다.");
            }
        }

        if (!string.IsNullOrEmpty(customerPresentationSceneName))
        {
            Debug.Log($"손님에게 전달하는 씬 ({customerPresentationSceneName})으로 이동합니다.");
            if (SceneSwitcher.Instance != null)
            {
                SceneSwitcher.Instance.LoadScene(customerPresentationSceneName);
            }
            else
            {
                SceneManager.LoadScene(customerPresentationSceneName);
            }
        }
        else
        {
            Debug.LogWarning("customerPresentationSceneName이 설정되지 않았습니다. 스테이지 선택 화면으로 이동합니다.");
            LoadTitleScene();
        }
    }

    public Sprite GetSpriteForFruitUI(FruitType fruitType)
    {
        if (fruitSpriteDic == null) InitializeSpriteDictionary();
        if (fruitSpriteDic != null && fruitSpriteDic.TryGetValue(fruitType, out Sprite sprite))
        {
            return sprite;
        }
        Debug.LogWarning($"CustomerOrderManager: UI용 {fruitType} 스프라이트를 찾을 수 없습니다.");
        return null;
    }

    // GameState를 설정하고 이벤트를 발생시키는 함수
    public void SetGameState(GameState newState)
    {
        // Debug.Log($"CustomerOrderManager: SetGameState 시도. Current: {currentGameState}, New: {newState}, isTutorialActive: {isTutorialActive}");
        // 튜토리얼 상태 결정 로직은 SetupInitialGameState에 집중되어 있으므로, 여기서는 GameState 변경만 처리
        // bool tutorialFlagForEvent = isTutorialActive; // isTutorialActive는 SetupInitialGameState에서 이미 결정됨

        // if (currentGameState != newState || isTutorialActive != tutorialFlagForEvent) // isTutorialActive 변경 여부도 조건에 포함했었으나, GameState 변경만으로도 이벤트 발생 가능
        if (currentGameState != newState)
        {
            currentGameState = newState;
            // isTutorialActive 값은 CustomerOrderManager의 현재 멤버 변수 값을 사용
            OnGameStateChanged?.Invoke(currentGameState, isTutorialActive);
            Debug.Log($"CustomerOrderManager: GameState 변경됨 -> {currentGameState}, isTutorialActive (전달값) -> {isTutorialActive}");
        }
    }

    // isTutorialActive 상태를 설정하고 이벤트를 발생시키는 함수
    public void SetTutorialState(bool tutorialActiveState)
    {
        if (isTutorialActive != tutorialActiveState)
        {
            isTutorialActive = tutorialActiveState;
            // isTutorialActive가 변경되면, 연관된 게임 상태도 변경될 수 있으므로 OnGameStateChanged 호출
            OnGameStateChanged?.Invoke(currentGameState, isTutorialActive);
            Debug.Log($"CustomerOrderManager: isTutorialActive 상태가 명시적으로 변경됨 -> {isTutorialActive} (SetTutorialState 직접 호출)");
        }
    }


    public void ProceedToNextMiniGameStep()
    {
        if (CurrentOrderData == null)
        {
            Debug.LogError("ProceedToNextMiniGameStep: CurrentOrderData is null. Cannot proceed.");
            LoadTitleScene();
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        string nextSceneToLoad = "";

        if (currentScene == fruitCatchingSceneName) // 과일 잡기 완료 후
        {
            nextSceneToLoad = sugarBoilingSceneName;
        }
        else if (currentScene == sugarBoilingSceneName)
        {
            nextSceneToLoad = sugarCoatingSceneName;
        }
        else if (currentScene == sugarCoatingSceneName)
        {
            if (!string.IsNullOrEmpty(toppingPlacementSceneName) && CurrentOrderData.toppingItem != FruitType.None)
            {
                nextSceneToLoad = toppingPlacementSceneName;
            }
            else
            {
                AllMiniGamesCompletedForCurrentCustomer();
                return;
            }
        }
        else if (currentScene == toppingPlacementSceneName)
        {
            AllMiniGamesCompletedForCurrentCustomer();
            return;
        }
        else
        {
             // ★★★ [수정] 무한 모드에서 ShopScene -> FruitCatchingGameScene 이동 처리 ★★★
            if (GameModeManager.IsEndlessMode && currentScene == "ShopScene")
            {
                nextSceneToLoad = fruitCatchingSceneName;
            }
            else
            {
                Debug.LogError($"ProceedToNextMiniGameStep: 현재 씬({currentScene})에서 다음 단계를 결정할 수 없습니다.");
                LoadTitleScene();
                return;
            }
        }

        if (!string.IsNullOrEmpty(nextSceneToLoad))
        {
            Debug.Log($"다음 미니게임 단계로 진행: {nextSceneToLoad}");
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(nextSceneToLoad);
            else SceneManager.LoadScene(nextSceneToLoad);
        }
    }

    private void LoadTitleScene()
    {
        // TitleScene으로 돌아갈 때 스테이지 선택 패널이 열리도록 설정
        GameInfoHolder.OpenStageSelectPanelOnLoad = true;
        if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(stageSelectSceneName);
        else SceneManager.LoadScene(stageSelectSceneName);
    }
    // 외부에서 현재 주문을 설정하는 필수 함수
    public void SetCurrentOrder(int customerIndex)
    {
        if (customerIndex < 0 || customerIndex >= allCustomerOrders.Count)
        {
            Debug.LogError($"SetCurrentOrder: 잘못된 인덱스({customerIndex})입니다.");
            return;
        }

        currentCustomerIndex = customerIndex;
        CurrentOrderData = allCustomerOrders[customerIndex];

        // 주문에 필요한 과일 목록 초기화 및 설정
        CurrentRequiredSkewerFruits.Clear();
        if (CurrentOrderData != null)
        {
            // [기존 오류 코드]
            // CurrentRequiredSkewerFruits.AddRange(CurrentOrderData.fruitsInOrder);

            // [최종 수정 코드]
            // skewerOrder 리스트에 있는 각 OrderItem에서 fruit 정보만 추출하여 추가합니다.
            foreach (OrderItem item in CurrentOrderData.skewerOrder)
            {
                CurrentRequiredSkewerFruits.Add(item.fruit);
            }
        }
        
        // BGM 재생 로직 (이 부분은 그대로 유지)
        if (AudioManager.Instance != null)
        {
            if (customerIndex >= allCustomerOrders.Count - 1)
            {
                AudioManager.Instance.PlayBgm(finalStageBgmName);
            }
            else
            {
                AudioManager.Instance.PlayBgm(normalStageBgmName);
            }
        }
    }

    // 현재 스테이지 인덱스를 외부에서 알 수 있게 하는 함수
    public int GetCurrentCustomerIndex()
    {
        return currentCustomerIndex;
    }
}