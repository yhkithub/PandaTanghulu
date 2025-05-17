// CustomerOrderManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UI.Image 사용
using System.Linq; // SequenceEqual 등 사용

public class CustomerOrderManager : MonoBehaviour
{
    [Header("주문서 동적 생성용 UI 설정")]
    public GameObject orderDisplayPanel; // 과일과 꼬치 이미지가 배치될 부모 Panel (Horizontal/Vertical Layout Group 권장)
    public Image skewerStickImagePrefab; // 꼬치 막대 UI Image 프리팹 (선택 사항, Panel 배경으로 처리 가능)
    public Image fruitImagePrefab_UI;    // 주문서에 표시될 개별 과일 UI Image 프리팹 (크기/피봇 미리 설정)

    // 과일 타입에 따른 스프라이트 매핑 (Inspector에서 설정)
    [System.Serializable]
    public struct FruitSpriteMapping
    {
        public FruitType fruitType;
        public Sprite sprite;
    }
    public List<FruitSpriteMapping> fruitSpritesForOrderUI;
    private Dictionary<FruitType, Sprite> fruitSpriteDic;

    [Header("손님 주문 데이터")]
    public List<CustomerOrderData> allCustomerOrders; // 모든 손님 주문 ScriptableObject 리스트 (Inspector에서 연결)
    // 만약 ScriptableObject를 사용하지 않고 CustomerDefinition 클래스를 사용한다면:
    // public List<CustomerDefinition> allCustomerDefinitions;

    private CustomerOrderData currentCustomerOrderData; // 현재 손님의 주문 데이터
    // 또는 private CustomerDefinition currentCustomerDefinition;

    public List<FruitType> currentRequiredFruits = new List<FruitType>(); // 현재 만들어야 할 과일/아이템 리스트 (FruitCollision2D에서 비교용)

    private int currentCustomerIndex = 0; // 다음 손님을 순차적으로 부르기 위한 인덱스 (또는 랜덤)

    // 싱글톤 패턴 (게임 전체에서 하나의 인스턴스만 존재하도록)
    public static CustomerOrderManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환 시 유지하고 싶다면
        }
        else
        {
            Destroy(gameObject);
        }
       // 과일 스프라이트 딕셔너리 초기화
        fruitSpriteDic = new Dictionary<FruitType, Sprite>();
        foreach (var mapping in fruitSpritesForOrderUI)
        {
            if (!fruitSpriteDic.ContainsKey(mapping.fruitType))
            {
                fruitSpriteDic.Add(mapping.fruitType, mapping.sprite);
            }
        }
    }

    void Start()
    {
        if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다!");
            return;
        }
        // 첫 번째 손님 주문 로드 (또는 특정 로직에 따라)
        LoadCustomerOrder(currentCustomerIndex);
    }

    public void LoadNextCustomerOrder()
    {
        currentCustomerIndex++;
        if (currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.Log("모든 손님의 주문을 완료했습니다! (게임 엔딩 또는 다음 단계 로직)");
            // 게임 클리어 또는 반복 로직 처리
            currentCustomerIndex = 0; // 처음부터 다시 시작 (예시)
        }
        LoadCustomerOrder(currentCustomerIndex);
    }

    void LoadCustomerOrder(int customerIndex)
    {
        if (customerIndex < 0 || customerIndex >= allCustomerOrders.Count)
        {
            Debug.LogError("유효하지 않은 손님 인덱스입니다: " + customerIndex);
            return;
        }

        currentCustomerOrderData = allCustomerOrders[customerIndex];
        // currentCustomerDefinition = allCustomerDefinitions[customerIndex]; // 클래스 방식일 경우

        // 주문서 UI 업데이트
        // DisplayOrder();

        // 현재 만들어야 할 과일 리스트 업데이트 (FruitCollision2D에서 사용할 실제 과일 타입 리스트)
        currentRequiredFruits.Clear();
        foreach (OrderItem item in currentCustomerOrderData.skewerOrder)
        // foreach (OrderItem item in currentCustomerDefinition.skewerOrder) // 클래스 방식일 경우
        {
            currentRequiredFruits.Add(item.fruit);
        }

        // (선택 사항) 손님 이름, 대사 등 표시 로직
        // if (customerNameText != null) customerNameText.text = currentCustomerOrderData.customerName;
        // if (dialogueText != null && currentCustomerOrderData.dialogueLines.Length > 0) dialogueText.text = currentCustomerOrderData.dialogueLines[0]; // 첫 번째 대사만 표시 (예시)

        Debug.Log(currentCustomerOrderData.customerName + " 손님의 주문: " + string.Join(", ", currentRequiredFruits.Select(f => f.ToString())));
    }

    void DisplayCurrentOrder() // 이전의 DisplayOrder 함수를 이렇게 수정
    {
        if (orderDisplayPanel == null || fruitImagePrefab_UI == null || currentOrderData == null)
        {
            Debug.LogError("주문서 표시에 필요한 UI 요소 또는 주문 데이터가 없습니다!");
            return;
        }

        // 1. 이전 주문 UI 요소들 삭제
        foreach (Transform child in orderDisplayPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // (선택 사항) 2. 꼬치 막대 이미지 생성
        if (skewerStickImagePrefab != null)
        {
            Image stickInstance = Instantiate(skewerStickImagePrefab, orderDisplayPanel.transform);
            // 꼬치 막대 위치/크기 조정 (Layout Group이 있다면 자동으로 어느정도 맞춰짐)
            // stickInstance.rectTransform.SetAsFirstSibling(); // 다른 과일들보다 뒤에 그려지도록 (필요시)
        }

        // 3. 주문된 과일 이미지들 순서대로 생성 및 배치
        if (currentOrderData.skewerOrder != null && currentOrderData.skewerOrder.Count > 0)
        {
            foreach (OrderItem item in currentOrderData.skewerOrder)
            {
                if (fruitSpriteDic.TryGetValue(item.fruit, out Sprite fruitSpriteToShow))
                {
                    Image fruitUI = Instantiate(fruitImagePrefab_UI, orderDisplayPanel.transform);
                    fruitUI.sprite = fruitSpriteToShow;
                    fruitUI.name = item.fruit.ToString() + "_OrderUI"; // 디버깅용 이름

                    // Layout Group (Horizontal/Vertical)을 orderDisplayPanel에 추가하면
                    // 자식 UI 요소들의 배치(순서, 간격 등)를 자동으로 관리해줍니다.
                    // 수동으로 위치를 잡으려면 fruitUI.rectTransform.anchoredPosition 등을 사용해야 합니다.
                }
                else
                {
                    Debug.LogWarning("주문서 UI: " + item.fruit.ToString() + "에 해당하는 스프라이트를 fruitSpritesForOrderUI에서 찾을 수 없습니다.");
                }
            }
            Debug.Log(currentOrderData.customerName + " 손님의 주문을 UI에 동적으로 표시했습니다.");
        }
        else
        {
            Debug.LogWarning(currentOrderData.customerName + " 손님의 주문 내용(skewerOrder)이 비어있습니다.");
        }
    }

    // 플레이어가 꼬치에 꽂은 과일 리스트와 현재 주문을 비교
    // FruitCollision2D 스크립트에서 호출됨
    public bool CheckOrder(List<FruitType> collectedPlayerFruits)
    {
        if (currentRequiredFruits.Count == 0)
        {
            Debug.LogWarning("현재 생성된 주문이 없습니다.");
            return false;
        }

        // 순서와 내용 모두 일치하는지 확인
        bool orderMatch = collectedPlayerFruits.SequenceEqual(currentRequiredFruits);

        if (orderMatch)
        {
            Debug.Log("주문 성공!");
            // 성공 처리 (점수 증가, 다음 손님 로드 등)
            // HeartManager.Instance.GainPoint(); // (예시) 점수 획득
            LoadNextCustomerOrder(); // 다음 손님 주문으로 넘어감
        }
        else
        {
            Debug.Log("주문 실패! 플레이어: " + string.Join(", ", collectedPlayerFruits.Select(f => f.ToString())) + " / 정답: " + string.Join(", ", currentRequiredFruits.Select(f => f.ToString())));
            HeartManager.Instance.LoseHeart(); // 하트 차감 (즉시)
        }
        return orderMatch;
    }

    // FruitCollision2D에서 과일 이름을 FruitType으로 변환하기 위한 헬퍼 함수 (필요시)
    // 또는 FruitCollision2D에서 FruitType을 직접 갖도록 수정하는 것이 더 좋음
    public FruitType GetFruitTypeByName(string fruitName)
    {
        try
        {
            return (FruitType)System.Enum.Parse(typeof(FruitType), fruitName);
        }
        catch (System.ArgumentException)
        {
            Debug.LogError("알 수 없는 과일 이름입니다: " + fruitName + ". FruitType Enum에 정의되어 있는지 확인하세요.");
            return FruitType.None;
        }
    }
}