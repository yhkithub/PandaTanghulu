using UnityEngine;
using TMPro;

public class MonkeySpeech : MonoBehaviour
{
    public TextMeshProUGUI speechText;

    string[] monkeyDialogues = {
        "바나나 5개로 탕후루 만들어줘!",
        "오늘은 통통통 조합으로 부탁해~",
        "달달한 바나나 최고지!",
        "푸푸 가게는 최고야! 오늘은 통통통 조합으로 부탁해~"
    };

    // 👉 랜덤 대사 반환 함수
    public string GetRandomSpeech()
    {
        int rand = Random.Range(0, monkeyDialogues.Length);
        return monkeyDialogues[rand];
    }
}
