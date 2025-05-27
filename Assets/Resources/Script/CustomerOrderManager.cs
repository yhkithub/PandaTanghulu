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

    [Header("데이터 에셋")]
    public List<CustomerOrderData> allCustomerOrders;
    public List<FruitSpriteMapping> fruitSpritesForOrderUI;

    public CustomerOrderData CurrentOrderData { get; private set; }
    public List<FruitType> CurrentRequiredSkewerFruits { get; private set; } = new List<FruitType>();
    public int currentCustomerIndex { get; private set; } = 0;

    private Dictionary<FruitType, Sprite> fruitSpriteDic;
    public event Action OnOrderLoaded;
    public event Action<GameState, bool> OnGameStateChanged;

    [System.Serializable]
    public struct FruitSpriteMapping
    {
        public FruitType fruitType;
        public Sprite sprite;
    }

    void Awake()
    {
        Debug.Log("CustomerOrderManager Awake() 호출됨");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSpriteDictionary();
        }
        else if (Instance != this)
        {
            Debug.LogWarning("이미 CustomerOrderManager 인스턴스가 존재하여 현재 오브젝트를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
    }

    // Start 대신 OnEnable/OnDisable에서 씬 로드 이벤트 구독/해제
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // 게임 시작 시 한 번만 초기화 (혹은 첫 씬에서만)
        if (SceneManager.GetActiveScene().name == "TitleScene") // 예시: TitleScene에서 처음 시작될 때만 초기화
        {
             currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad;
             Debug.Log($"CustomerOrderManager: 활성화 시 GameInfoHolder로부터 로드할 손님 인덱스: {currentCustomerIndex}");
             SetupInitialGameState();
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
        Debug.Log($"CustomerOrderManager: Scene '{scene.name}' loaded. Re-evaluating game state.");
        currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad; // 항상 GameInfoHolder 값으로 동기화
        SetupInitialGameState(); // PlayerPrefs를 다시 읽어 튜토리얼 상태 등을 설정
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
        // currentCustomerIndex는 OnSceneLoaded 또는 Start에서 GameInfoHolder를 통해 이미 설정됨
        Debug.Log($"CustomerOrderManager SetupInitialGameState: 현재 손님 인덱스: {currentCustomerIndex}");

        if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다! 게임을 진행할 수 없습니다.");
            LoadTitleScene();
            return;
        }

        bool isTutorialAlreadyCompleted = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        Debug.Log($"SetupInitialGameState: isTutorialAlreadyCompleted = {isTutorialAlreadyCompleted} (currentCustomerIndex: {currentCustomerIndex})");

        if (currentCustomerIndex == 0 && !isTutorialAlreadyCompleted)
        {
            SetTutorialState(true);
            SetGameState(GameState.TutorialDisplay);
            Debug.Log("튜토리얼 모드입니다. 첫 번째 손님의 주문 데이터를 로드합니다.");
            LoadOrderForCurrentCustomer();

            // FruitCatchingGameScene에서만 스포너를 특별히 제어하는 로직은 LoadOrderForCurrentCustomer 내부에서 처리
        }
        else
        {
            SetTutorialState(false);
            SetGameState(GameState.Playing);
            LoadOrderForCurrentCustomer();
        }
    }

    public void EndTutorialAndStartGame()
    {
        if (currentGameState == GameState.TutorialDisplay && isTutorialActive)
        {
            Debug.Log("튜토리얼 버튼 클릭됨. 게임을 시작합니다!");
            SetTutorialState(false);
            SetGameState(GameState.Playing);

            // PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1); // 여기서 바로 완료 처리하지 않음
            // PlayerPrefs.Save();
            // Debug.Log("튜토리얼 완료 상태 저장됨."); // 아직 저장 안 함

            LoadOrderForCurrentCustomer();
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
            Debug.LogError($"CustomerOrderManager: 손님 인덱스 {currentCustomerIndex}에 대한 주문 데이터 또는 꼬치 주문 내용이 없습니다.");
            LoadTitleScene();
            return;
        }

        OnOrderLoaded?.Invoke();

        // FruitCatchingGameScene이고, 튜토리얼이 아니거나, 튜토리얼이 끝나고 게임 플레이 상태일 때 스포너 시작
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == fruitCatchingSceneName) { // FruitCatchingGameScene의 원래 파일 이름을 사용
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
            Debug.Log($"과일 꽂기 실패! ({CurrentOrderData.customerName})");
            if (HeartManager.Instance != null) HeartManager.Instance.LoseHeart(); // 하트 차감은 HeartManager 내부에서 튜토리얼 여부 판단

            // 실패 시 꼬치 비우기는 SkewerManager에서 하도록 유도하거나, 여기서 직접 호출.
            // SkewerManager.Instance?.ClearSkewer(); // 필요시 호출
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

        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.SetStageCleared(currentCustomerIndex);
        }

        // 튜토리얼 손님(첫 번째 손님)의 모든 미니게임을 완료했다면, 여기서 튜토리얼 완료 상태 저장
        if (currentCustomerIndex == 0)
        {
            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("첫 번째 손님(튜토리얼) 완료. TUTORIAL_COMPLETED_KEY 저장됨.");
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

    public void SetGameState(GameState newState)
    {
        if (currentGameState != newState || isTutorialActive != (newState == GameState.TutorialDisplay && currentCustomerIndex == 0 && PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 0)) // 튜토리얼 상태도 함께 고려
        {
            currentGameState = newState;
            // isTutorialActive는 SetupInitialGameState에서 결정된 값을 따르도록 함
            OnGameStateChanged?.Invoke(currentGameState, isTutorialActive);
            Debug.Log($"CustomerOrderManager: GameState 변경됨 -> {currentGameState}, isTutorialActive -> {isTutorialActive}");
        }
    }

    public void SetTutorialState(bool tutorialActiveState) // 함수 이름 변경 및 역할 명확화
    {
        if (isTutorialActive != tutorialActiveState)
        {
            isTutorialActive = tutorialActiveState;
            OnGameStateChanged?.Invoke(currentGameState, isTutorialActive);
            Debug.Log($"CustomerOrderManager: TutorialActive 상태 명시적 변경됨 -> {isTutorialActive}");
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
            Debug.LogError($"ProceedToNextMiniGameStep: 현재 씬({currentScene})에서 다음 단계를 결정할 수 없습니다.");
            LoadTitleScene();
            return;
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
}