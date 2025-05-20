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
[CreateAssetMenu(fileName = "NewCustomerOrder", menuName = "Game/Customer Order")]
public class CustomerOrderData : ScriptableObject
{
    public string customerName;
    public Sprite customerSprite; // 손님 캐릭터 이미지 (선택)
    // public Sprite completedSkewerSprite; // 주문서 UI에 완성된 꼬치 이미지를 직접 할당하는 방식 (이전 방식)

    [Header("탕후루 주문 내용")]
    public List<OrderItem> skewerOrder; // 탕후루 과일/아이템 순서

    [Header("손님 대화 내용")]     // ★★★ 추가된 부분 ★★★
    public List<DialogueEntry> dialogueSequence; // 이 손님과의 대화 순서
}