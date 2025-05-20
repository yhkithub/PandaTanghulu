// CustomerOrderData.cs
using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.


[System.Serializable]
public class OrderItem
{
    public FruitType fruit; // FruitType 참조 (GameEnums.cs에 정의)
}

[CreateAssetMenu(fileName = "NewCustomerOrder", menuName = "Game/Customer Order")]
public class CustomerOrderData : ScriptableObject
{
    public string customerName;
    public Sprite customerSprite;

    [Header("탕후루 주문 내용")]
    public List<OrderItem> skewerOrder;

    [Header("손님 대화 내용")]
    public List<DialogueEntry> dialogueSequence; // DialogueEntry 참조 (GameEnums.cs에 정의)
}