using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickDetector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("UI Element clicked: " + gameObject.name);
    }
}