    // CustomerOrderData.cs
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class OrderItem
    {
        public FruitType fruit;
    }

    [CreateAssetMenu(fileName = "새로운손님주문", menuName = "탕후루게임/손님 주문 데이터")]
    public class CustomerOrderData : ScriptableObject
    {
        public string customerName;
        public Sprite customerSprite;
        public Sprite smilingCustomerSprite; // 손님 웃는 표정 스프라이트 (새로 추가 또는 기존에 있었다면 확인)

        [Header("과일 꽂기 단계 주문")]
        public List<OrderItem> skewerOrder;

        [Header("토핑 아이템 단계 주문")]
        public FruitType toppingItem;
        public Sprite toppingItemSpriteForHint;

        [Header("주문서 및 게임 내 이미지")]
        public Sprite completedSkewerSprite;
        public Sprite sugarCoatedSkewerSprite;
        public Sprite skewerWithToppingSprite;

        [Header("손님 등장 및 주문 시 대화")] // 헤더 이름 변경으로 명확화
        public List<DialogueEntry> dialogueSequence;

        [Header("탕후루 전달 후 감사 대화")] // 헤더 추가 및 필드 명시
        public List<DialogueEntry> presentationDialogueSequence; // 이 필드가 실제로 사용됩니다.
    }
    