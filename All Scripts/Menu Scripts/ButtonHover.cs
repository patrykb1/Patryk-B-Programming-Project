//DOCUMENTED CODE 
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [TextArea]
    public string descriptionText;

    public TextMeshProUGUI descriptionLabel;
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    public void OnPointerEnter(PointerEventData eventData)
    { 
        descriptionLabel.text = descriptionText; 
        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        descriptionLabel.text = ""; 
        onHoverExit?.Invoke();
    }
}