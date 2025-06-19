using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // TODO: 여기에 기존 Shop Scene에 있던 UI 관리, 아이템 구매/판매 등의 로직을 옮겨옵니다.
    // 예를 들어, 아래와 같은 함수들을 추가할 수 있습니다.

    public void BuyItem(string itemName)
    {
        Debug.Log(itemName + " 아이템을 구매했습니다.");
        // 여기에 실제 구매 로직 구현
    }

    public void UpdateGoldUI(int gold)
    {
        Debug.Log("골드를 " + gold + "로 업데이트합니다.");
        // 여기에 골드 UI 업데이트 로직 구현
    }
}