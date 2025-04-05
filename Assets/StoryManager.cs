using UnityEngine;
using TMPro;

public class StoryManager : MonoBehaviour
{
    [System.Serializable]
    public class StoryStep { public string text; public Sprite backgroundImage; }

    public StoryStep[] storySteps;
    public SpriteRenderer backgroundRenderer; // 월드 공간 배경
    public TextMeshProUGUI storyText;         // UI 텍스트
    public GameObject nextButton;             // UI 버튼
    public GameObject ribbonObj;              // 월드 공간 리본

    private int currentStep = 0;

    void Start()
    {
        if (ribbonObj != null) ribbonObj.SetActive(false);
        ShowStep(currentStep);
    }

    public void OnNextClicked()
    {
        currentStep++;
        if (currentStep >= storySteps.Length)
        {
            nextButton.SetActive(false);
            if (ribbonObj != null) ribbonObj.SetActive(true);
            return;
        }
        ShowStep(currentStep);
    }

    void ShowStep(int index)
    {
        storyText.text = storySteps[index].text;
        if (backgroundRenderer != null)
            backgroundRenderer.sprite = storySteps[index].backgroundImage;
        if (index == storySteps.Length - 1)
        {
            nextButton.SetActive(false);
            if (ribbonObj != null) ribbonObj.SetActive(true);
        }
    }
}
