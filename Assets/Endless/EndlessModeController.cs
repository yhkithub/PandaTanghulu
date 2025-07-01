// EndlessModeController.cs
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndlessModeController : MonoBehaviour
{
    public static EndlessModeController Instance;

    [Header("UI 프리팹 연결")]
    [Tooltip("1단계에서 만든 UI 프리팹을 여기에 연결하세요.")]
    public GameObject endlessUIPrefab;

    // --- 내부에서 사용할 UI 변수들 ---
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timerText;
    private CanvasGroup endlessUIGroup;

    [Header("게임 설정")]
    public float initialTimePerCustomer = 45f;
    public string endlessGameOverSceneName = "EndlessGameOverScene"; 

    // 나머지 변수는 기존과 동일
    private int score = 0;
    private float timePerCustomer, currentTime;
    private bool isGameActive = false;
    private SkewerVisualizer visualizer;
    private float sugarBoilingSpeedMultiplier = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            visualizer = GetComponent<SkewerVisualizer>();

            // UI 프리팹이 할당되어 있으면, 인스턴스를 생성합니다.
            if (endlessUIPrefab != null)
            {
                // this.transform을 부모로 하여, UI도 DontDestroyOnLoad의 일부가 되게 합니다.
                GameObject uiInstance = Instantiate(endlessUIPrefab, this.transform);
                uiInstance.name = "EndlessUI_Instance";

                // 생성된 UI 인스턴스에서 컴포넌트를 찾습니다.
                // 자식 오브젝트의 이름이 다르다면 실제 이름으로 수정해야 합니다.
                endlessUIGroup = uiInstance.GetComponent<CanvasGroup>();
                scoreText = uiInstance.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
                timerText = uiInstance.transform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();

                // 컴포넌트를 잘 찾았는지 확인
                if (endlessUIGroup == null || scoreText == null || timerText == null)
                {
                    Debug.LogError("EndlessUI 프리팹 또는 그 자식(ScoreText, TimerText)에서 컴포넌트를 찾지 못했습니다! 프리팹 구조와 자식 오브젝트 이름을 확인해주세요.");
                }
            }
            else
            {
                Debug.LogError("EndlessModeController에 endlessUIPrefab이 할당되지 않았습니다!");
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameModeManager.IsEndlessMode)
        {
            if (endlessUIGroup != null)
            {
                endlessUIGroup.alpha = 0;
                endlessUIGroup.interactable = false;
                endlessUIGroup.blocksRaycasts = false;
            }
            return;
        }
        StartCoroutine(ShowUIAndSkewerOnSceneLoad(scene));
    }

    IEnumerator ShowUIAndSkewerOnSceneLoad(Scene scene)
    {
        // ★★★[최종 해결책] UI를 0.5초간 계속 활성화하여 다른 스크립트의 비활성화 명령을 무시 ★★★
        float timer = 0f;
        while(timer < 0.5f)
        {
            if (endlessUIGroup != null)
            {
                if(!endlessUIGroup.gameObject.activeSelf) endlessUIGroup.gameObject.SetActive(true);
                if(endlessUIGroup.alpha < 1f) endlessUIGroup.alpha = 1;
                if(!endlessUIGroup.interactable) endlessUIGroup.interactable = true;
                if(!endlessUIGroup.blocksRaycasts) endlessUIGroup.blocksRaycasts = true;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        Debug.Log($"[{scene.name}] 무한 모드 UI 활성화를 완료했습니다.");


        // --- 이하 꼬치 생성 및 스포너 로직은 기존과 동일 ---
        if (scene.name == "FruitCatchingGameScene" && FruitSpawner2D.Instance != null)
        {
            FruitSpawner2D.Instance.StartSpawning();
        }

        if (CustomerOrderManager.Instance == null || visualizer == null) yield break;
        CustomerOrderData currentOrder = CustomerOrderManager.Instance.CurrentOrderData;
        if (currentOrder == null) yield break;

        if (scene.name == "SugarCoatingScene")
        {
            var manager = FindFirstObjectByType<SugarCoatingManager>();
            if (manager != null && manager.skewerParent != null)
            {
                manager.skewerParent.gameObject.SetActive(true);
                // ✅ manager가 가진 목표 높이 값을 전달합니다.
                visualizer.DisplaySkewer(manager.skewerParent, currentOrder.skewerOrder, manager.targetVisualHeightInWorldUnits);
            }
        }
        else if (scene.name == "ToppingPlacementScene")
        {
            var manager = FindFirstObjectByType<ToppingPlacementManager>();
            if (manager != null && manager.skewerParent != null)
            {
                manager.skewerParent.gameObject.SetActive(true);
                // 꼬치 모양을 먼저 생성합니다.
                visualizer.DisplaySkewer(manager.skewerParent, currentOrder.skewerOrder, manager.targetVisualHeightInWorldUnits);
                
                // [수정 전] 삭제된 함수를 호출하여 에러 발생
                // visualizer.ApplySugarCoatingEffect(manager.skewerParent);

                // [수정 후] 새롭고 올바른 마스크 기반 코팅 함수를 호출합니다.
                // 토핑 씬에서는 코팅이 완료된 상태이므로 progress를 1.0f로 전달합니다.
                visualizer.ApplyMaskedSugarCoating(manager.skewerParent, 1.0f);
            }
        }
        else if (scene.name == "CustomerPresentationScene")
        {
            var manager = FindFirstObjectByType<CustomerPresentationManager>();
            if (manager != null && manager.tanghuluOnBoardRect != null)
            {
                manager.tanghuluOnBoardRect.gameObject.SetActive(true);

                // ✅ visualizer가 가진 기본 높이(stickTargetHeight)를 사용하도록 수정합니다.
                visualizer.DisplaySkewer(manager.tanghuluOnBoardRect, currentOrder.skewerOrder, visualizer.stickTargetHeight);
                
                StartCoroutine(CustomerExitAnimation(manager.customerImage.gameObject, manager));
            }
        }
    }

    public void GameOver()
    {
        isGameActive = false;
        GameModeManager.IsEndlessMode = false;

        if (endlessUIGroup != null)
        {
            endlessUIGroup.alpha = 0;
            endlessUIGroup.interactable = false;
            endlessUIGroup.blocksRaycasts = false;
        }

        // 최고 점수와 마지막 점수를 저장합니다.
        int highScore = PlayerPrefs.GetInt("EndlessHighScore", 0);
        if (score > highScore)
        {
            PlayerPrefs.SetInt("EndlessHighScore", score);
        }
        PlayerPrefs.SetInt("LastEndlessScore", score);
        PlayerPrefs.Save(); // 변경사항 저장

        // 무한 모드 전용 게임오버 씬을 불러옵니다.
        SceneManager.LoadScene(endlessGameOverSceneName);
    }
    // ... 나머지 함수들은 모두 기존과 동일하게 유지
    public void StartEndlessMode() 
    { 
        if (GameModeManager.IsEndlessMode) return; 
        GameModeManager.IsEndlessMode = true; 
        InitializeEndlessMode(); 
    }

    void InitializeEndlessMode() 
    {
        // ▼▼▼ [핵심 수정] 튜토리얼 상태를 강제로 해제하는 로직 추가 ▼▼▼
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.SetTutorialState(false);
            CustomerOrderManager.Instance.SetGameState(GameState.Playing);
            Debug.Log("무한 모드 시작: 튜토리얼 상태를 강제로 해제했습니다.");
        }
        else
        {
            Debug.LogError("튜토리얼 상태 해제 실패: CustomerOrderManager를 찾을 수 없습니다!");
        }
        // ▲▲▲ [핵심 수정] 여기까지 ▲▲▲

        timePerCustomer = initialTimePerCustomer; 
        score = 0; 
        sugarBoilingSpeedMultiplier = 1.0f; 
        UpdateScoreUI(); 
        StartCoroutine(StartNextCustomerFlow()); 
    }
    IEnumerator StartNextCustomerFlow() { var allCustomers = CustomerOrderManager.Instance.allCustomerOrders; CustomerOrderData randomOrder = EndlessOrderGenerator.Generate(allCustomers, score); if (randomOrder == null) yield break; CustomerOrderManager.Instance.SetEndlessModeOrder(randomOrder); currentTime = timePerCustomer; isGameActive = true; SceneManager.LoadScene("ShopScene"); }
    public void CustomerCleared() { isGameActive = false; score++; UpdateScoreUI(); timePerCustomer *= 0.95f; if (timePerCustomer < 15f) timePerCustomer = 15f; sugarBoilingSpeedMultiplier += 0.1f; StartCoroutine(StartNextCustomerFlow()); }
    public float GetSugarBoilingSpeed() { return sugarBoilingSpeedMultiplier; }
    void Update() { if (isGameActive) { currentTime -= Time.deltaTime; UpdateTimerUI(); if (currentTime <= 0) GameOver(); } }
    void UpdateScoreUI() { if (scoreText != null) scoreText.text = $"SCORE: {score}"; }
    void UpdateTimerUI() { if (timerText != null) timerText.text = $"TIME: {Mathf.Max(0, currentTime):F1}"; }
    IEnumerator CustomerExitAnimation(GameObject cObj, CustomerPresentationManager pManager) { if(pManager.pupuSpeechBubbleGroup!=null && CustomerOrderManager.Instance.CurrentOrderData.presentationDialogueSequence.Count > 0){pManager.pupuSpeechBubbleGroup.gameObject.SetActive(true);pManager.pupuSpeechBubbleGroup.alpha=1;pManager.pupuSpeechText.text=CustomerOrderManager.Instance.CurrentOrderData.presentationDialogueSequence[0].line;} yield return new WaitForSeconds(1.5f); if(pManager.pupuSpeechBubbleGroup!=null){pManager.pupuSpeechBubbleGroup.gameObject.SetActive(false);} float dur=0.8f; Vector3 sPos=cObj.transform.position; Vector3 ePos=sPos+new Vector3(20,5,0); Vector3 sScl=cObj.transform.localScale; Vector3 eScl=sScl*0.5f; for(float t=0;t<dur;t+=Time.deltaTime){if(cObj==null)break; cObj.transform.position=Vector3.Lerp(sPos,ePos,t/dur); cObj.transform.localScale=Vector3.Lerp(sScl,eScl,t/dur); yield return null;} CustomerCleared(); }
}