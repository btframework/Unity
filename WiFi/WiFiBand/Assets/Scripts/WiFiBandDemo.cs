using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclWiFi;

public class WiFiBandDemo : MonoBehaviour
{
    // ---------------- Interfaces ----------------
    public Transform  interfacesContent;
    public GameObject interfaceRowPrefab;

    // ---------------- Params ----------------
    public Transform  paramsContent;
    public GameObject paramRowPrefab;

    // ---------------- Values ----------------
    public TMP_Dropdown valuesDropdown;
    public Button       setValueButton;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;

    // ==================================================================
    //  State
    // ==================================================================
    private wclWiFiClient _client;

    private TableRowUI _selectedInterfaceRow;
    private TableRowUI _selectedParamRow;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        setValueButton.onClick.AddListener(OnSetValueClicked);
    }

    private void Start()
    {
        _client = new wclWiFiClient();
        _client.AfterOpen  += ClientAfterOpen;
        _client.BeforeClose += ClientBeforeClose;

        Int32 res = _client.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Open failed: 0x" + res.ToString("X8"));
    }

    private void OnDestroy()
    {
        if (_client == null) return;

        try { _client.Close(); } catch (Exception e) { Debug.LogException(e); }

        _client.AfterOpen   -= ClientAfterOpen;
        _client.BeforeClose -= ClientBeforeClose;
        _client = null;
    }

    // ==================================================================
    //  Client events
    // ==================================================================
    private void ClientAfterOpen(object s, EventArgs e)
    {
        wclWiFiInterfaceData[] ifaces;
        Int32 res = _client.EnumInterfaces(out ifaces);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enum interfaces failed: 0x" + res.ToString("X8"));
            _client.Close();
            return;
        }

        if (ifaces == null || ifaces.Length == 0)
        {
            Log("No Wi-Fi interfaces were found.");
            _client.Close();
            return;
        }

        foreach (var data in ifaces)
        {
            var rowGO = Instantiate(interfaceRowPrefab, interfacesContent);
            var row = rowGO.GetComponent<TableRowUI>();
            row.Init(OnInterfaceRowClicked);
            row.SetCell(0, data.Id.ToString());

            var iface = new wclWiFiInterface(data.Id);
            res = iface.Open();
            if (res != wclErrors.WCL_E_SUCCESS)
            {
                row.SetCell(1, "Open error: 0x" + res.ToString("X8"));
                row.Tag = null;
            }
            else
            {
                row.SetCell(1, data.Description);
                row.Tag = iface;
            }
        }
    }

    private void ClientBeforeClose(object s, EventArgs e)
    {
        foreach (Transform child in interfacesContent)
        {
            var row = child.GetComponent<TableRowUI>();
            if (row == null) continue;
            var iface = row.Tag as wclWiFiInterface;
            iface?.Close();
        }
        ClearChildren(interfacesContent);
        ClearChildren(paramsContent);
        _selectedInterfaceRow = null;
        _selectedParamRow = null;
    }

    // ==================================================================
    //  Row clicks
    // ==================================================================
    private void OnInterfaceRowClicked(TableRowUI row)
    {
        if (_selectedInterfaceRow != null) _selectedInterfaceRow.SetSelected(false);
        _selectedInterfaceRow = row;
        if (_selectedInterfaceRow != null) _selectedInterfaceRow.SetSelected(true);

        ClearChildren(paramsContent);
        ClearChildren(valuesDropdown);
        _selectedParamRow = null;

        var iface = row.Tag as wclWiFiInterface;
        if (iface == null) return;

        wclWiFiInterfaceParameter[] prms;
        Int32 res = iface.EnumParams(out prms);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enumerate parameters failed: 0x" + res.ToString("X8"));
            return;
        }
        if (prms == null || prms.Length == 0) return;

        foreach (var prm in prms)
        {
            var rowGO = Instantiate(paramRowPrefab, paramsContent);
            var paramRow = rowGO.GetComponent<TableRowUI>();
            paramRow.Init(OnParamRowClicked);
            paramRow.Tag = prm;
            paramRow.SetCell(0, prm.Name);
            paramRow.SetCell(1, prm.DisplayName);
            paramRow.SetCell(2, prm.Default);

            string value;
            res = iface.GetParamValue(prm.Name, out value);
            paramRow.SetCell(3, res == wclErrors.WCL_E_SUCCESS
                ? value
                : "Error: 0x" + res.ToString("X8"));
        }
    }

    private void OnParamRowClicked(TableRowUI row)
    {
        if (_selectedParamRow != null) _selectedParamRow.SetSelected(false);
        _selectedParamRow = row;
        if (_selectedParamRow != null) _selectedParamRow.SetSelected(true);

        valuesDropdown.ClearOptions();

        var iface = _selectedInterfaceRow?.Tag as wclWiFiInterface;
        if (iface == null) return;

        var values = new List<string>();
        Int32 res = iface.GetParamValues(row.GetCell(0), values);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Enum values failed: 0x" + res.ToString("X8"));
            return;
        }

        valuesDropdown.AddOptions(values);

        string current = row.GetCell(3);
        int idx = values.IndexOf(current);
        valuesDropdown.SetValueWithoutNotify(idx >= 0 ? idx : 0);
        valuesDropdown.RefreshShownValue();
    }

    // ==================================================================
    //  Set value
    // ==================================================================
    private void OnSetValueClicked()
    {
        if (_selectedParamRow == null || _selectedInterfaceRow == null) return;
        if (valuesDropdown.options.Count == 0) return;
        if (valuesDropdown.value < 0 || valuesDropdown.value >= valuesDropdown.options.Count) return;

        var iface = _selectedInterfaceRow.Tag as wclWiFiInterface;
        if (iface == null) return;

        string paramName = _selectedParamRow.GetCell(0);
        string newValue  = valuesDropdown.options[valuesDropdown.value].text;

        Int32 res = iface.SetParamValue(paramName, newValue);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Log("Set value failed: 0x" + res.ToString("X8"));
            return;
        }

        string value;
        res = iface.GetParamValue(paramName, out value);
        _selectedParamRow.SetCell(3, res == wclErrors.WCL_E_SUCCESS
            ? value
            : "Error: 0x" + res.ToString("X8"));

        int idx = valuesDropdown.options.FindIndex(o => o.text == value);
        valuesDropdown.SetValueWithoutNotify(idx >= 0 ? idx : 0);
        valuesDropdown.RefreshShownValue();
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static void ClearChildren(TMP_Dropdown dd)
    {
        if (dd == null) return;
        dd.ClearOptions();
        dd.RefreshShownValue();
    }

    // ==================================================================
    //  Log
    // ==================================================================
    private void Log(string msg)
    {
        Debug.Log("[WiFiBand] " + msg);
        if (logContent == null || logRowPrefab == null) return;

        var go = Instantiate(logRowPrefab, logContent);
        var row = go.GetComponent<TableRowUI>();
        row.Init(null);
        row.SetCell(0, msg);

        var sr = logContent.GetComponentInParent<ScrollRect>();
        if (sr != null)
        {
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = 0f;
        }
    }
}