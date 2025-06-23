// StoryManager.cs
using UnityEngine;
using UnityEngine.UI; // TextMeshPro를 사용하므로 UnityEngine.UI는 Text 컴포넌트를 사용하지 않는다면 지워도 무방합니다.
using TMPro;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    [System.Serializable]
    public class StoryStep
    {
        [TextArea(3, 5)]
        public string text;
        
        // [수정] '배경 이미지'가 아닌 '장면별 추가 이미지'를 위한 변수
        public Sprite sceneImage; 

        public AudioClip soundEffect;
    }

    public StoryStep[] storySteps;

    // [수정] 장면별 추가 이미지를 표시할 Image
    public Image sceneSpecificRenderer;

    public TextMeshProUGUI storyText;
    public GameObject nextButton;
    public GameObject ribbonObj;
    public GameObject mouseTrailObj;
    public AudioClip nextButtonSound;

    // [제거] 아래 두 변수는 더 이상 사용하지 않습니다.
    // public SpriteRenderer backgroundRenderer;
    // public BackgroundScaler backgroundScaler;

    private int currentStep = 0;
    private AudioSource audioSource;

    void Start()
    {
        // --- 오디오 및 BGM 설정 (기존과 동일) ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.PlayBgm("MainGameBGM");
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("StoryManager Error: AudioSource 컴포넌트가 연결되지 않았습니다!", this.gameObject);
            enabled = false;
            return;
        }

        // --- 유효성 검사 (수정된 부분 반영) ---
        if (storySteps == null || storySteps.Length == 0) { Debug.LogError("StoryManager Error: 'Story Steps' 배열이 비어있습니다!", this.gameObject); enabled = false; return; }
        
        // [수정] sceneSpecificRenderer가 할당되었는지 확인
        if (sceneSpecificRenderer == null) Debug.LogError("StoryManager Error: 'Scene Specific Renderer'가 연결되지 않았습니다!", this.gameObject);
        
        if (storyText == null) Debug.LogError("StoryManager Error: 'Story Text'가 연결되지 않았습니다!", this.gameObject);
        if (nextButton == null) Debug.LogError("StoryManager Error: 'Next Button'이 연결되지 않았습니다!", this.gameObject);
        if (ribbonObj == null) Debug.LogWarning("StoryManager Warning: 'Ribbon Obj'가 연결되지 않았습니다.", this.gameObject);
        if (mouseTrailObj == null) Debug.LogWarning("StoryManager Warning: 'Mouse Trail Obj'가 연결되지 않았습니다.", this.gameObject);

        // --- 초기화 (기존과 거의 동일) ---
        if (ribbonObj != null) ribbonObj.SetActive(false);
        if (mouseTrailObj != null) mouseTrailObj.SetActive(false);

        currentStep = 0;
        ShowStep(currentStep);

        if (nextButton != null && storySteps.Length > 1)
        {
            nextButton.SetActive(true);
        }
        else if (nextButton != null)
        {
            nextButton.SetActive(false);
        }
    }
    
    // "다음" 버튼 클릭 시 호출 (기존과 동일)
    public void OnNextClicked()
    {
        if (nextButtonSound != null)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxEnabled)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(nextButtonSound);
                }
            }
        }

        currentStep++;

        if (currentStep < storySteps.Length)
        {
            ShowStep(currentStep);
        }
    }

    void ShowStep(int index)
    {
        if (index < 0 || index >= storySteps.Length) { Debug.LogError($"StoryManager Error: 잘못된 스토리 스텝 인덱스입니다: {index}"); return; }

        Debug.Log($"StoryManager: 스텝 {index} 표시 중.");
        StoryStep step = storySteps[index];

        if (storyText != null) { storyText.text = step.text; }

        // [수정] 배경을 바꾸는 대신, 장면별 추가 이미지를 제어하는 로직
        if (sceneSpecificRenderer != null)
        {
            // 현재 스텝에 할당된 추가 이미지가 있는지 확인
            if (step.sceneImage != null)
            {
                // 이미지가 있으면 Sprite를 설정하고 활성화
                sceneSpecificRenderer.sprite = step.sceneImage;
                sceneSpecificRenderer.gameObject.SetActive(true);
                Debug.Log($"StoryManager: 추가 이미지를 '{step.sceneImage.name}'으로 변경.");
            }
            else
            {
                // 이미지가 없으면 비활성화하여 숨김
                sceneSpecificRenderer.gameObject.SetActive(false);
                Debug.Log($"StoryManager: 스텝 {index}에 추가 이미지가 없어 숨김 처리합니다.");
            }
        }
        
        // --- 효과음 재생 로직 (기존과 동일) ---
        if (step.soundEffect != null)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxEnabled)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(step.soundEffect);
                }
            }
        }
        
        // --- 마지막 스텝 처리 (기존과 동일) ---
        if (index == storySteps.Length - 1)
        {
            Debug.Log("StoryManager: 마지막 스텝 표시 완료. 리본 커팅 단계 진입.");
            if (nextButton != null)
            {
                nextButton.SetActive(false);
            }
            if (ribbonObj != null)
            {
                ribbonObj.SetActive(true);
            }
            if (mouseTrailObj != null)
            {
                mouseTrailObj.SetActive(true);
            }
        }
        else
        {
            if (nextButton != null && !nextButton.activeSelf)
            {
                nextButton.SetActive(true);
            }
        }
    }

    // --- 스킵 버튼 로직 (기존과 동일) ---
    public void OnSkipButtonClicked()
    {
        Debug.Log("스토리 씬 스킵! 상점 씬으로 이동합니다.");
        SceneManager.LoadScene("ShopScene");
    }
}