using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BeaconRowUI : MonoBehaviour
{
    public RectTransform indentRect;      // сдвигаем дочерние тексты для иерархии
    public TMP_Text deviceText;           // колонка "Device"
    public TMP_Text frameTypeText;        // колонка "Frame type"
    public Image background;

    public Color normalColor = new Color(1, 1, 1, 0.03f);
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

    private Button _button;
    private Action<BeaconRowUI> _onClick;

    public FrameStorage Frame { get; set; }

    private void Awake() => _button = GetComponent<Button>();

    public void Init(Action<BeaconRowUI> onClick)
    {
        _onClick = onClick;
        SetSelected(false);
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke(this));
        }
    }

    public void SetDeviceText(string value)
    {
        if (deviceText != null) deviceText.text = value;
    }

    public void SetFrameTypeText(string value)
    {
        if (frameTypeText != null) frameTypeText.text = value;
    }

    public void SetIndent(float indent)
    {
        if (indentRect != null)
        {
            var p = indentRect.anchoredPosition;
            indentRect.anchoredPosition = new Vector2(indent, p.y);
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}