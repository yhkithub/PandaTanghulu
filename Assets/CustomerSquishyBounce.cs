using UnityEngine;
using System.Collections;

public class CustomerSquishyBounce : MonoBehaviour
{
    public Vector3 startPos = new Vector3(-6f, -10f, 0f);
    public Vector3 endPos = new Vector3(-6f, -1f, 0f);
    public float moveSpeed = 5f;
    public float stretchAmount = 1.3f;
    public float stretchSpeed = 10f;

    // 더 이상 이 스크립트에서 말풍선을 직접 제어하지 않습니다.
    // public CanvasGroup speechBubbleGroup;
    // public TextMeshProUGUI speechText;

    // 👉 초기 크기를 Inspector에서 설정할 수 있도록 public 변수 추가
    public Vector3 initialScale = Vector3.one;

    // private MonkeySpeech monkeySpeech; // 더 이상 랜덤 대사를 사용하지 않으므로 제거

    private CustomerDialogueManager dialogueManager; // DialogueManager 스크립트 참조
    public AudioSource arrivalAudioSource; // Inspector에서 연결

    void Start()
    {
        // 👉 초기 크기 적용
        transform.localScale = initialScale;
        transform.position = startPos;

        // 더 이상 이 스크립트에서 말풍선을 초기화하지 않습니다.
        // speechBubbleGroup.alpha = 0;
        // speechBubbleGroup.gameObject.SetActive(false);

        // 👉 MonkeySpeech 스크립트 가져오기 (더 이상 필요 없을 수 있습니다.)
        // monkeySpeech = GetComponent<MonkeySpeech>();

        // DialogueManager 컴포넌트 찾기 (씬에 하나만 존재한다고 가정)
        dialogueManager = FindObjectOfType<CustomerDialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogError("CustomerDialogueManager 스크립트를 찾을 수 없습니다!");
        }

        // Inspector에서 AudioSource가 연결되었는지 확인 (선택 사항)
        if (arrivalAudioSource == null)
        {
            Debug.LogError("Arrival AudioSource가 연결되지 않았습니다!");
        }

        StartCoroutine(AfterBounce());
    }

    IEnumerator BounceWithSquash()
    {
        Vector3 peakPos = endPos + Vector3.up * 1f;
        float t = 0;
        Vector3 stretchedScale = new Vector3(initialScale.x, initialScale.y * stretchAmount, initialScale.z); // 초기 크기 기반으로 늘림
        Vector3 originalScale = initialScale;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, peakPos, t);
            transform.localScale = Vector3.Lerp(originalScale, stretchedScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(peakPos, endPos, t);
            transform.localScale = Vector3.Lerp(stretchedScale, originalScale, t);
            yield return null;
        }

        // 더 이상 이 스크립트에서 말풍선을 직접 표시하지 않습니다.
        // // 도착 후 말풍선 등장
        // StartCoroutine(ShowSpeechBubble());
    }

    IEnumerator AfterBounce()
    {
        yield return StartCoroutine(BounceWithSquash());
        // 바운스 애니메이션이 끝난 후 등장 사운드 재생
        if (arrivalAudioSource != null)
        {
            arrivalAudioSource.Play();
        }
        // DialogueManager에 첫 번째 대화를 시작하라고 알립니다.
        if (dialogueManager != null)
        {
            dialogueManager.StartFirstDialogue();
        }
    }
}