// UIPopupManager.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPopupManager : MonoBehaviour
{
    public static UIPopupManager Instance { get; private set; }

    public GameObject exitConfirmationPrefab;
    private string titleSceneName = "TitleScene";
    private GameObject instantiatedPanel;

    // ✅ Awake, Update, HideExitPopup, ConfirmExitToTitle 메서드는 기존과 동일하게 유지합니다.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // ❌ SetupCanvas() 메서드와 private Canvas popupCanvas; 변수는 삭제합니다.

    void ShowExitPopup()
    {
        if (exitConfirmationPrefab == null)
        {
            Debug.LogError("UIPopupManager: 'Exit Confirmation Prefab'이 할당되지 않았습니다!");
            return;
        }

        if (instantiatedPanel == null)
        {
            // 부모를 지정하지 않고, 독립적인 객체로 생성합니다.
            instantiatedPanel = Instantiate(exitConfirmationPrefab); 
            
            // 프리팹의 Canvas가 ScreenSpaceCamera 모드일 때, 메인 카메라를 찾아 할당해줍니다.
            Canvas prefabCanvas = instantiatedPanel.GetComponent<Canvas>();
            if (prefabCanvas != null && prefabCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // 월드 카메라가 설정되지 않았다면, 씬의 메인 카메라를 찾아 할당
                if (prefabCanvas.worldCamera == null)
                {
                    prefabCanvas.worldCamera = Camera.main;
                }
            }

            // ✅ 프리팹의 모든 자식에서 Button 컴포넌트를 가져와서 처리합니다.
            Button yesButton = null;
            Button noButton = null;

            Button[] allButtons = instantiatedPanel.GetComponentsInChildren<Button>(true); // 비활성화된 버튼도 포함하여 찾기
            foreach (Button btn in allButtons)
            {
                if (btn.name == "YesButton")
                {
                    yesButton = btn;
                }
                else if (btn.name == "NoButton")
                {
                    noButton = btn;
                }
            }

            if (yesButton != null && noButton != null)
            {
                yesButton.onClick.AddListener(ConfirmExitToTitle);
                noButton.onClick.AddListener(HideExitPopup);
            }
            else
            {
                Debug.LogError("프리팹에서 YesButton 또는 NoButton을 찾을 수 없습니다. 프리팹의 계층 구조와 버튼 오브젝트의 이름을 확인해주세요.");
            }
        }

        instantiatedPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    // (기존과 동일한 나머지 코드)
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
        if (instantiatedPanel != null)
        {
             instantiatedPanel.SetActive(false);
        }
        SceneManager.LoadScene(titleSceneName);
    }
}