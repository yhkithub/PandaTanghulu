using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI; // Button 컴포넌트 사용

public class CustomerDialogueManager : MonoBehaviour
{
    [Header("끼끼 말풍선 설정")]
    public CanvasGroup kikiSpeechBubbleGroup;
    public TextMeshProUGUI kikiSpeechText;
    public Button kikiNextButton; // 끼끼용 다음 버튼 연결

    [Header("푸푸 말풍선 설정")]
    public CanvasGroup pupuSpeechBubbleGroup;
    public TextMeshProUGUI pupuSpeechText;
    public Button pupuNextButton; // 푸푸용 다음 버튼 연결

    [Header("사운드 설정")]
    public AudioSource bubbleOpenAudioSource; // Inspector에서 연결
    public AudioSource textAudioSource;     // Inspector에서 연결

    public List<string> kikiDialogueLines = new List<string>()
    {
        "푸푸~!! 여기 맞지?! 우와, 진짜 가게가 생겼네!!\n간판도 귀엽고, 냄새도 완전 최고고… 으아아, 지금 침이 고이는 중이야!!",
        "그래! 수업 끝나자마자 가방을 들고 냅다 달려왔지!\n물론 오면서 돌부리에 두 번, 크크에 한 번 걸려서 넘어질 뻔 했지만…\n 푸푸의 탕후루 가게의 첫 손님 자리를 놓칠 수는 없었어!",
        "당연하지!! 푸푸가 직접 만든 가게에, 탕후루잖아?!\n얼마나 기다렸는지 푸푸는 모를 거야! 근데, 다 맛있어 보여서 고민된달까.\n바나나가 없는 게 조금 아쉽지만… 으아, 왜 이렇게 고르기 어렵게 만들어 놨어!?",
        "오오오! 그거 완전 비타민 폭발 조합이잖아!!\n좋아! 통귤 4개로 꽉 채워줘! 이름은… 끼끼 에너지 폭발 탕후루로 하자!",
        "… … … …!\n뭐야, 이거! 진짜 맛있잖아!!\n설탕이 아삭하면서 달달하고, 그 뒤에는 귤의 상큼함이 팍 터져!\n게다가 이 끝의 바나나가 킥! 처음인데 너무 잘 만든 것 같아!",
        "기운이 나는 게 아니라 차오른다! 마치 레벨업을 한 것 같아!\n푸푸, 나 이거 먹고 내일 달리기 시합도 1등할 거야! 아니, 학교 신기록을 세울 수 있을 것 같아!",
        "음음, 좋아! 다음엔 친구들도 데려올게! 우리 동네의 명물로 만들어주고 말겠어!"
    };

    public List<string> pupuDialogueLines = new List<string>()
    {
        "끼끼~ 와줘서 고마워. 오늘 학교 끝나자마자 바로 온 거야?",
        "하하, 다치진 않았지?\n그렇게 급하게 온 거 보니까, 꽤 기대하고 있었나 봐!",
        "음, 그럼 내 추천 조합은 어때?\n아무래도 정석은 모두 같은 과일로 통일해서 먹는 거지!\n첫 손님이니까 특별히 내가 통귤로 가득 채워서 줄게.",
        "자, 끼끼 에너지 폭발 탕후루 나왔습니다!",
        "마지막은 내 선물이야! 아무래도 끼끼가 가장 좋아하는 과일이 빠지면 섭섭하니까~ 어때, 기운 나?",
        "좋아, 그럼 다음엔 끼끼 챔피언 조합으로 준비해둘게!"
    };

    private int currentKikiDialogueIndex = 0;
    private int currentPupuDialogueIndex = 0;
    private bool isDialoguePlaying = false;
    private bool isTextTyping = false; // 현재 텍스트 타이핑 중인지 확인
    private CanvasGroup currentBubbleGroup; // 현재 활성화된 말풍선 그룹

    void Start()
    {
        // 각 버튼에 클릭 리스너 추가 및 초기 비활성화
        if (kikiNextButton != null)
        {
            kikiNextButton.onClick.AddListener(OnNextButtonClicked);
            kikiNextButton.gameObject.SetActive(false);
        }
        if (pupuNextButton != null)
        {
            pupuNextButton.onClick.AddListener(OnNextButtonClicked);
            pupuNextButton.gameObject.SetActive(false);
        }

        // Inspector에서 AudioSource가 연결되었는지 확인 (선택 사항)
        if (bubbleOpenAudioSource == null)
        {
            Debug.LogError("Bubble Open AudioSource가 연결되지 않았습니다!");
        }
        if (textAudioSource == null)
        {
            Debug.LogError("Text AudioSource가 연결되지 않았습니다!");
        }
    }

    public void StartFirstDialogue()
    {
        if (isDialoguePlaying) return;
        StartCoroutine(ProceedFirstDialogue());
    }

    IEnumerator ProceedFirstDialogue()
    {
        isDialoguePlaying = true;

        // 끼끼 첫 대사
        currentBubbleGroup = kikiSpeechBubbleGroup;
        yield return ShowSpeechBubbleAndText(kikiSpeechBubbleGroup, kikiSpeechText, kikiDialogueLines[currentKikiDialogueIndex++], kikiNextButton);
        yield return WaitForNextButtonClick();

        // 푸푸 첫 대사
        currentBubbleGroup = pupuSpeechBubbleGroup;
        yield return ShowSpeechBubbleAndText(pupuSpeechBubbleGroup, pupuSpeechText, pupuDialogueLines[currentPupuDialogueIndex++], pupuNextButton);
        yield return WaitForNextButtonClick();

        // 끼끼 두 번째 대사
        currentBubbleGroup = kikiSpeechBubbleGroup;
        yield return ShowSpeechBubbleAndText(kikiSpeechBubbleGroup, kikiSpeechText, kikiDialogueLines[currentKikiDialogueIndex++], kikiNextButton);
        yield return WaitForNextButtonClick();

        // 푸푸 두 번째 대사
        currentBubbleGroup = pupuSpeechBubbleGroup;
        yield return ShowSpeechBubbleAndText(pupuSpeechBubbleGroup, pupuSpeechText, pupuDialogueLines[currentPupuDialogueIndex++], pupuNextButton);
        yield return WaitForNextButtonClick();

        // 나머지 대사들을 순서대로 추가
        while (currentKikiDialogueIndex < kikiDialogueLines.Count || currentPupuDialogueIndex < pupuDialogueLines.Count)
        {
            if (currentKikiDialogueIndex < kikiDialogueLines.Count)
            {
                currentBubbleGroup = kikiSpeechBubbleGroup;
                yield return ShowSpeechBubbleAndText(kikiSpeechBubbleGroup, kikiSpeechText, kikiDialogueLines[currentKikiDialogueIndex++], kikiNextButton);
                yield return WaitForNextButtonClick();
            }
            if (currentPupuDialogueIndex < pupuDialogueLines.Count)
            {
                currentBubbleGroup = pupuSpeechBubbleGroup;
                yield return ShowSpeechBubbleAndText(pupuSpeechBubbleGroup, pupuSpeechText, pupuDialogueLines[currentPupuDialogueIndex++], pupuNextButton);
                yield return WaitForNextButtonClick();
            }
        }

        isDialoguePlaying = false;
        Debug.Log("첫 번째 대화 종료!");
        // 대화 종료 후 음식 만들기 단계로 넘어가는 로직 호출
        // 예: FindObjectOfType<FoodMakingManager>()?.StartFoodMaking();
    }

    IEnumerator ShowSpeechBubbleAndText(CanvasGroup bubbleGroup, TextMeshProUGUI textComponent, string message, Button nextBtn)
    {
        if (bubbleGroup != null)
        {
            bubbleGroup.gameObject.SetActive(true);
            if (bubbleOpenAudioSource != null)
            {
                bubbleOpenAudioSource.Play(); // 말풍선 등장 사운드 재생
            }
            textComponent.text = "";
            yield return StartCoroutine(TypeText(textComponent, message));
            // 텍스트 타이핑 완료 후 해당 다음 버튼 활성화
            if (nextBtn != null)
            {
                nextBtn.gameObject.SetActive(true);
            }
        }
    }

    IEnumerator TypeText(TextMeshProUGUI textComponent, string message)
    {
        isTextTyping = true;
        textComponent.text = ""; // 텍스트 초기화
        foreach (char c in message)
        {
            textComponent.text += c;
            if (textAudioSource != null)
            {
                textAudioSource.PlayOneShot(textAudioSource.clip); // 말 출력 사운드 재생 (겹쳐서 재생 가능하도록 PlayOneShot 사용)
            }
            yield return new WaitForSeconds(0.02f); // 타이핑 속도 약간 빠르게 조정
        }
        isTextTyping = false;
    }

    IEnumerator FadeIn(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        // 말풍선 사라질 때 모든 다음 버튼 비활성화
        if (kikiNextButton != null) kikiNextButton.gameObject.SetActive(false);
        if (pupuNextButton != null) pupuNextButton.gameObject.SetActive(false);
    }

    IEnumerator WaitForNextButtonClick()
    {
        bool buttonClicked = false;
        if (currentBubbleGroup == kikiSpeechBubbleGroup && kikiNextButton != null)
        {
            while (!buttonClicked && kikiNextButton.gameObject.activeSelf)
            {
                if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(kikiNextButton.GetComponent<RectTransform>(), Input.mousePosition))
                {
                    buttonClicked = true;
                }
                yield return null;
            }
        }
        else if (currentBubbleGroup == pupuSpeechBubbleGroup && pupuNextButton != null)
        {
            while (!buttonClicked && pupuNextButton.gameObject.activeSelf)
            {
                if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(pupuNextButton.GetComponent<RectTransform>(), Input.mousePosition))
                {
                    buttonClicked = true;
                }
                yield return null;
            }
        }
        // 버튼이 눌렸거나 비활성화된 경우 (마지막 대사 후) 코루틴 종료
    }

    void OnNextButtonClicked()
    {
        if (!isTextTyping) // 텍스트가 모두 출력된 후에만 다음 대사로 넘어갈 수 있도록
        {
            StartCoroutine(FadeOut(currentBubbleGroup));
        }
    }
}