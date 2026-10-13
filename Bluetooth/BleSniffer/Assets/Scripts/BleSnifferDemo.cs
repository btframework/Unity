using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclBluetooth;

public class BleSnifferDemo : MonoBehaviour
{
    [Header("Channel")]
    public TMP_Dropdown channelDropdown;

    [Header("Buttons")]
    public Button startButton;
    public Button stopButton;
    public Button clearButton;

    [Header("Log")]
    public TMP_Text logText;
    public ScrollRect logScrollRect;

    private wclBleSniffer _sniffer;

    // ================================================================
    //  Lifecycle
    // ================================================================
    private void Awake()
    {
        startButton.onClick.AddListener(OnStartClicked);
        stopButton .onClick.AddListener(OnStopClicked);
        clearButton.onClick.AddListener(OnClearClicked);
    }

    private void Start()
    {
        // Значения по умолчанию
        if (channelDropdown != null)
        {
            channelDropdown.ClearOptions();
            channelDropdown.AddOptions(new System.Collections.Generic.List<string> { "37", "38", "39" });
            channelDropdown.value = 0;
            channelDropdown.RefreshShownValue();
        }

        _sniffer = new wclBleSniffer();
        _sniffer.OnRawPacketReceived        += SnifferOnRawPacketReceived;
        _sniffer.OnAdvIndReceived           += SnifferOnAdvIndReceived;
        _sniffer.OnAdvDirectIndReceived     += SnifferOnAdvDirectIndReceived;
        _sniffer.OnAdvNonConnIndReceived    += SnifferOnAdvNonConnIndReceived;
        _sniffer.OnAdvScanIndReceived       += SnifferOnAdvScanIndReceived;
        _sniffer.OnScanReqReceived          += SnifferOnScanReqReceived;
        _sniffer.OnScanRspReceived          += SnifferOnScanRspReceived;
        _sniffer.OnConnectIndReceived       += SnifferOnConnectIndReceived;
        _sniffer.OnStarted                  += SnifferOnStarted;
        _sniffer.OnStopped                  += SnifferOnStopped;
    }

    private void OnDestroy()
    {
        if (_sniffer == null) return;

        try { _sniffer.Stop(); }
        catch (Exception e) { Debug.LogException(e); }

        _sniffer.OnRawPacketReceived        -= SnifferOnRawPacketReceived;
        _sniffer.OnAdvIndReceived           -= SnifferOnAdvIndReceived;
        _sniffer.OnAdvDirectIndReceived     -= SnifferOnAdvDirectIndReceived;
        _sniffer.OnAdvNonConnIndReceived    -= SnifferOnAdvNonConnIndReceived;
        _sniffer.OnAdvScanIndReceived       -= SnifferOnAdvScanIndReceived;
        _sniffer.OnScanReqReceived          -= SnifferOnScanReqReceived;
        _sniffer.OnScanRspReceived          -= SnifferOnScanRspReceived;
        _sniffer.OnConnectIndReceived       -= SnifferOnConnectIndReceived;
        _sniffer.OnStarted                  -= SnifferOnStarted;
        _sniffer.OnStopped                  -= SnifferOnStopped;

        _sniffer = null;
    }

    // ================================================================
    //  Buttons
    // ================================================================
    private void OnStartClicked()
    {
        byte channel = (byte)(channelDropdown.value + 37);
        Int32 res = _sniffer.Start(channel);
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Start failed: 0x" + res.ToString("X8"));
    }

    private void OnStopClicked()
    {
        Int32 res = _sniffer.Stop();
        if (res != wclErrors.WCL_E_SUCCESS)
            Log("Stop failed: 0x" + res.ToString("X8"));
    }

    private void OnClearClicked()
    {
        if (logText != null) logText.text = string.Empty;
    }

    // ================================================================
    //  Sniffer events
    // ================================================================
    private void SnifferOnStarted(object sender, EventArgs e) => Log("Started");
    private void SnifferOnStopped(object sender, EventArgs e) => Log("Stopped");

    private void SnifferOnRawPacketReceived(object sender,
        wclBluetoothLePacketHeader header, byte[] payload)
    {
        Log("RAW PACKET RECEIVED");
        DumpHeader(header);
        DumpPayload(payload);
    }

    private void SnifferOnAdvIndReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader, long advA, byte[] advData)
    {
        Log("ADV_IND RECEIVED");
        Log("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(advData);
    }

    private void SnifferOnAdvDirectIndReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader, long advA, long targetA)
    {
        Log("ADV_DIRECT_IND RECEIVED");
        Log("  AdvA: " + advA.ToString("X12"));
        Log("  TargetA: " + targetA.ToString("X12"));
        DumpPduHeader(pduHeader);
    }

    private void SnifferOnAdvNonConnIndReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader, long advA, byte[] advData)
    {
        Log("ADV_NONCONN_IND RECEIVED");
        Log("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(advData);
    }

    private void SnifferOnAdvScanIndReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader, long advA, byte[] advData)
    {
        Log("ADV_SCAN_IND RECEIVED");
        Log("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(advData);
    }

    private void SnifferOnScanReqReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader, long scanA, long advA)
    {
        Log("SCAN_REQ RECEIVED");
        Log("  ScanA: " + scanA.ToString("X12"));
        Log("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
    }

    private void SnifferOnScanRspReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader, long advA, byte[] scanRspData)
    {
        Log("SCAN_RSP RECEIVED");
        Log("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(scanRspData);
    }

    private void SnifferOnConnectIndReceived(object sender,
        wclBluetoothLeAdvertisingPduHeader pduHeader,
        long initA, long advA, wclBleSnifferLlData llData)
    {
        Log("CONNECT_IND RECEIVED");
        Log("  InitA: " + initA.ToString("X12"));
        Log("  AdvA: " + advA.ToString("X12"));
        Log("  LLDATA");
        Log("    Aa: " + llData.Aa.ToString("X8"));
        Log("    CrcInit: " + llData.CrcInit.ToString("X8"));
        Log("    WinSize: " + llData.WinSize.ToString());
        Log("    WinOffset: " + llData.WinOffset.ToString());
        Log("    Interval: " + llData.Interval.ToString());
        Log("    Latency: " + llData.Latency.ToString());
        Log("    Timeout: " + llData.Timeout.ToString());
        Log("    ChM: " + llData.ChM.ToString("X16"));
        Log("    Hop: " + llData.Hop.ToString());
        Log("    Sca: " + llData.Sca.ToString());
        DumpPduHeader(pduHeader);
    }

    // ================================================================
    //  Dump helpers
    // ================================================================
    private void DumpHeader(wclBluetoothLePacketHeader header)
    {
        Log("   HEADER");
        Log("  ========");
        Log("    Timestamp: " + header.Timestamp.ToString("X8"));
        Log("    Access Address: " + header.AccessAddress.ToString("X8"));
        Log("    Channel: " + header.Channel.ToString("X2"));
        Log("    Crc: " + header.Crc.ToString("X8"));
        Log("    Rssi: " + header.Rssi.ToString());
        Log(header.Valid ? "    PACKET: OK" : "    PACKET: ERROR");
    }

    private void DumpPduHeader(wclBluetoothLeAdvertisingPduHeader pduHeader)
    {
        DumpHeader(pduHeader.Header);
        Log("   PDU HEADER");
        Log("  ============");
        Log("    ChSel: " + pduHeader.ChSel.ToString());
        Log("    TxAdd: " + pduHeader.TxAdd.ToString());
        Log("    RxAdd: " + pduHeader.RxAdd.ToString());
    }

    private void DumpPayload(byte[] payload)
    {
        if (payload != null && payload.Length > 0)
        {
            string s = "";
            byte ndx = 1;
            for (int i = 0; i < payload.Length; i++)
            {
                s += payload[i].ToString("X2") + " ";
                ndx++;
                if (ndx == 16)
                {
                    Log("    " + s);
                    s = "";
                    ndx = 1;
                }
            }
            if (s != "") Log("    " + s);
        }
        Log("");
    }

    // ================================================================
    //  Log
    // ================================================================
    private void Log(string msg)
    {
        Debug.Log("[BleSniffer] " + msg);
        if (logText == null) return;

        logText.text += (logText.text.Length > 0 ? "\n" : "") + msg;

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}