using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class EndlessOrderGenerator
{
    // ★★★ [수정] score를 인자로 받도록 변경
    public static CustomerOrderData Generate(List<CustomerOrderData> allCustomers, int score)
    {
        if (allCustomers == null || allCustomers.Count == 0) return null;

        CustomerOrderData baseCustomer = allCustomers[Random.Range(0, allCustomers.Count)];
        CustomerOrderData endlessOrder = ScriptableObject.CreateInstance<CustomerOrderData>();
        endlessOrder.name = baseCustomer.name + "_EndlessOrder";

        endlessOrder.customerName = baseCustomer.customerName;
        endlessOrder.customerSprite = baseCustomer.customerSprite;
        endlessOrder.smilingCustomerSprite = baseCustomer.smilingCustomerSprite;
        endlessOrder.toppingItem = baseCustomer.toppingItem;
        
        List<OrderItem> randomSkewer = new List<OrderItem>();
        
        // ★★★ [수정] 점수에 따라 과일 개수가 늘어남 (최대 7개)
        int minFruits = Mathf.Min(3 + score / 5, 6); // 5점마다 1개씩 증가, 최소 3개, 최대 6개
        int maxFruits = Mathf.Min(minFruits + 2, 7);   // 최대 7개
        int fruitCount = Random.Range(minFruits, maxFruits);
        
        var validFruits = System.Enum.GetValues(typeof(FruitType))
                                      .Cast<FruitType>()
                                      .Where(f => (int)f >= 1 && (int)f <= 8)
                                      .ToList();
        for (int i = 0; i < fruitCount; i++)
        {
            randomSkewer.Add(new OrderItem { fruit = validFruits[Random.Range(0, validFruits.Count)] });
        }
        endlessOrder.skewerOrder = randomSkewer;

        // ★★★ [수정] 다양한 랜덤 대사를 추가하고, 그 중 하나를 선택하도록 변경 ★★★
        List<string> randomDialogues = new List<string>
        {
            "안녕! 오늘은 어떤 맛있는 걸로 만들어줄 거야?",
            "왔어! 내 단골집. 오늘은 뭘 먹어볼까?",
            "나 왔어! 실력 발휘 좀 해봐~",
            "음~ 달달한 게 당기는데? 알아서 잘 만들어줘!",
            "오늘따라 운이 좋은 것 같아. 최고의 탕후루를 부탁해!",
            "여기 탕후루가 제일 맛있더라. 기대할게!",
            "지난번에 먹었던 거 진짜 맛있었어! 이번에도 잘 부탁해."
        };

        string selectedDialogue = randomDialogues[Random.Range(0, randomDialogues.Count)];
        endlessOrder.dialogueSequence = new List<DialogueEntry> { new DialogueEntry { speaker = DialogueEntry.Speaker.customer, line = selectedDialogue } };
        
        endlessOrder.presentationDialogueSequence = new List<DialogueEntry> { new DialogueEntry { speaker = DialogueEntry.Speaker.customer, line = "역시 최고야! 다음에 또 올게!" } };

        return endlessOrder;
    }
}