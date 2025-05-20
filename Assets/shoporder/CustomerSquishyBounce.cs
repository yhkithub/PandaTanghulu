using UnityEngine;
using System.Collections;

public class CustomerSquishyBounce : MonoBehaviour
{
    public Vector3 startPos = new Vector3(-6f, -10f, 0f);
    public Vector3 endPos = new Vector3(-6f, -1f, 0f);
    public float moveSpeed = 5f;
    public float stretchAmount = 1.3f;
    public float stretchSpeed = 10f;

    public Vector3 initialScale = Vector3.one;

    private CustomerDialogueManager dialogueManager;
    public string arrivalSoundName = "CustomerArrival"; // ★ Inspector에서 AudioManager에 등록한 사운드 이름을 입력합니다.


    void Start()
    {
        transform.localScale = initialScale;
        transform.position = startPos;

        dialogueManager = FindFirstObjectByType<CustomerDialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogError("CustomerDialogueManager 스크립트를 찾을 수 없습니다!");
        }
        
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(arrivalSoundName))
        {
            // PlayOneShotSound는 일회성 효과음에 더 적합합니다.
            // PlaySound는 루프되거나 좀 더 제어가 필요한 사운드에 사용될 수 있습니다.
            // 등장 효과음은 보통 일회성이므로 PlayOneShotSound를 추천합니다.
            AudioManager.Instance.PlayOneShotSound(arrivalSoundName);
            Debug.Log($"Arrival sound '{arrivalSoundName}' requested from AudioManager.");
        }
        else
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("AudioManager 인스턴스를 찾을 수 없어 등장 사운드를 재생할 수 없습니다!");
            }
            if (string.IsNullOrEmpty(arrivalSoundName))
            {
                Debug.LogWarning("Arrival Sound Name이 지정되지 않아 등장 사운드를 재생할 수 없습니다!");
            }
        }

        StartCoroutine(AfterBounce());
    }

    IEnumerator BounceWithSquash()
    {
        Vector3 peakPos = endPos + Vector3.up * 1f;
        float t = 0;
        Vector3 stretchedScale = new Vector3(initialScale.x, initialScale.y * stretchAmount, initialScale.z);
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
    }

    IEnumerator AfterBounce()
    {
        yield return StartCoroutine(BounceWithSquash());
        // DialogueManager에 첫 번째 대화를 시작하라고 알립니다.
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue();
        }
    }
}