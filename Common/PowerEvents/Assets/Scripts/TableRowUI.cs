using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TableRowUI : MonoBehaviour
{
    public TMP_Text[] cells;
    public Image background;

    public Color normalColor   = new Color(1f, 1f, 1f, 0.03f);
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

    private Button _button;
    private Action<TableRowUI> _onClick;

    public object Tag { get; set; }

    private void Awake() => _button = GetComponent<Button>();

    public void Init(Action<TableRowUI> onClick)
    {
        _onClick = onClick;
        SetSelected(false);
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke(this));
        }
    }

    public void SetCell(int index, string value)
    {
        if (cells == null || index < 0 || index >= cells.Length) return;
        if (cells[index] != null) cells[index].text = value ?? "";
    }

    public string GetCell(int index)
    {
        if (cells == null || index < 0 || index >= cells.Length) return "";
        return cells[index] != null ? cells[index].text : "";
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}