using UnityEngine;
using TMPro;

public class MonkeySpeech : MonoBehaviour
{
    public TextMeshProUGUI speechText;

    string[] monkeyDialogues = {
        "푸푸~!! 여기 맞지?! 우와, 진짜 가게가 생겼네!! 간판도 귀엽고, 냄새도 완전 최고고...으아아, 지금 침이 고이는 중이야!!"
    };

    // 👉 랜덤 대사 반환 함수
    public string GetRandomSpeech()
    {
        int rand = Random.Range(0, monkeyDialogues.Length);
        return monkeyDialogues[rand];
    }
}
