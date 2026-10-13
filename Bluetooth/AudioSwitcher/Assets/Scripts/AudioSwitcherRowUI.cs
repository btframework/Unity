using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSwitcherRowUI : MonoBehaviour
{
    [Header("Columns")]
    public TMP_Text idText;
    public TMP_Text friendlyNameText;
    public TMP_Text descriptionText;
    public TMP_Text flowText;
    public TMP_Text rolesText;
    public TMP_Text stateText;
    public TMP_Text macText;
    public TMP_Text serviceText;

    [Header("Selection")]
    public Image background;
    public Color normalColor   = new Color(1f, 1f, 1f, 0.03f);
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 0.35f);

    private Button _button;
    private string _id;
    private string _mac;
    private Action<string> _onClick;

    public string Id  => _id;
    public string Mac => _mac;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void Init(string id, Action<string> onClick)
    {
        _id = id;
        _onClick = onClick;
        SetSelected(false);

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke(_id));
        }
    }

    public void SetData(string id, string friendlyName, string description,
                        string flow, string roles, string state,
                        string mac, string service)
    {
        _id  = id;
        _mac = mac;

        if (idText           != null) idText.text           = id;
        if (friendlyNameText != null) friendlyNameText.text = friendlyName;
        if (descriptionText  != null) descriptionText.text  = description;
        if (flowText         != null) flowText.text         = flow;
        if (rolesText        != null) rolesText.text        = roles;
        if (stateText        != null) stateText.text        = state;
        if (macText          != null) macText.text          = mac;
        if (serviceText      != null) serviceText.text      = service;
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }
}