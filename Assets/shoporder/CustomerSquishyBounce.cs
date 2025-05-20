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
    public AudioSource arrivalAudioSource; // Inspector에서 연결

    void Start()
    {
        transform.localScale = initialScale;
        transform.position = startPos;

        dialogueManager = FindFirstObjectByType<CustomerDialogueManager>();
        if (dialogueManager == null)
        {
            Debug.LogError("CustomerDialogueManager 스크립트를 찾을 수 없습니다!");
        }

        // Inspector에서 AudioSource가 연결되었는지 확인 (선택 사항)
        if (arrivalAudioSource == null)
        {
            Debug.LogError("Arrival AudioSource가 연결되지 않았습니다!");
        }
        else
        {
            // 등장 시 바로 사운드 재생 시작
            arrivalAudioSource.Play();
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