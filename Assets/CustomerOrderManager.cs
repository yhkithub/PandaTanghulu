using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Linq 사용을 위해 추가

public class CustomerOrderManager : MonoBehaviour
{
    public List<string> possibleFruits = new List<string> { "딸기", "포도", "귤" }; // 가능한 과일 이름 목록
    public int orderLength = 3; // 손님이 주문할 과일 개수
    public TextMeshProUGUI orderTextUI; // 주문을 표시할 UI 텍스트 (Inspector에서 연결)
    public List<string> currentOrder = new List<string>(); // 현재 손님의 주문

    void Start()
    {
        GenerateNewOrder();
        DisplayOrder();
    }

    public void GenerateNewOrder()
    {
        currentOrder.Clear();
        for (int i = 0; i < orderLength; i++)
        {
            int randomIndex = Random.Range(0, possibleFruits.Count);
            currentOrder.Add(possibleFruits[randomIndex]);
        }
    }

    public void DisplayOrder()
    {
        if (orderTextUI != null)
        {
            orderTextUI.text = "주문: " + string.Join(", ", currentOrder);
        }
        else
        {
            Debug.LogError("OrderTextUI가 연결되지 않았습니다!");
        }
    }

    // 현재 꼬치에 꽂힌 과일과 주문을 비교하는 함수 (FruitCollision2D에서 호출)
    public bool CheckOrder(List<string> collectedFruits)
    {
        // 순서와 내용 모두 일치해야 성공으로 처리
        return collectedFruits.SequenceEqual(currentOrder);
    }
}