// SkewerManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkewerManager : MonoBehaviour
{
    public static SkewerManager Instance { get; private set; }

    public List<FruitType> collectedFruitsOnSkewer = new List<FruitType>();

    [Header("과일 꽂히는 시각적 설정")]
    public Transform fruitAttachPoint;
    public float fruitSpacing = 1f;
    public Vector3 fruitRotation = Vector3.zero;

    private List<GameObject> attachedFruitObjects = new List<GameObject>();
    private CustomerOrderManager orderManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 이 오브젝트가 여러 씬에 걸쳐 유지되어야 한다면 아래 주석 해제
            // DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Start()에 있던 내용을 Awake()로 옮기거나, Start()는 그대로 두어도 됨.
        // orderManager 참조는 Start()에서 하는 것이 안전할 수 있음 (CustomerOrderManager.Instance가 Awake에서 설정되므로)
    }

    void Start()
    {
        // orderManager는 CustomerOrderManager.Instance가 확실히 설정된 후인 Start에서 가져오는 것이 좋음
        if (CustomerOrderManager.Instance != null)
        {
            orderManager = CustomerOrderManager.Instance;
        }
        else
        {
            Debug.LogError("SkewerManager: CustomerOrderManager 인스턴스를 찾을 수 없습니다!");
        }

        if (fruitAttachPoint == null)
        {
            fruitAttachPoint = transform;
            Debug.LogWarning("SkewerManager: Fruit Attach Point가 설정되지 않았습니다. 꼬치 오브젝트를 기준으로 합니다.");
        }
    }

    public void AddFruitToSkewer(FruitType fruitType, GameObject fruitObject)
    {
        // 게임 상태가 'Playing'이 아닐 때는 과일 꽂기 로직 실행 안 함
        if (CustomerOrderManager.Instance == null || CustomerOrderManager.Instance.currentGameState != GameState.Playing)
        {
            Debug.Log("SkewerManager: 게임 플레이 중이 아니므로 과일을 꽂을 수 없습니다. 현재 상태: " + (CustomerOrderManager.Instance != null ? CustomerOrderManager.Instance.currentGameState.ToString() : "Unknown"));
            if (fruitObject != null) Destroy(fruitObject);
            return;
        }

        // CustomerOrderManager나 현재 주문 데이터가 없으면 과일 처리하지 않음 (위의 게임 상태 체크로 대부분 커버될 수 있음)
        if (orderManager == null || orderManager.CurrentOrderData == null)
        {
            Debug.LogError("SkewerManager: 현재 주문 정보를 가져올 수 없습니다! (orderManager 또는 CurrentOrderData가 null)");
            if (fruitObject != null) Destroy(fruitObject);
            return;
        }

        int maxFruitsForCurrentOrder = orderManager.CurrentOrderData.skewerOrder.Count;

        if (collectedFruitsOnSkewer.Count >= maxFruitsForCurrentOrder)
        {
            Debug.Log("꼬치가 현재 주문에 맞게 가득 찼습니다! 더 이상 꽂을 수 없습니다.");
            if (fruitObject != null) Destroy(fruitObject);
            return;
        }

        Rigidbody2D rb = fruitObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero; // velocity 사용 권장 (linearVelocity는 Rigidbody에서 사용)
            rb.angularVelocity = 0f;
        }
        Collider2D col = fruitObject.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        collectedFruitsOnSkewer.Add(fruitType);
        attachedFruitObjects.Add(fruitObject);

        // 과일의 원래 월드 스케일 저장 (부모로 설정하기 전)
        Vector3 originalWorldScale = fruitObject.transform.lossyScale;

        fruitObject.transform.SetParent(fruitAttachPoint);

        float newYPosition = (collectedFruitsOnSkewer.Count - 1) * fruitSpacing;
        fruitObject.transform.localPosition = new Vector3(0, newYPosition, 0);
        fruitObject.transform.localRotation = Quaternion.Euler(fruitRotation);

        // 스케일 보정 로직:
        // 목표: fruitObject의 최종 월드 스케일이 originalWorldScale과 동일하게 되도록
        // 현재 fruitAttachPoint의 월드 스케일 가져오기
        Vector3 parentWorldScale = fruitAttachPoint.lossyScale;

        // 부모의 월드 스케일이 0인 경우 (비정상적 상황 방지)
        if (parentWorldScale.x == 0 || parentWorldScale.y == 0 || parentWorldScale.z == 0)
        {
            Debug.LogWarning("SkewerManager: fruitAttachPoint의 월드 스케일 중 0인 축이 있어 스케일 보정이 어렵습니다. 기본 로컬 스케일(1,1,1)을 사용합니다.");
            fruitObject.transform.localScale = Vector3.one;
        }
        else
        {
            // 새로운 로컬 스케일 계산: (원하는 월드 스케일) / (부모의 월드 스케일)
            // 각 축별로 나누어야 함
            fruitObject.transform.localScale = new Vector3(
                originalWorldScale.x / parentWorldScale.x,
                originalWorldScale.y / parentWorldScale.y,
                originalWorldScale.z / parentWorldScale.z
            );
        }

        // Sorting Layer 및 Order in Layer 설정 (이전 답변 참고하여 필요시 추가)
        SpriteRenderer fruitRenderer = fruitObject.GetComponent<SpriteRenderer>();
        if (fruitRenderer != null)
        {
            SpriteRenderer skewerRenderer = GetComponentInParent<SpriteRenderer>(); // 꼬치 자체의 SpriteRenderer 또는 fruitAttachPoint의 부모 등에서 찾아야 함
            if (skewerRenderer != null)
            { // 꼬치 막대 SpriteRenderer가 있다면
                fruitRenderer.sortingLayerID = skewerRenderer.sortingLayerID;
                fruitRenderer.sortingOrder = skewerRenderer.sortingOrder + attachedFruitObjects.Count; // 꼬치보다 앞에, 그리고 겹치도록
            }
            else
            { // 없다면 기본값 또는 다른 기준
              // fruitRenderer.sortingOrder = attachedFruitObjects.Count; // 예시
            }
        }


        Debug.Log("꼬치에 " + fruitType.ToString() + " 추가. 현재 꼬치: " + string.Join(", ", collectedFruitsOnSkewer.Select(f => f.ToString())));

        if (collectedFruitsOnSkewer.Count == maxFruitsForCurrentOrder)
        {
            bool orderCorrect = orderManager.CheckOrder(new List<FruitType>(collectedFruitsOnSkewer));
            if (orderCorrect)
            {
                Debug.Log("SkewerManager: 주문 성공! 꼬치를 비웁니다.");
            }
            else
            {
                Debug.Log("SkewerManager: 주문 실패! 꼬치를 비웁니다.");
            }
            ClearSkewer();
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
}