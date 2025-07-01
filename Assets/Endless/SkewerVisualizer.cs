// SkewerVisualizer.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SkewerVisualizer : MonoBehaviour
{
    public static SkewerVisualizer Instance { get; private set; }

    [Header("시각적 에셋")]
    public GameObject skewerStickPrefab;
    public Sprite sugarCoatingSprite;

    [Header("과일 프리팹 매핑")]
    public List<FruitType> fruitTypes;
    public List<GameObject> fruitPrefabs;
    private Dictionary<FruitType, GameObject> fruitPrefabDict;

    [Header("크기 및 간격 실시간 조절")]
    [Tooltip("꼬치 막대의 최종 목표 높이 (월드 유닛)")]
    public float stickTargetHeight = 8.0f;

    [Tooltip("꼬치 상단에 토핑을 위해 남겨둘 여백 (월드 유닛)")]
    public float topMarginForTopping = 0.8f;


    [Tooltip("막대 너비 대비 과일 크기 (1 = 막대와 같은 너비)")]
    [Range(0.1f, 3.0f)]
    public float fruitScaleMultiplier = 1.0f;
    public float toppingScaleMultiplier = 1.2f; // Inspector에서 조절 가능하게 추가

    [Tooltip("과일과 과일 사이의 간격 (월드 유닛)")]
    [Range(-2.0f, 2.0f)] // ✅ 최소 범위를 음수로 변경
    public float fruitSpacing = 0.2f;

    [Header("디버그용 테스트 설정")]
    public Transform debugTargetParent;
    public List<FruitType> testSkewerOrder;

    // [수정] 토핑 설정을 위한 변수들을 클래스 레벨로 옮기고 선언합니다.
    [Header("토핑 설정")]
    public float toppingYOffset = 0.5f; // 맨 위 과일로부터의 간격

    // [수정] 꼬치의 부모 Transform을 저장할 변수를 클래스 레벨로 선언합니다.
    private Transform skewerParent;



    [ContextMenu("테스트 꼬치 생성 (에디터에서 우클릭)")]
    void GenerateTestSkewerInEditor()
    {
        if (debugTargetParent == null)
        {
            Debug.LogError("테스트를 위해 debugTargetParent를 Inspector에서 할당해주세요!");
            return;
        }
        if (Application.isPlaying)
        {
            var orderData = ScriptableObject.CreateInstance<CustomerOrderData>();
            orderData.skewerOrder = testSkewerOrder.Select(f => new OrderItem { fruit = f }).ToList();

            // ✅ 이 함수를 호출할 때, stickTargetHeight 값을 넘겨주도록 수정합니다.
            DisplaySkewer(debugTargetParent, orderData.skewerOrder, this.stickTargetHeight);
        }
        else
        {
            Debug.LogWarning("이 기능은 플레이 모드에서만 정확하게 동작합니다. 플레이 후 일시정지하고 사용하세요.");
        }
    }

    void Awake()
    {
        // [수정] 싱글톤 인스턴스 할당 로직
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 과일 프리팹 딕셔너리 초기화
        fruitPrefabDict = new Dictionary<FruitType, GameObject>();
        for (int i = 0; i < fruitTypes.Count; i++)
        {
            if (i < fruitPrefabs.Count && fruitPrefabs[i] != null)
            {
                if (!fruitPrefabDict.ContainsKey(fruitTypes[i]))
                    fruitPrefabDict.Add(fruitTypes[i], fruitPrefabs[i]);
            }
        }
    }

    public void DisplaySkewer(Transform parent, List<OrderItem> skewerOrder, float targetHeight)
    {
        this.skewerParent = parent;

        if (parent == null)
        {
            Debug.LogError("SkewerVisualizer: 꼬치를 생성할 부모(parent)가 지정되지 않았습니다.");
            return;
        }

        // 1. 이전 파츠가 있다면 삭제합니다.
        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag("DynamicSkewerPart"))
            {
                Destroy(child.gameObject);
            }
        }

        // 2. 부모 오브젝트에 콜라이더를 설정합니다.
        BoxCollider2D parentCollider = parent.GetComponent<BoxCollider2D>();
        if (parentCollider == null) parentCollider = parent.gameObject.AddComponent<BoxCollider2D>();
        parentCollider.isTrigger = true;
        parent.gameObject.tag = "TanghuluSkewer";

        if (skewerStickPrefab == null)
        {
            Debug.LogError("SkewerVisualizer: 꼬치 막대 프리팹(skewerStickPrefab)이 할당되지 않았습니다.");
            return;
        }

        // 3. 막대기를 생성하고 스케일을 설정합니다.
        GameObject stickInstance = Instantiate(skewerStickPrefab, parent);
        stickInstance.transform.localPosition = Vector3.zero;
        stickInstance.tag = "DynamicSkewerPart";

        var stickRenderer = stickInstance.GetComponent<SpriteRenderer>();
        if (stickRenderer == null || stickRenderer.sprite == null) return;

        // --- 스케일 계산 로직 (수정) ---
        stickInstance.transform.localScale = Vector3.one; // 먼저 로컬 스케일을 1로 초기화
        float stickOriginalHeight = stickRenderer.sprite.bounds.size.y;
        if (stickOriginalHeight == 0) return;

        // 최종 월드 높이가 targetHeight가 되도록 필요한 '로컬 스케일'을 계산합니다.
        // 부모의 월드 스케일(parent.lossyScale.y)을 고려하여 나누어줍니다.
        float requiredLocalScale = targetHeight / (parent.lossyScale.y * stickOriginalHeight);
        stickInstance.transform.localScale = new Vector3(requiredLocalScale, requiredLocalScale, 1f);

        // --- 콜라이더 및 과일 위치 계산 기준 설정 (수정) ---
        // 막대의 '로컬' 크기를 기준으로 모든 것을 계산합니다.
        float stickLocalHeight = stickRenderer.sprite.bounds.size.y * stickInstance.transform.localScale.y;
        float stickLocalWidth = stickRenderer.sprite.bounds.size.x * stickInstance.transform.localScale.x;

        parentCollider.size = new Vector2(stickLocalWidth, stickLocalHeight);
        parentCollider.offset = Vector2.zero;

        // 4. 과일을 배치합니다. (계산 기준 수정)
        // 막대의 로컬 높이를 기준으로 시작점을 잡습니다. (막대기 최상단)
        float scaledTopMargin = topMarginForTopping / parent.lossyScale.y;
        float currentY = (stickLocalHeight / 2f) - scaledTopMargin;

        // skewerOrder를 역순으로 순회하여 아래쪽부터 과일을 쌓도록 합니다.
        foreach (var orderItem in Enumerable.Reverse(skewerOrder))
        {
            if (fruitPrefabDict.TryGetValue(orderItem.fruit, out GameObject fruitPrefab))
            {
                GameObject fruitObj = Instantiate(fruitPrefab, parent);
                fruitObj.tag = "DynamicSkewerPart";

                // 과일의 스케일도 막대의 새 로컬 스케일에 비례하여 설정합니다.
                fruitObj.transform.localScale = stickInstance.transform.localScale * fruitScaleMultiplier;

                var fruitRenderer = fruitObj.GetComponent<SpriteRenderer>();
                if (fruitRenderer != null && fruitRenderer.sprite != null)
                {
                    // 과일의 로컬 높이를 계산합니다.
                    float fruitLocalHeight = fruitRenderer.sprite.bounds.size.y * fruitObj.transform.localScale.y;

                    // 과일 위치를 설정합니다. (아래쪽부터 채워나감)
                    currentY -= (fruitLocalHeight / 2f); // 과일의 중심을 현재 Y에 맞춤
                    fruitObj.transform.localPosition = new Vector3(0, currentY, 0);

                    // 다음 과일이 놓일 위치를 업데이트합니다.
                    float scaledSpacing = fruitSpacing / parent.lossyScale.y;
                    currentY -= (fruitLocalHeight / 2f + scaledSpacing);
                }
            }
        }
    }
    
    public void ApplyMaskedSugarCoating(Transform skewerParent, float progress)
    {
        if (skewerParent == null || sugarCoatingSprite == null) return;

        foreach (Transform part in skewerParent)
        {
            // 과일 파츠에만 적용하고, 꼬치 막대는 건너뜁니다.
            if (!part.CompareTag("DynamicSkewerPart") || part.name.Contains(skewerStickPrefab.name)) continue;

            var partRenderer = part.GetComponent<SpriteRenderer>();
            if (partRenderer == null) continue;

            // 'SugarCoatingEffect' 오브젝트 찾기 또는 생성
            Transform coatingEffectT = part.Find("SugarCoatingEffect");
            SpriteRenderer coatingRenderer;

            if (coatingEffectT == null)
            {
                GameObject coatingObj = new GameObject("SugarCoatingEffect");
                coatingObj.transform.SetParent(part, false);

                SpriteMask mask = coatingObj.AddComponent<SpriteMask>();
                mask.sprite = partRenderer.sprite;

                GameObject actualCoatingSpriteObj = new GameObject("ActualCoating");
                actualCoatingSpriteObj.transform.SetParent(coatingObj.transform, false);
                coatingRenderer = actualCoatingSpriteObj.AddComponent<SpriteRenderer>();

                coatingRenderer.sprite = this.sugarCoatingSprite; // 할당된 하얀 사각형 스프라이트 사용
                coatingRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                coatingRenderer.sortingLayerID = partRenderer.sortingLayerID;
                coatingRenderer.sortingOrder = partRenderer.sortingOrder + 1; // 과일보다 위에 렌더링
            }
            else
            {
                // 자식 오브젝트에서 SpriteRenderer를 찾아야 합니다.
                coatingRenderer = coatingEffectT.GetComponentInChildren<SpriteRenderer>();
            }

            if (coatingRenderer != null)
            {
                // ✅ "너무 하얗다"는 문제를 해결하기 위해 최종 투명도를 0.6 정도로 조절합니다.
                float finalAlpha = Mathf.Clamp01(progress) * 0.3f;
                coatingRenderer.color = new Color(1f, 1f, 1f, finalAlpha);
            }
        }
    }
    
    public void AddTopping(FruitType toppingType)
    {
        if (skewerParent == null) return;

        // 기존 토핑이 있으면 제거
        foreach (Transform child in skewerParent)
        {
            if (child.name.Contains("_Topping"))
            {
                Destroy(child.gameObject);
            }
        }

        if (fruitPrefabDict.TryGetValue(toppingType, out GameObject toppingPrefab))
        {
            Transform topPartTransform = null;
            float highestY = -Mathf.Infinity;

            // 꼬치에서 가장 위에 있는 과일의 'Transform'을 찾습니다.
            foreach (Transform part in skewerParent)
            {
                if (part.CompareTag("DynamicSkewerPart") && !part.name.Contains(skewerStickPrefab.name))
                {
                    if (part.transform.localPosition.y > highestY)
                    {
                        highestY = part.transform.localPosition.y;
                        topPartTransform = part;
                    }
                }
            }

            if (topPartTransform != null)
            {
                var topPartRenderer = topPartTransform.GetComponent<SpriteRenderer>();
                if(topPartRenderer == null) return;

                GameObject toppingInstance = Instantiate(toppingPrefab, skewerParent);
                toppingInstance.name = toppingType.ToString() + "_Topping";

                toppingInstance.transform.localScale = topPartTransform.localScale * toppingScaleMultiplier;

                var toppingRenderer = toppingInstance.GetComponent<SpriteRenderer>();
                if (toppingRenderer != null)
                {
                    toppingRenderer.sortingLayerID = topPartRenderer.sortingLayerID;
                    toppingRenderer.sortingOrder = topPartRenderer.sortingOrder + 1;
                }

                // [핵심 수정] 로컬 좌표를 기준으로 토핑 위치를 계산하고 설정합니다.
                float topPartLocalHeight = topPartRenderer.sprite.bounds.size.y * topPartTransform.localScale.y;
                float toppingLocalHeight = toppingRenderer.sprite.bounds.size.y * toppingInstance.transform.localScale.y;
                
                // 부모 스케일을 고려한 오프셋 계산
                float scaledToppingYOffset = toppingYOffset / skewerParent.lossyScale.y;

                float newToppingLocalY = topPartTransform.localPosition.y + (topPartLocalHeight / 2f) + (toppingLocalHeight / 2f) + scaledToppingYOffset;
                
                toppingInstance.transform.localPosition = new Vector3(topPartTransform.localPosition.x, newToppingLocalY, 0);
            }
        }
    }
}