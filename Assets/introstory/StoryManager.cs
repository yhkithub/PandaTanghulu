// StoryManager.cs
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
        public AudioClip soundEffect; // 각 스텝별 효과음 AudioClip 직접 참조
    }

    public StoryStep[] storySteps;
    public SpriteRenderer backgroundRenderer;
    public BackgroundScaler backgroundScaler;
    public TextMeshProUGUI storyText;
    public GameObject nextButton;
    public GameObject ribbonObj;
    public GameObject mouseTrailObj;
    public AudioClip nextButtonSound; // 다음 버튼 클릭 효과음 AudioClip 직접 참조

    private int currentStep = 0;
    private AudioSource audioSource; // StoryManager 자체의 AudioSource

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            // 현재 재생 중인 BGM을 중지하려면
            AudioManager.Instance.StopBackgroundMusic();
            AudioManager.Instance.PlayBgm("MainGameBGM"); // "ShopBGM"으로 교체

        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // AudioSource가 없다면 하나 추가해줍니다. (선택적, 오류 대신 자동 추가)
            // Debug.LogWarning("StoryManager: AudioSource 컴포넌트가 없어 새로 추가합니다.", this.gameObject);
            // audioSource = gameObject.AddComponent<AudioSource>();
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

    // "다음" 버튼 클릭 시 호출
    public void OnNextClicked()
    {
        // ★★★ 다음 버튼 클릭 시 사운드 재생 수정 ★★★
        if (nextButtonSound != null) // AudioClip이 할당되어 있는지 먼저 확인
        {
            // AudioManager가 있고, SFX가 활성화되어 있을 때만 재생
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxEnabled)
            {
                if (audioSource != null) // StoryManager의 AudioSource가 있는지 확인
                {
                    audioSource.PlayOneShot(nextButtonSound);
                }
                else
                {
                    Debug.LogWarning("StoryManager Warning: 다음 버튼 사운드를 재생할 AudioSource가 없습니다 (StoryManager에).");
                }
            }
            else if (AudioManager.Instance != null && !AudioManager.Instance.IsSfxEnabled)
            {
                Debug.Log("StoryManager: SFX 비활성화됨, 다음 버튼 사운드 재생 안 함.");
            }
            else if (AudioManager.Instance == null)
            {
                Debug.LogWarning("StoryManager Warning: AudioManager 인스턴스를 찾을 수 없어 다음 버튼 사운드 설정을 확인할 수 없습니다.");
                // AudioManager가 없는 경우, 그냥 재생하거나 재생하지 않는 정책을 정할 수 있습니다.
                // if (audioSource != null) audioSource.PlayOneShot(nextButtonSound); // 예: AudioManager 없으면 그냥 재생
            }
        }

        currentStep++;

        if (currentStep < storySteps.Length)
        {
            ShowStep(currentStep);
        }
        else
        {
            Debug.Log("StoryManager: OnNextClicked에서 모든 스텝 완료 확인 (실제 처리는 ShowStep에서).");
        }
    }

    void ShowStep(int index)
    {
        if (index < 0 || index >= storySteps.Length) { Debug.LogError($"StoryManager Error: 잘못된 스토리 스텝 인덱스입니다: {index}"); return; }

        Debug.Log($"StoryManager: 스텝 {index} 표시 중.");
        StoryStep step = storySteps[index];

        if (storyText != null) { storyText.text = step.text; }
        if (backgroundRenderer != null)
        {
            if (step.backgroundImage != null)
            {
                backgroundRenderer.sprite = step.backgroundImage;
                Debug.Log($"StoryManager: 배경 이미지를 '{step.backgroundImage.name}'으로 변경.");
                if (backgroundScaler != null) { backgroundScaler.ScaleBackground(); }
                else { Debug.LogWarning("StoryManager Warning: BackgroundScaler 참조가 없어 스케일 재조정을 호출할 수 없습니다.", this.gameObject); }
            }
            else { Debug.LogWarning($"StoryManager Warning: 스텝 {index}의 배경 이미지가 null입니다."); }
        }

        // ★★★ 장면 사운드(step.soundEffect) 재생 로직 수정 ★★★
        if (step.soundEffect != null) // AudioClip이 할당되어 있는지 먼저 확인
        {
            // AudioManager가 있고, SFX가 활성화되어 있을 때만 재생
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxEnabled)
            {
                if (audioSource != null) // StoryManager의 AudioSource가 있는지 확인
                {
                    audioSource.PlayOneShot(step.soundEffect);
                    Debug.Log($"StoryManager: 스텝 {index}의 사운드 '{step.soundEffect.name}' 재생 시도 (SFX 활성화됨).");
                }
                else
                {
                    Debug.LogWarning($"StoryManager Warning: 스텝 {index} 사운드를 재생할 AudioSource가 없습니다 (StoryManager에).");
                }
            }
            else if (AudioManager.Instance != null && !AudioManager.Instance.IsSfxEnabled)
            {
                Debug.Log($"StoryManager: 스텝 {index}의 사운드 '{step.soundEffect.name}' 재생 안 함 (SFX 비활성화됨).");
            }
            else if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"StoryManager Warning: AudioManager 인스턴스를 찾을 수 없어 스텝 {index} 사운드 설정을 확인할 수 없습니다.");
                // if (audioSource != null) audioSource.PlayOneShot(step.soundEffect); // 예: AudioManager 없으면 그냥 재생
            }
        }


        // 마지막 스텝인지 확인하고 리본/트레일 활성화
        if (index == storySteps.Length - 1)
        {
            Debug.Log("StoryManager: 마지막 스텝 표시 완료. 리본 커팅 단계 진입.");
            if (nextButton != null)
            {
                nextButton.SetActive(false);
                Debug.Log("StoryManager: 다음 버튼 비활성화됨.");
            }

            if (ribbonObj != null)
            {
                ribbonObj.SetActive(true);
                Debug.Log("StoryManager: RibbonObj 활성화됨.");
            }
            if (mouseTrailObj != null)
            {
                mouseTrailObj.SetActive(true);
                Debug.Log("StoryManager: MouseTrailObj 활성화됨.");
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
    public void OnSkipButtonClicked()
    {
        Debug.Log("스토리 씬 스킵! 상점 씬으로 이동합니다.");
        // 상점 씬으로 즉시 이동
        SceneManager.LoadScene("ShopScene");
    }
}