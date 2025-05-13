using UnityEngine;
using System.Collections.Generic;

public class FruitCollision2D : MonoBehaviour
{
    private List<string> collectedFruits = new List<string>();
    private Transform skewerTransform;
    private bool isAttached = false;
    private CustomerOrderManager orderManager;
    private HeartManager heartManager; // 하트 매니저

    public string fruitName; // Inspector에서 과일 이름을 설정 (각 프리팹마다 다르게 설정)

    void Start()
    {
        GameObject skewerObject = GameObject.Find("Skewer2D");
        if (skewerObject != null)
        {
            skewerTransform = skewerObject.transform;
        }
        else
        {
            Debug.LogError("Skewer2D 오브젝트를 찾을 수 없습니다!");
        }

        GameObject orderManagerObject = GameObject.Find("CustomerOrderManager");
        if (orderManagerObject != null)
        {
            orderManager = orderManagerObject.GetComponent<CustomerOrderManager>();
        }
        else
        {
            Debug.LogError("CustomerOrderManager 오브젝트를 찾을 수 없습니다!");
        }

        GameObject heartManagerObject = GameObject.Find("HeartManager");
        if (heartManagerObject != null)
        {
            heartManager = heartManagerObject.GetComponent<HeartManager>();
        }
        else
        {
            Debug.LogError("HeartManager 오브젝트를 찾을 수 없습니다!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Skewer") && !isAttached)
        {
            isAttached = true;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector2.zero;
            }
            transform.position = skewerTransform.position + Vector3.up * 0.3f;
            transform.SetParent(skewerTransform);
            collectedFruits.Add(fruitName); // 꼬치에 꽂힌 과일 이름 추가
            Debug.Log("꼬치에 " + fruitName + " 추가. 현재 꼬치: " + string.Join(", ", collectedFruits));

            // 모든 주문 개수만큼 과일을 모았다면 주문 확인
            if (collectedFruits.Count == orderManager.orderLength)
            {
                if (orderManager.CheckOrder(collectedFruits))
                {
                    Debug.Log("주문 성공!");
                    // 성공 처리 로직 (점수 증가, 다음 손님 등)
                    // 일단 꼬치에 붙은 과일들을 파괴
                    foreach (Transform child in skewerTransform)
                    {
                        Destroy(child.gameObject);
                    }
                    collectedFruits.Clear();
                    orderManager.GenerateNewOrder();
                    orderManager.DisplayOrder();
                }
                else
                {
                    Debug.Log("주문 실패!");
                    heartManager?.LoseHeart(); // 하트 감소
                    // 실패 처리 로직 (UI 피드백 등)
                    // 일단 꼬치에 붙은 과일들을 파괴
                    foreach (Transform child in skewerTransform)
                    {
                        Destroy(child.gameObject);
                    }
                    collectedFruits.Clear();
                    orderManager.GenerateNewOrder();
                    orderManager.DisplayOrder();
                }
            }
        }
    }
}