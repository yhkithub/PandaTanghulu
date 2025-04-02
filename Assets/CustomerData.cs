using UnityEngine;

[System.Serializable]
public class CustomerData
{
    public string name; // 예: 원숭이
    public Sprite image; // 손님 캐릭터 이미지
    public string[] preferredCombo; // 좋아하는 과일 조합 (예: ["통", "통", "통"])
    public string bonusItem; // 보너스 아이템 (예: "바나나")
}
