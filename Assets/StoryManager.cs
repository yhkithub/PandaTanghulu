using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    [System.Serializable]
    public class StoryStep
    {
        [TextArea(3, 5)]
        public string text;
        public Sprite backgroundImage;
        public AudioClip soundEffect;
    }

    public StoryStep[] storySteps;
    public SpriteRenderer backgroundRenderer;
    public BackgroundScaler backgroundScaler;
    public TextMeshProUGUI storyText;
    public GameObject nextButton;
    public GameObject ribbonObj;
    public GameObject mouseTrailObj;
    // 다음 버튼 클릭 사운드 추가
    public AudioClip nextButtonSound;

    private int currentStep = 0;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("StoryManager Error: AudioSource 컴포넌트가 연결되지 않았습니다!", this.gameObject);
            enabled = false;
            return;
        }

        if (storySteps == null || storySteps.Length == 0) { Debug.LogError("StoryManager Error: 'Story Steps' 배열이 비어있습니다!", this.gameObject); enabled = false; return; }
        if (backgroundRenderer == null) Debug.LogError("StoryManager Error: 'Background Renderer'가 연결되지 않았습니다!", this.gameObject);
        if (backgroundScaler == null) Debug.LogError("StoryManager Error: 'Background Scaler'가 연결되지 않았습니다! 배경 스케일링이 작동하지 않습니다.", this.gameObject);
        if (storyText == null) Debug.LogError("StoryManager Error: 'Story Text'가 연결되지 않았습니다!", this.gameObject);
        if (nextButton == null) Debug.LogError("StoryManager Error: 'Next Button'이 연결되지 않았습니다!", this.gameObject);
        if (ribbonObj == null) Debug.LogWarning("StoryManager Warning: 'Ribbon Obj'가 연결되지 않았습니다.", this.gameObject);
        if (mouseTrailObj == null) Debug.LogWarning("StoryManager Warning: 'Mouse Trail Obj'가 연결되지 않았습니다.", this.gameObject);

        if (ribbonObj != null) ribbonObj.SetActive(false);
        if (mouseTrailObj != null) mouseTrailObj.SetActive(false);

        currentStep = 0;
        ShowStep(currentStep); // 첫 번째 스텝 보여주기

        // 첫 스텝에서는 다음 버튼 활성화 (만약 스텝이 하나뿐이라면 ShowStep에서 바로 비활성화됨)
        if (nextButton != null && storySteps.Length > 1) {
            nextButton.SetActive(true);
        } else if (nextButton != null) {
            // 스토리가 1개 뿐이면 시작부터 다음 버튼 숨김 (또는 ShowStep에서 처리)
            nextButton.SetActive(false);
        }
    }

    // "다음" 버튼 클릭 시 호출
    public void OnNextClicked()
    {
        // 다음 버튼 클릭 시 사운드 재생
        if (audioSource != null && nextButtonSound != null)
        {
            audioSource.PlayOneShot(nextButtonSound);
        }
        else if (nextButtonSound != null)
        {
            Debug.LogWarning("StoryManager Warning: 다음 버튼 클릭 사운드가 설정되었지만 AudioSource가 없거나 비활성화되어 재생할 수 없습니다.");
        }

        currentStep++;

        // 다음 스텝 보여주기 (ShowStep 함수가 마지막 스텝 처리를 함)
        if (currentStep < storySteps.Length)
        {
            ShowStep(currentStep);
        }
        else
        {
            // 이 부분은 이제 ShowStep에서 처리되므로 비워두거나 로그만 남김
            Debug.Log("StoryManager: OnNextClicked에서 모든 스텝 완료 확인 (실제 처리는 ShowStep에서).");
        }
    }

    // 특정 인덱스의 스토리 단계를 화면에 표시
    void ShowStep(int index)
    {
        
        if (index < 0 || index >= storySteps.Length) { Debug.LogError($"StoryManager Error: 잘못된 스토리 스텝 인덱스입니다: {index}"); return; }

        Debug.Log($"StoryManager: 스텝 {index} 표시 중.");
        StoryStep step = storySteps[index];

        // --- 텍스트 및 배경 업데이트 (이전과 동일) ---
        if (storyText != null) { storyText.text = step.text; }
        if (backgroundRenderer != null) {
            if (step.backgroundImage != null) {
                backgroundRenderer.sprite = step.backgroundImage;
                Debug.Log($"StoryManager: 배경 이미지를 '{step.backgroundImage.name}'으로 변경.");
                if (backgroundScaler != null) { backgroundScaler.ScaleBackground(); }
                else { Debug.LogWarning("StoryManager Warning: BackgroundScaler 참조가 없어 스케일 재조정을 호출할 수 없습니다.", this.gameObject); }
            } else { Debug.LogWarning($"StoryManager Warning: 스텝 {index}의 배경 이미지가 null입니다."); }
        }

        // ★★★ 장면 사운드 재생 로직 (이전과 동일) ★★★
        if (audioSource != null && step.soundEffect != null)
        {
            audioSource.PlayOneShot(step.soundEffect);
            Debug.Log($"StoryManager: 스텝 {index}의 사운드 '{step.soundEffect.name}' 재생.");
        }
        else if (step.soundEffect != null)
        {
            Debug.LogWarning($"StoryManager Warning: 스텝 {index}에 사운드가 설정되었지만 AudioSource가 없거나 비활성화되어 재생할 수 없습니다.");
        }

        // ★★★ 마지막 스텝인지 확인하고 리본/트레일 활성화 (이전과 동일) ★★★
        if (index == storySteps.Length - 1)
        {
            Debug.Log("StoryManager: 마지막 스텝 표시 완료. 리본 커팅 단계 진입.");
            if (nextButton != null)
            {
                nextButton.SetActive(false); // 다음 버튼 숨기기
                Debug.Log("StoryManager: 다음 버튼 비활성화됨.");
            }
             // 스토리 텍스트 숨기기 (선택 사항)
             // if (storyText != null) storyText.gameObject.SetActive(false);

            // 리본과 마우스 트레일 활성화
            if (ribbonObj != null)
            {
                ribbonObj.SetActive(true);
                Debug.Log("StoryManager: RibbonObj 활성화됨.");
            }
            if (mouseTrailObj != null)
            {
                mouseTrailObj.SetActive(true);
                Debug.Log("StoryManager: MouseTrailObj 활성화됨.");
                // 필요 시 여기서 트레일 초기화
                // TrailRenderer tr = mouseTrailObj.GetComponent<TrailRenderer>();
                // if (tr != null) tr.Clear();
            }
        }
        else
        {
             // 마지막 스텝이 아니면 다음 버튼 활성화 (혹시 비활성화 상태였다면)
             if (nextButton != null && !nextButton.activeSelf)
             {
                 nextButton.SetActive(true);
             }
        }
    }
}