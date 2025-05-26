using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Inspector에서 이 버튼의 자식으로 있는 HoverBorderImage를 연결해주세요.
    public Image hoverBorderImage;

    void Start()
    {
        if (hoverBorderImage != null)
        {
            hoverBorderImage.gameObject.SetActive(false); // 시작 시 비활성화 확실히
        }
        else
        {
            Debug.LogWarning("HoverBorderImage가 이 버튼에 할당되지 않았습니다: " + gameObject.name, this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverBorderImage != null)
        {
            hoverBorderImage.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverBorderImage != null)
        {
            hoverBorderImage.gameObject.SetActive(false);
        }
    }
}