// CustomerOrderData.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OrderItem
{
    public FruitType fruit;
}

[CreateAssetMenu(fileName = "NewCustomerOrder", menuName = "Customer/Customer Order")]
public class CustomerOrderData : ScriptableObject
{
    public string customerName;
    public Sprite customerSprite;
    public Sprite smilingCustomerSprite; // 손님 웃는 표정 스프라이트

    [Header("과일 꽂기 단계 주문")]
    public List<OrderItem> skewerOrder;

    [Header("토핑 아이템 단계 주문")]
    public FruitType toppingItem;
    public Sprite toppingItemSpriteForHint;

    [Header("주문서 및 게임 내 이미지")]
    public Sprite completedSkewerSprite;
    public Sprite sugarCoatedSkewerSprite;
    public Sprite skewerWithToppingSprite;

    [Header("설탕 끓이기 난이도 설정")]
    [Tooltip("스테이지의 기본 속도입니다. 값이 클수록 타이밍 바가 빨라집니다. (예: 1.0, 1.2, 1.5)")]
    public float sugarBoilingSpeed = 1.0f;

    [Tooltip("성공 영역(Success Zone)의 너비입니다. 값이 작을수록 어려워집니다. (예: 150, 120, 100)")]
    public float sugarBoilingSuccessZoneWidth = 150f;

    [Header("손님 등장 및 주문 시 대화")] 
    public List<DialogueEntry> dialogueSequence;

    [Header("탕후루 전달 후 감사 대화")]
    public List<DialogueEntry> presentationDialogueSequence;


}
    