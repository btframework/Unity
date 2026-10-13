using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeviceRowUI : MonoBehaviour
{
    public TMP_Text idText;
    public TMP_Text nameText;
    public Image background;

    public Color normalColor   = new Color(1f, 1f, 1f, 0.05f);
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

    private Button _button;
    private string _id;
    private Action<string> _onClick;

    public string Id => _id;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void Init(string id, string name, Action<string> onClick)
    {
        _id = id;
        _onClick = onClick;
        if (idText   != null) idText.text   = id;
        if (nameText != null) nameText.text = name;

        SetSelected(false);

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke(_id));
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}