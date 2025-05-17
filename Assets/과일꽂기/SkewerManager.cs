// SkewerManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkewerManager : MonoBehaviour
{
    public List<FruitType> collectedFruitsOnSkewer = new List<FruitType>();
    // public int maxFruitsOnSkewer = 5; // 이 부분은 CustomerOrderManager에서 현재 주문 길이를 가져와서 사용하는 것이 더 좋습니다.

    [Header("과일 꽂히는 시각적 설정")]
    public Transform fruitAttachPoint; // 과일이 꽂히기 시작할 기준점 (꼬치 오브젝트의 자식으로 빈 오브젝트를 만들어 연결하면 편리)
    public float fruitSpacing = 0.5f;  // 꽂히는 과일 이미지들 사이의 간격
    public Vector3 fruitRotation = Vector3.zero; // 꽂힐 때 과일의 기본 회전 값 (필요하다면)
    public float fruitScale = 1f; // 꽂힐 때 과일의 크기 (필요하다면)

    private List<GameObject> attachedFruitObjects = new List<GameObject>();

    // 싱글톤으로 CustomerOrderManager 접근 (CustomerOrderManager에 Instance가 정의되어 있어야 함)
    private CustomerOrderManager orderManager;

    void Start()
    {
        orderManager = CustomerOrderManager.Instance; // 싱글톤 인스턴스 가져오기
        if (orderManager == null)
        {
            Debug.LogError("SkewerManager: CustomerOrderManager 인스턴스를 찾을 수 없습니다!");
        }

        if (fruitAttachPoint == null)
        {
            fruitAttachPoint = transform; // 기준점이 없으면 꼬치 자신을 기준으로
            Debug.LogWarning("SkewerManager: Fruit Attach Point가 설정되지 않았습니다. 꼬치 오브젝트를 기준으로 합니다.");
        }
    }

    public void AddFruitToSkewer(FruitType fruitType, GameObject fruitObject)
    {
        if (orderManager == null || orderManager.currentOrderData == null)
        {
            Debug.LogError("SkewerManager: 현재 주문 정보를 가져올 수 없습니다!");
            Destroy(fruitObject); // 과일 처리 불가
            return;
        }

        int maxFruitsForCurrentOrder = orderManager.currentOrderData.skewerOrder.Count;

        if (collectedFruitsOnSkewer.Count >= maxFruitsForCurrentOrder)
        {
            Debug.Log("꼬치가 현재 주문에 맞게 가득 찼습니다! 더 이상 꽂을 수 없습니다.");
            // 이미 주문 길이만큼 꽂혔으므로, 이 과일은 튕겨나가거나 파괴될 수 있습니다.
            // 또는 아무 동작도 하지 않도록 설정할 수 있습니다.
            // 여기서는 일단 파괴하는 것으로 처리합니다.
            Destroy(fruitObject);
            return;
        }

        // 1. 데이터 추가
        collectedFruitsOnSkewer.Add(fruitType);
        attachedFruitObjects.Add(fruitObject);

        // 2. 시각적 처리 (꼬치에 꽂힌 것처럼 보이게)
        fruitObject.transform.SetParent(fruitAttachPoint); // 꼬치(또는 기준점)의 자식으로 만듦

        // 꽂히는 위치 계산 (예: 아래에서부터 위로 쌓이는 방식)
        // 첫 과일은 fruitAttachPoint의 로컬 y=0, 다음 과일부터 fruitSpacing만큼 위로
        float newYPosition = (collectedFruitsOnSkewer.Count - 1) * fruitSpacing;

        // 과일 스프라이트의 피봇(Pivot) 위치에 따라 조정이 필요할 수 있습니다.
        // 일반적으로 과일 스프라이트의 피봇을 중앙 또는 하단 중앙으로 설정하면 위치 잡기가 수월합니다.
        fruitObject.transform.localPosition = new Vector3(0, newYPosition, 0); // Z축은 꼬치와 같게 (혹은 약간 앞에)
        fruitObject.transform.localRotation = Quaternion.Euler(fruitRotation);
        fruitObject.transform.localScale = Vector3.one * fruitScale;

        // 과일이 꼬치에 꽂혔으므로, 물리적 움직임은 멈추고 Collider도 비활성화 (FruitCollision2D에서 이미 처리)
        // Rigidbody2D rb = fruitObject.GetComponent<Rigidbody2D>();
        // if (rb != null) rb.isKinematic = true;
        // Collider2D col = fruitObject.GetComponent<Collider2D>();
        // if (col != null) col.enabled = false;


        Debug.Log("꼬치에 " + fruitType.ToString() + " 추가. 현재 꼬치: " + string.Join(", ", collectedFruitsOnSkewer.Select(f => f.ToString())));

        // 3. 주문 길이만큼 과일을 모았다면 주문 확인
        if (collectedFruitsOnSkewer.Count == maxFruitsForCurrentOrder)
        {
            bool orderCorrect = orderManager.CheckOrder(new List<FruitType>(collectedFruitsOnSkewer)); // 복사본 전달
            // CheckOrder 함수 내에서 성공/실패에 따른 하트 차감, 다음 주문 로드 등이 처리됩니다.
            // 주문 확인 후 꼬치를 비우는 것은 CheckOrder의 결과에 따라 또는 여기서 바로 할 수 있습니다.
            // 여기서는 CheckOrder가 성공하면 CustomerOrderManager에서 다음 주문을 로드하고,
            // SkewerManager는 그 결과를 받아 꼬치를 비울지 결정해야 합니다.
            // 또는, 성공/실패 여부와 관계없이 일단 비우고, 실패 시 재도전하도록 할 수 있습니다.
            // 현재 게임 플로우상으로는 성공/실패 시 바로 하트가 차감되고 결과가 나오므로,
            // 꼬치를 비우고 새 꼬치를 준비해야 합니다.
            if (orderCorrect)
            {
                Debug.Log("SkewerManager: 주문 성공! 꼬치를 비웁니다. (다음 단계로 진행 준비)");
                // 성공 시 다음 게임 단계로 넘어가는 로직 필요 (예: 설탕 코팅 단계)
                // 지금은 일단 꼬치만 비웁니다.
            }
            else
            {
                Debug.Log("SkewerManager: 주문 실패! 꼬치를 비웁니다. (재도전 준비)");
            }
            ClearSkewer(); // 성공/실패 판정 후 꼬치 비우기
        }
    }

    public void ClearSkewer()
    {
        foreach (GameObject fruitObj in attachedFruitObjects)
        {
            if (fruitObj != null) Destroy(fruitObj);
        }
        attachedFruitObjects.Clear();
        collectedFruitsOnSkewer.Clear();
        Debug.Log("꼬치 비워짐.");
    }

    // (선택 사항) 플레이어가 수동으로 꼬치를 비우거나 제출하는 기능
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space)) // 예: 스페이스바를 누르면 현재 꼬치 제출
    //     {
    //         if (orderManager != null && collectedFruitsOnSkewer.Count > 0)
    //         {
    //             orderManager.CheckOrder(new List<FruitType>(collectedFruitsOnSkewer));
    //             ClearSkewer();
    //         }
    //     }
    // }
}