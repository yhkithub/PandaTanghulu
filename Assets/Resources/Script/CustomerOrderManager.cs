// CustomerOrderManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // SequenceEqual 등 사용
using UnityEngine.SceneManagement; // SceneManager 사용
using System; // Action 이벤트 사용을 위해 추가


public class CustomerOrderManager : MonoBehaviour
{
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    public static CustomerOrderManager Instance { get; private set; }

    [Header("게임 상태 및 튜토리얼")]
    public GameState currentGameState = GameState.Playing; // 초기 상태 (EditorSceneInitializer 이후 결정될 수 있음)
    public bool isTutorialActive = false;

    [Header("씬 이름 설정")]
    public string sugarBoilingSceneName = "SugarBoilingScene";
    public string sugarCoatingSceneName = "SugarCoatingScene";
    public string toppingPlacementSceneName = "ToppingPlacementScene";
    public string stageSelectSceneName = "TitleScene"; // 또는 ShopScene 등 스테이지 선택 화면
    public string customerPresentationSceneName = "CustomerPresentationScene";
    // public string fruitCatchingSceneName = "FruitCatchingGameScene"; // 현재 씬이므로 불필요할 수 있음

    [Header("데이터 에셋")]
    public List<CustomerOrderData> allCustomerOrders; // Inspector에서 모든 손님 주문 ScriptableObject 연결
    public List<FruitSpriteMapping> fruitSpritesForOrderUI; // UI에 과일 스프라이트를 표시하기 위한 매핑 (딕셔너리 초기화용)

    // 현재 손님 및 주문 정보
    public CustomerOrderData CurrentOrderData { get; private set; }
    public List<FruitType> CurrentRequiredSkewerFruits { get; private set; } = new List<FruitType>(); // 꼬치에 꽂아야 할 과일 목록
    public int currentCustomerIndex { get; private set; } = 0; // 현재 손님 인덱스

    // 내부 사용 딕셔너리
    private Dictionary<FruitType, Sprite> fruitSpriteDic;

    // 이벤트 (UI 업데이트 등 다른 스크립트에서 구독 가능)
    public event Action OnOrderLoaded; // 새 주문이 로드되었을 때
    public event Action<GameState, bool> OnGameStateChanged; // 게임 상태 또는 튜토리얼 상태 변경 시

    [System.Serializable]
    public struct FruitSpriteMapping // UI 표시용 스프라이트 매핑 구조체
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
            InitializeSpriteDictionary(); // 스프라이트 딕셔너리 초기화
        }
        else if (Instance != this)
        {
            Debug.LogWarning("이미 CustomerOrderManager 인스턴스가 존재하여 현재 오브젝트를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Debug.Log("CustomerOrderManager Start() 호출됨");
        // EditorSceneInitializer에 의해 인스턴스가 미리 생성될 수 있으므로,
        // Start에서는 주로 게임 시작 시점의 로직을 처리합니다.
        // GameInfoHolder에서 초기 손님 인덱스를 가져와 설정합니다.
        currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad;
        Debug.Log($"CustomerOrderManager: 시작 시 GameInfoHolder로부터 로드할 손님 인덱스: {currentCustomerIndex}");

        SetupInitialGameState();
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

    // 초기 게임 상태 설정 및 손님 주문 로드
    void SetupInitialGameState()
    {
        currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad;
        Debug.Log($"CustomerOrderManager SetupInitialGameState: 현재 손님 인덱스: {currentCustomerIndex}");

        if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다! 게임을 진행할 수 없습니다.");
            LoadTitleScene(); // 예외 처리 강화
            return;
        }
        

        // PlayerPrefs에서 튜토리얼 완료 여부 확인
        if (PlayerPrefs.HasKey(TUTORIAL_COMPLETED_KEY))
        {
            Debug.Log($"SetupInitialGameState: '{TUTORIAL_COMPLETED_KEY}' 키 존재. 값: {PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY)}");
        }
        else
        {
            Debug.Log($"SetupInitialGameState: '{TUTORIAL_COMPLETED_KEY}' 키 존재하지 않음.");
        }
        bool isTutorialAlreadyCompleted = PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        Debug.Log($"SetupInitialGameState: isTutorialAlreadyCompleted = {isTutorialAlreadyCompleted} (currentCustomerIndex: {currentCustomerIndex})");

        if (currentCustomerIndex == 0 && !isTutorialAlreadyCompleted)
        {
            SetTutorialState(true);
            SetGameState(GameState.TutorialDisplay);
            Debug.Log("튜토리얼 모드입니다. 첫 번째 손님의 주문 데이터를 로드합니다.");
            LoadOrderForCurrentCustomer(); 

            // ★★★ 추가된 로직 시작 ★★★
            // 직접 테스트하는 씬들에서 튜토리얼 모드일 때도 첫 번째 주문 데이터를 로드하도록 함
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            if (currentSceneName == sugarBoilingSceneName ||
                currentSceneName == sugarCoatingSceneName ||
                currentSceneName == toppingPlacementSceneName ||
                currentSceneName == "FruitCatchingGameScene") // FruitCatchingGameScene도 포함할 수 있음
            {
                Debug.Log($"튜토리얼 모드에서 {currentSceneName} 직접 실행 감지. 첫 번째 주문 데이터를 로드합니다.");
                LoadOrderForCurrentCustomer(); // CurrentOrderData를 설정
            }
            // ★★★ 추가된 로직 끝 ★★★
            else if (FruitSpawner2D.Instance != null && SceneManager.GetActiveScene().name == "FruitCatchingGameScene") // 기존 과일 꽂기 튜토리얼 시작 시 스폰 중지 로직
            {
                FruitSpawner2D.Instance.StopSpawningCompletely();
                Debug.Log("FruitSpawner2D 스폰 중지됨 (과일 꽂기 씬 튜토리얼 시작).");
            }
        }
        else
    {
        SetTutorialState(false);
        SetGameState(GameState.Playing);
        LoadOrderForCurrentCustomer(); // 일반 게임 시작 시 주문 로드
    }
}

    // 튜토리얼 UI의 시작 버튼 등에서 호출될 함수
    public void EndTutorialAndStartGame()
    {
        if (currentGameState == GameState.TutorialDisplay && isTutorialActive)
        {
            Debug.Log("튜토리얼 종료. 게임을 시작합니다!");
            SetTutorialState(false); // isTutorialActive를 false로 변경
            SetGameState(GameState.Playing); // 게임 상태를 Playing으로 변경

            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
            PlayerPrefs.Save();
            Debug.Log("튜토리얼 완료 상태 저장됨.");

            // 현재 손님 (튜토리얼 손님, 즉 0번 인덱스)의 주문을 다시 로드하거나,
            // 이미 로드된 주문 정보를 바탕으로 게임 플레이 상태에 맞는 처리를 시작합니다.
            // LoadOrderForCurrentCustomer()를 호출하면 내부에서 스폰 로직이 트리거될 수 있습니다.
            LoadOrderForCurrentCustomer(); // 이 함수는 내부적으로 OnOrderLoaded를 호출하고,
                                        // isTutorialActive가 false이고 currentGameState가 Playing이므로
                                        // FruitSpawner2D.Instance.StartSpawning();을 호출할 것입니다.
        }
    }

    // 현재 손님 인덱스에 따라 주문을 로드하고 게임을 준비합니다.
    void LoadOrderForCurrentCustomer()
    {
        if (currentCustomerIndex < 0 || currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.LogError($"CustomerOrderManager: 유효하지 않은 손님 인덱스({currentCustomerIndex})입니다. 최대 인덱스: {allCustomerOrders.Count - 1}");
            // 예외 처리: 타이틀 씬으로 이동 등
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(stageSelectSceneName);
            else SceneManager.LoadScene(stageSelectSceneName);
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
            // 예외 처리
            return;
        }

        OnOrderLoaded?.Invoke(); // 주문 로드 완료 이벤트 발생 (UI 업데이트용)

        // 과일 스포너 시작 (튜토리얼이 아니거나, 튜토리얼이 끝난 후 게임 시작 시)
        if (!isTutorialActive && currentGameState == GameState.Playing && FruitSpawner2D.Instance != null)
        {
            FruitSpawner2D.Instance.StartSpawning();
            Debug.Log("FruitSpawner2D 스폰 시작됨.");
        }
    }

    // 다음 손님으로 넘어갈 때 호출
    public void LoadNextCustomerOrder()
    {
        currentCustomerIndex++;
        GameInfoHolder.CustomerIndexToLoad = currentCustomerIndex; // GameInfoHolder에도 업데이트

        if (currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.Log("모든 손님의 주문을 완료했습니다! 게임 완료 또는 다음 단계로...");
            // 모든 손님 완료 후 로직 (예: 엔딩 씬, 타이틀로 돌아가기 등)
            // 여기서는 일단 타이틀 씬으로 돌아가도록 설정
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(stageSelectSceneName);
            else SceneManager.LoadScene(stageSelectSceneName);
            return;
        }

        SetTutorialState(false); // 다음 손님은 튜토리얼 아님
        SetGameState(GameState.Playing); // 게임 상태를 Playing으로 설정
        LoadOrderForCurrentCustomer();
    }


    // 과일 꽂기 단계에서 호출
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

            // 다음 미니게임 단계로 전환 (예: 설탕 끓이기)
            Debug.Log($"{CurrentOrderData.customerName} 손님의 [{sugarBoilingSceneName}] 단계로 넘어갑니다.");
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(sugarBoilingSceneName);
            else SceneManager.LoadScene(sugarBoilingSceneName);
        }
        else
        {
            Debug.Log($"과일 꽂기 실패! ({CurrentOrderData.customerName})");
            if (HeartManager.Instance != null) HeartManager.Instance.LoseHeart();
            // if (SkewerManager.Instance != null) SkewerManager.Instance.ClearSkewer();

            // 실패 시 튜토리얼 메시지 등 (각 씬의 UI 컨트롤러에서 isTutorialActive 참조하여 처리 가능)
            if (isTutorialActive)
            {
                // 예: TutorialUIManager.Instance.ShowMessage("이런! 과일 순서가 다른 것 같아요. 주문서를 잘 보고 다시 시도해보세요!");
                Debug.Log("튜토리얼 중 과일 꽂기 실패. 다시 시도하세요.");
            }
        }
        return orderMatch;
    }

    // 모든 미니게임 단계 완료 후 호출될 함수
    public void AllMiniGamesCompletedForCurrentCustomer()
    {
        if (CurrentOrderData == null)
        {
            Debug.LogError("AllMiniGamesCompletedForCurrentCustomer: CurrentOrderData가 null입니다. 오류 발생.");
            LoadTitleScene(); // 예외 처리로 타이틀/스테이지 선택 씬으로
            return;
        }

        Debug.Log($"{CurrentOrderData.customerName} 손님의 모든 탕후루 제작 단계 완료!");

        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.SetStageCleared(currentCustomerIndex);
        }

        // ★★★ 손님에게 전달하는 씬으로 이동 ★★★
        if (!string.IsNullOrEmpty(customerPresentationSceneName))
        {
            Debug.Log($"손님에게 전달하는 씬 ({customerPresentationSceneName})으로 이동합니다.");
            // GameInfoHolder.CustomerIndexToLoad는 이미 현재 손님 인덱스로 설정되어 있을 것입니다.
            // CustomerPresentationScene에서 이 인덱스를 사용하여 해당 손님과 완성된 탕후루를 보여줄 수 있습니다.
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
            LoadTitleScene(); // 전달 씬이 없으면 기존 로직대로 타이틀(스테이지 선택) 씬으로
        }
    }

    // UI 스크립트에서 과일 스프라이트를 가져가기 위한 함수
    public Sprite GetSpriteForFruitUI(FruitType fruitType)
    {
        if (fruitSpriteDic == null) InitializeSpriteDictionary(); // 안전장치

        if (fruitSpriteDic != null && fruitSpriteDic.TryGetValue(fruitType, out Sprite sprite))
        {
            return sprite;
        }
        Debug.LogWarning($"CustomerOrderManager: UI용 {fruitType} 스프라이트를 찾을 수 없습니다.");
        return null;
    }

    // 외부에서 게임 상태를 변경할 수 있는 함수
    public void SetGameState(GameState newState)
    {
        if (currentGameState != newState)
        {
            currentGameState = newState;
            OnGameStateChanged?.Invoke(currentGameState, isTutorialActive);
            Debug.Log($"CustomerOrderManager: GameState 변경됨 -> {currentGameState}");
        }
    }

    // 외부에서 튜토리얼 상태를 변경할 수 있는 함수
    public void SetTutorialState(bool tutorialActive)
    {
        if (isTutorialActive != tutorialActive)
        {
            isTutorialActive = tutorialActive;
            OnGameStateChanged?.Invoke(currentGameState, isTutorialActive); // 게임 상태 변경 이벤트에 튜토리얼 상태도 포함하여 전달
            Debug.Log($"CustomerOrderManager: TutorialActive 상태 변경됨 -> {isTutorialActive}");
        }
    }

    // 다음 주요 게임 단계로 이동 (SugarBoilingManager 등에서 호출)
    // 이 함수는 현재 미니게임이 성공적으로 완료되었을 때 다음 미니게임으로 넘어가는 것을 담당합니다.
    public void ProceedToNextMiniGameStep()
    {
        if (CurrentOrderData == null)
        {
            Debug.LogError("ProceedToNextMiniGameStep: CurrentOrderData is null. Cannot proceed.");
            LoadTitleScene(); // 예외 처리로 타이틀 씬으로
            return;
        }

        // 현재 씬 이름을 기준으로 다음 단계를 결정하거나,
        // CustomerOrderData에 현재 완료된 단계를 저장하고 다음 단계를 결정할 수 있습니다.
        // 여기서는 간단하게 현재 씬 이름을 기반으로 다음 씬을 로드하는 예시를 보여드립니다.
        // 더 정교한 상태 관리가 필요할 수 있습니다.

        string currentSceneName = SceneManager.GetActiveScene().name;
        string nextSceneToLoad = "";

        if (currentSceneName == sugarBoilingSceneName) // 설탕 끓이기 완료 후
        {
            nextSceneToLoad = sugarCoatingSceneName;
        }
        else if (currentSceneName == sugarCoatingSceneName) // 설탕 코팅 완료 후
        {
            // 토핑 단계가 있다면 토핑 씬으로, 없다면 전체 완료 처리
            if (!string.IsNullOrEmpty(toppingPlacementSceneName) && CurrentOrderData.toppingItem != FruitType.None)
            {
                nextSceneToLoad = toppingPlacementSceneName;
            }
            else
            {
                AllMiniGamesCompletedForCurrentCustomer();
                return; // 모든 단계 완료, 더 이상 진행할 미니게임 없음
            }
        }
        else if (currentSceneName == toppingPlacementSceneName) // 토핑 완료 후
        {
            AllMiniGamesCompletedForCurrentCustomer();
            return; // 모든 단계 완료
        }
        else
        {
            Debug.LogError($"ProceedToNextMiniGameStep: 현재 씬({currentSceneName})에서 다음 단계를 결정할 수 없습니다.");
            LoadTitleScene();
            return;
        }

        if (!string.IsNullOrEmpty(nextSceneToLoad))
        {
            Debug.Log($"다음 미니게임 단계로 진행: {nextSceneToLoad}");
            if (SceneSwitcher.Instance != null)
            {
                SceneSwitcher.Instance.LoadScene(nextSceneToLoad);
            }
            else
            {
                SceneManager.LoadScene(nextSceneToLoad);
            }
        }
    }

    // 타이틀 씬 로드 (공통 사용 가능)
    private void LoadTitleScene()
    {
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene(stageSelectSceneName);
        }
        else
        {
            SceneManager.LoadScene(stageSelectSceneName);
        }
    }

    // 이전에 사용하던 UI 관련 필드들은 제거되었으므로, 해당 필드를 사용하는 함수들도 제거하거나 수정해야 합니다.
    // 예를 들어, DisplayOrderOnUI는 이제 각 씬의 UI 관리자가 호출하거나,
    // OnOrderLoaded 이벤트를 구독하여 필요한 정보를 가져가 UI를 업데이트합니다.
    // 아래는 DisplayOrderOnUI 함수를 남겨두되, UI 참조 없이 데이터만 준비하는 형태로 변경한 예시입니다.
    // 하지만 실제 UI 표시는 각 씬의 UI 스크립트에서 GetSpriteForFruitUI 등을 사용하여 구현해야 합니다.

    // DisplayOrderOnUI 함수는 이제 직접 UI를 조작하지 않으므로,
    // 이 함수를 호출하는 부분에서 OnOrderLoaded 이벤트를 발생시키고,
    // 각 씬의 UI 스크립트가 이 이벤트를 구독하여 UI를 업데이트하도록 변경하는 것이 좋습니다.
    // 여기서는 일단 함수 시그니처만 남겨두고, 실제 UI 조작 코드는 제거합니다.
    void DisplayOrderOnUI()
    {
        // 이 함수는 이제 더 이상 CustomerOrderManager에서 직접 UI를 제어하지 않습니다.
        // 주문 정보가 로드되면 OnOrderLoaded 이벤트가 발생하고,
        // 각 씬의 UI 관리 스크립트 (예: FruitCatchingOrderUI.cs)가 이 이벤트를 받아
        // CustomerOrderManager.Instance.CurrentOrderData 와 GetSpriteForFruitUI()를 사용하여
        // 해당 씬의 주문서 UI를 업데이트합니다.
        Debug.Log("DisplayOrderOnUI: 주문 정보가 준비되었습니다. UI 업데이트는 각 씬의 담당 스크립트에서 OnOrderLoaded 이벤트를 통해 진행됩니다.");
        OnOrderLoaded?.Invoke(); // 주문 정보가 준비되었음을 알림
    }
}
