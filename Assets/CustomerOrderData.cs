// CustomerOrderData.cs
using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.

// 이 클래스는 꼬치에 들어갈 각 과일/아이템 하나를 의미합니다.
// CustomerOrderData 스크립트 파일 안에 함께 정의합니다.
[System.Serializable] // Unity Inspector 창에 보이도록 설정
public class OrderItem
{
    public FruitType fruit; // 어떤 과일/아이템인지 (FruitType.cs에서 정의한 것 중 선택)
    // 이 아이템의 개별 스프라이트를 여기에 연결할 수도 있지만,
    // 여기서는 완성된 꼬치 스프라이트를 CustomerOrderData에 직접 연결할 겁니다.
}

// 아래 부분이 ScriptableObject를 만드는 핵심입니다.
// [CreateAssetMenu(...)] 를 통해 Unity 에디터에서 이 데이터 파일을 쉽게 만들 수 있게 됩니다.
[CreateAssetMenu(fileName = "새로운손님주문", menuName = "탕후루게임/손님 주문 데이터")]
public class CustomerOrderData : ScriptableObject
{
    public string customerName; // 손님 이름 (예: "끼끼", "뭉뭉")
    // public Sprite customerCharacterSprite; // 손님 캐릭터 이미지 (필요하다면)

    [Header("탕후루 주문 내용")]
    public List<OrderItem> skewerOrder; // 이 손님이 주문한 탕후루 꼬치의 과일/아이템 순서 목록

    [Header("주문서 UI 용")]
    public Sprite completedSkewerSprite; // 완성된 탕후루 꼬치 이미지 (이것을 UI에 보여줄 겁니다!)
                                         // (예: image_e052a8.png 처럼 과일들이 순서대로 꽂힌 이미지)

    // [TextArea(3, 5)] // 여러 줄 텍스트 입력을 위해
    // public string[] dialogueLines; // 손님 대사 (필요하다면)
}