using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class DoubleClickEmitter : MonoBehaviour, IPointerClickHandler
{
    public float doubleClickTime = 0.3f;
    public Action onDoubleClick;

    private float _lastClickTime = -999f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float now = Time.unscaledTime;
        if (now - _lastClickTime <= doubleClickTime)
        {
            _lastClickTime = -999f;
            onDoubleClick?.Invoke();
        }
        else
        {
            _lastClickTime = now;
        }
    }
}