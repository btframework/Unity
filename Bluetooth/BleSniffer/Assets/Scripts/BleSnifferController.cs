using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using wclCommon;
using wclBluetooth;

public class BleSnifferController : MonoBehaviour
{
    [Header("Controls")]
    public Button       btStart;
    public Button       btStop;
    public Button       btClear;
    public TMP_Dropdown cbChannel;

    [Header("Log")]
    public ScrollRect      lbPacketsScroll;
    public TextMeshProUGUI lbPacketsText;

    // ==========================================================
    //  Library + state
    // ==========================================================
    private wclBleSniffer FSniffer;

    private readonly List<string> packetLines = new List<string>();
    private bool _logDirty = false;

    // ==========================================================
    //  Unity lifecycle
    // ==========================================================
    private void Awake()
    {
        btStart.onClick.AddListener(OnStartClick);
        btStop.onClick.AddListener(OnStopClick);
        btClear.onClick.AddListener(OnClearClick);
    }

    private void Start()
    {
        // Mirrors fmMain_Load
        cbChannel.value = 0;

        FSniffer = new wclBleSniffer();
        FSniffer.OnRawPacketReceived     += OnRawPacketReceived;
        FSniffer.OnAdvIndReceived        += OnAdvIndReceived;
        FSniffer.OnAdvDirectIndReceived  += OnAdvDirectIndReceived;
        FSniffer.OnAdvNonConnIndReceived += OnAdvNonConnIndReceived;
        FSniffer.OnAdvScanIndReceived    += OnAdvScanIndReceived;
        FSniffer.OnScanReqReceived       += OnScanReqReceived;
        FSniffer.OnScanRspReceived       += OnScanRspReceived;
        FSniffer.OnConnectIndReceived    += OnConnectIndReceived;
        FSniffer.OnStarted               += OnSnifferStarted;
        FSniffer.OnStopped               += OnSnifferStopped;
    }

    private void Update()
    {
        // Batch log refreshes so bursts of packets cost only one TMP rebuild per frame.
        if (_logDirty)
        {
            _logDirty = false;
            RefreshLog();
        }
    }

    private void OnDestroy()
    {
        if (FSniffer != null)
        {
            try { FSniffer.Stop(); } catch { }
            FSniffer = null;
        }
    }

    // ==========================================================
    //  Button handlers
    // ==========================================================
    private void OnStartClick()
    {
        int Res = FSniffer.Start((byte)(cbChannel.value + 37));
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLine("Start failed: 0x" + Res.ToString("X8"));
    }

    private void OnStopClick()
    {
        int Res = FSniffer.Stop();
        if (Res != wclErrors.WCL_E_SUCCESS)
            AddLine("Stop failed: 0x" + Res.ToString("X8"));
    }

    private void OnClearClick()
    {
        packetLines.Clear();
        _logDirty = true;
    }

    // ==========================================================
    //  Sniffer events
    // ==========================================================
    private void OnSnifferStarted(object sender, EventArgs e)
    {
        AddLine("Started");
    }

    private void OnSnifferStopped(object sender, EventArgs e)
    {
        AddLine("Stopped");
    }

    private void OnRawPacketReceived(object sender, wclBluetoothLePacketHeader header, byte[] payload)
    {
        AddLine("RAW PACKET RECEIVED");
        DumpHeader(header);
        DumpPayload(payload);
    }

    private void OnAdvIndReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long advA, byte[] advData)
    {
        AddLine("ADV_IND RECEIVED");
        AddLine("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(advData);
    }

    private void OnAdvDirectIndReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long advA, long targetA)
    {
        AddLine("ADV_DIRECT_IND RECEIVED");
        AddLine("  AdvA: " + advA.ToString("X12"));
        AddLine("  TargetA: " + targetA.ToString("X12"));
        DumpPduHeader(pduHeader);
    }

    private void OnAdvNonConnIndReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long advA, byte[] advData)
    {
        AddLine("ADV_NONCONN_IND RECEIVED");
        AddLine("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(advData);
    }

    private void OnAdvScanIndReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long advA, byte[] advData)
    {
        AddLine("ADV_SCAN_IND RECEIVED");
        AddLine("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(advData);
    }

    private void OnScanReqReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long scanA, long advA)
    {
        AddLine("SCAN_REQ RECEIVED");
        AddLine("  ScanA: " + scanA.ToString("X12"));
        AddLine("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
    }

    private void OnScanRspReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long advA, byte[] scanRspData)
    {
        AddLine("SCAN_RSP RECEIVED");
        AddLine("  AdvA: " + advA.ToString("X12"));
        DumpPduHeader(pduHeader);
        DumpPayload(scanRspData);
    }

    private void OnConnectIndReceived(object sender, wclBluetoothLeAdvertisingPduHeader pduHeader,
        long initA, long advA, wclBleSnifferLlData llData)
    {
        AddLine("CONNECT_IND RECEIVED");
        AddLine("  InitA: " + initA.ToString("X12"));
        AddLine("  AdvA: " + advA.ToString("X12"));
        AddLine("  LLDATA");
        AddLine("    Aa: " + llData.Aa.ToString("X8"));
        AddLine("    CrcInit: " + llData.CrcInit.ToString("X8"));
        AddLine("    WinSize: " + llData.WinSize.ToString());
        AddLine("    WinOffset: " + llData.WinOffset.ToString());
        AddLine("    Interval: " + llData.Interval.ToString());
        AddLine("    Latency: " + llData.Latency.ToString());
        AddLine("    Timeout: " + llData.Timeout.ToString());
        AddLine("    ChM: " + llData.ChM.ToString("X16"));
        AddLine("    Hop: " + llData.Hop.ToString());
        AddLine("    Sca: " + llData.Sca.ToString());
        DumpPduHeader(pduHeader);
    }

    // ==========================================================
    //  Dump helpers
    // ==========================================================
    private void DumpHeader(wclBluetoothLePacketHeader header)
    {
        AddLine("   HEADER");
        AddLine("  ========");
        AddLine("    Timestamp: " + header.Timestamp.ToString("X8"));
        AddLine("    Access Address: " + header.AccessAddress.ToString("X8"));
        AddLine("    Channel: " + header.Channel.ToString("X2"));
        AddLine("    Crc: " + header.Crc.ToString("X8"));
        AddLine("    Rssi: " + header.Rssi.ToString());
        AddLine(header.Valid ? "    PACKET: OK" : "    PACKET: ERROR");
    }

    private void DumpPduHeader(wclBluetoothLeAdvertisingPduHeader pduHeader)
    {
        DumpHeader(pduHeader.Header);
        AddLine("   PDU HEADER");
        AddLine("  ============");
        AddLine("    ChSel: " + pduHeader.ChSel.ToString());
        AddLine("    TxAdd: " + pduHeader.TxAdd.ToString());
        AddLine("    RxAdd: " + pduHeader.RxAdd.ToString());
    }

    private void DumpPayload(byte[] payload)
    {
        if (payload != null && payload.Length > 0)
        {
            string str = "";
            byte ndx = 1;
            for (int i = 0; i < payload.Length; i++)
            {
                str = str + payload[i].ToString("X2") + " ";
                ndx++;
                if (ndx == 16)
                {
                    AddLine("    " + str);
                    str = "";
                    ndx = 1;
                }
            }
            if (str != "")
                AddLine("    " + str);
        }
        AddLine("");
    }

    // ==========================================================
    //  Log
    // ==========================================================
    private void AddLine(string line)
    {
        packetLines.Add(line ?? "");
        _logDirty = true;
    }

    private void RefreshLog()
    {
        if (lbPacketsText != null)
            lbPacketsText.text = string.Join("\n", packetLines);
    }
}