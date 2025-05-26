// CustomerOrderData.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OrderItem // 이 부분은 FruitType만 가지도록 단순화될 수 있음 (개수가 항상 1이라면)
{
    public FruitType fruit;
}

[CreateAssetMenu(fileName = "새로운손님주문", menuName = "탕후루게임/손님 주문 데이터")]
public class CustomerOrderData : ScriptableObject
{
    [Header("캐릭터")]
    public string customerName;
    public Sprite customerSprite;
    public Sprite smilingCustomerSprite;

    [Header("과일 꽂기 단계 주문")]
    public List<OrderItem> skewerOrder; // ★★★ 과일 꽂기 단계에서 꽂을 기본 과일들 ★★★

    [Header("토핑 아이템 단계 주문")]
    public FruitType toppingItem;       // ★★★ 토핑/상징 아이템 (하나만) ★★★
    public Sprite toppingItemSpriteForHint; // UI에 힌트로 보여줄 토핑 아이템 이미지 (선택 사항)

    [Header("주문서 및 게임 내 이미지")]
    public Sprite completedSkewerSprite;     // 과일 꽂기 완료 후 (설탕 코팅 전) 꼬치 이미지
    public Sprite sugarCoatedSkewerSprite;   // 설탕 코팅 완료 후 (토핑 전) 꼬치 이미지
    public Sprite skewerWithToppingSprite; // ★★★ 새로 추가: 토핑까지 완료된 최종 꼬치 이미지 ★★★


    [Header("손님 대화 내용")]
    public List<DialogueEntry> dialogueSequence;
    [Header("탕후루 전달 후 대화")]
    public List<DialogueEntry> presentationDialogueSequence;
}