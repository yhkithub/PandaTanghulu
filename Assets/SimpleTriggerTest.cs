// SimpleTriggerTest.cs
using UnityEngine;

public class SimpleTriggerTest : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"!!! Ribbon OnTriggerEnter2D by: {other.name} with tag: {other.tag} !!!");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Stay는 너무 자주 호출될 수 있으니 Enter로 먼저 확인
        // Debug.Log($"--- Ribbon OnTriggerStay2D with: {other.name} ---");
    }

    // 만약 RibbonObj의 Collider가 Trigger가 아니라면 아래 함수들 사용
    void OnCollisionEnter2D(Collision2D collision)
    {
         Debug.Log($"!!! Ribbon OnCollisionEnter2D with: {collision.gameObject.name} with tag: {collision.gameObject.tag} !!!");
    }
}