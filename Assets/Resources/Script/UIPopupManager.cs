using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Button, CanvasScaler, GraphicRaycaster 클래스를 사용하기 위해 추가

public class UIPopupManager : MonoBehaviour
{
    public static UIPopupManager Instance { get; private set; }

    // Unity 에디터에서 할당할 확인 창 UI '프리팹'
    public GameObject exitConfirmationPrefab;

    // 타이틀 씬의 이름
    private string titleSceneName = "TitleScene";

    // UIPopupManager가 직접 생성하고 관리할 전용 캔버스
    private Canvas popupCanvas; 
    // 실제로 생성된 팝업 창을 저장할 변수
    private GameObject instantiatedPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ▼▼▼▼▼ [핵심] 매니저가 스스로 전용 캔버스를 생성하는 로직 ▼▼▼▼▼
        SetupCanvas();
    }

    void SetupCanvas()
    {
        // 팝업을 담을 전용 게임오브젝트 생성
        GameObject canvasGO = new GameObject("PopupCanvas");
        // 이 오브젝트도 씬 전환 시 파괴되지 않도록 설정
        DontDestroyOnLoad(canvasGO);
        // UIPopupManager의 자식으로 두어 관리하기 편하게 함
        canvasGO.transform.SetParent(this.transform);

        // 캔버스 컴포넌트 추가 및 설정
        popupCanvas = canvasGO.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 다른 모든 캔버스보다 위에 그려지도록 높은 정렬 순서 부여
        popupCanvas.sortingOrder = 999;

        // 캔버스 스케일러 추가 및 설정 (해상도에 따라 UI 크기가 일정하게 유지됨)
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // 게임의 기준 해상도에 맞게 조절

        // 그래픽 레이캐스터 추가 (UI가 마우스/터치 입력을 받도록 함)
        canvasGO.AddComponent<GraphicRaycaster>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SceneManager.GetActiveScene().name != titleSceneName)
            {
                if (instantiatedPanel != null && instantiatedPanel.activeSelf)
                {
                    HideExitPopup();
                }
                else
                {
                    ShowExitPopup();
                }
            }
        }
    }

    void ShowExitPopup()
    {
        if (exitConfirmationPrefab == null)
        {
            Debug.LogError("UIPopupManager: 'Exit Confirmation Prefab'이 할당되지 않았습니다!");
            return;
        }

        if (instantiatedPanel == null)
        {
            // 이제 씬에서 Canvas를 찾지 않고, 매니저가 가진 전용 캔버스에 생성
            instantiatedPanel = Instantiate(exitConfirmationPrefab, popupCanvas.transform);
            
            RectTransform panelRect = instantiatedPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.localScale = Vector3.one;
            }

            // 버튼 이름으로 찾아 리스너 자동 연결
            Button yesButton = instantiatedPanel.transform.Find("YesButton")?.GetComponent<Button>();
            Button noButton = instantiatedPanel.transform.Find("NoButton")?.GetComponent<Button>();

            if (yesButton != null && noButton != null)
            {
                yesButton.onClick.AddListener(ConfirmExitToTitle);
                noButton.onClick.AddListener(HideExitPopup);
            }
        }

        instantiatedPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void HideExitPopup()
    {
        if (instantiatedPanel != null)
        {
            instantiatedPanel.SetActive(false);
        }
        Time.timeScale = 1f;
    }
    
    public void ConfirmExitToTitle()
    {
        Time.timeScale = 1f;
        // 팝업창을 끈 후에 씬을 로드합니다.
        if (instantiatedPanel != null)
        {
             instantiatedPanel.SetActive(false);
        }
        SceneManager.LoadScene(titleSceneName);
    }
}