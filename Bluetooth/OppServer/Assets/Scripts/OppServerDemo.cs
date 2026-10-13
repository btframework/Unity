using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using wclCommon;
using wclCommunication;
using wclBluetooth;

public class OppServerDemo : MonoBehaviour
{
    // ---------------- Top ----------------
    public Button listenButton;
    public Button closeButton;
    public Button disconnectButton;
    public Toggle autoAcceptToggle;

    // ---------------- Clients ----------------
    public Transform  clientsContent;
    public GameObject clientRowPrefab;

    // ---------------- Log ----------------
    public Transform  logContent;
    public GameObject logRowPrefab;
    public Button     clearLogButton;

    // ==================================================================
    //  State
    // ==================================================================
    private wclBluetoothManager _manager;
    private wclRfCommServer     _server;
    private string              _lastFileName;

    private class ClientEntry
    {
        public wclObexOppServer Proc;
        public TableRowUI Row;
        public long Address;
    }

    private readonly List<ClientEntry> _clients = new List<ClientEntry>();
    private TableRowUI _selectedRow;

    // ==================================================================
    //  Lifecycle
    // ==================================================================
    private void Awake()
    {
        listenButton    .onClick.AddListener(OnListenClicked);
        closeButton     .onClick.AddListener(OnCloseClicked);
        disconnectButton.onClick.AddListener(OnDisconnectClicked);
        clearLogButton  .onClick.AddListener(() => ClearChildren(logContent));
    }

    private void Start()
    {
        _manager = new wclBluetoothManager();

        _server = new wclRfCommServer();
        _server.Authentication = false;
        _server.Service     = wclUUIDs.OBEXObjectPushServiceClass_UUID;
        _server.ServiceName = "WCL OPP Server";

        _server.OnListen           += ServerOnListen;
        _server.OnClosed           += ServerOnClosed;
        _server.OnDisconnect       += ServerOnDisconnect;
        _server.OnDestroyProcessor += ServerOnDestroyProcessor;
        _server.OnConnect          += ServerOnConnect;
        _server.OnCreateProcessor  += ServerOnCreateProcessor;
        _server.GetSdpAttributes   += ServerGetSdpAttributes;

        disconnectButton.interactable = false;
        autoAcceptToggle.isOn = true;
    }

    private void OnDestroy()
    {
        try { _server?.Close(); } catch (Exception e) { Debug.LogException(e); }
        try { _manager?.Close(); } catch (Exception e) { Debug.LogException(e); }
        _server = null;
        _manager = null;
    }

    // ==================================================================
    //  SDP attributes
    // ==================================================================
    private void ServerGetSdpAttributes(object sender,
        out ushort[] protocols, out wclBluetoothSdpProfile[] profiles,
        out byte[] formats, out uint cod)
    {
        protocols = new ushort[1];
        protocols[0] = wclUUIDs.OBEX_PROTOCOL_UUID16;

        profiles = new wclBluetoothSdpProfile[1];
        profiles[0].Uuid = wclUUIDs.OBEXObjectPushServiceClass_UUID16;
        profiles[0].Version = 0x0100;

        formats = new byte[1];
        formats[0] = 0xFF;   // All formats

        cod = wclCODs.COD_SERVICE_OBJECT_XFER;
    }

    // ==================================================================
    //  Server events
    // ==================================================================
    private void ServerOnListen(object sender, EventArgs e)
    {
        Trace("Server listening");
        listenButton.interactable = false;
        closeButton.interactable = true;
    }

    private void ServerOnClosed(object sender, int reason)
    {
        Trace("Server closed. Reason: 0x" + reason.ToString("X8"));
        listenButton.interactable = true;
        closeButton.interactable = false;
    }

    private void ServerOnConnect(object sender, wclRfCommServerClientConnection client, int error)
    {
        if (error == wclErrors.WCL_E_SUCCESS)
            Trace("Client " + client.Address.ToString("X12") + " connected");
        else
            Trace("Client " + client.Address.ToString("X12") + " connect failed: 0x" + error.ToString("X8"));
    }

    private void ServerOnDisconnect(object sender, wclRfCommServerClientConnection client, int reason)
        => Trace("Client " + client.Address.ToString("X12") + " disconnected. Reason: 0x" + reason.ToString("X8"));

    private void ServerOnDestroyProcessor(object sender, wclServerClientDataConnection connection)
    {
        var c = (wclRfCommServerClientConnection)connection;
        Trace("Data processor for client " + c.Address.ToString("X12") + " is destroying");
        connection.Processor.Dispose();
    }

    private void ServerOnCreateProcessor(object sender, wclServerClientDataConnection connection)
    {
        var proc = new wclObexOppServer(connection);
        proc.OnConnect        += ProcOnConnect;
        proc.OnDisconnected   += ProcOnDisconnected;
        proc.OnPutBegin       += ProcOnPutBegin;
        proc.OnPutCompleted   += ProcOnPutCompleted;
        proc.OnPutProgress    += ProcOnPutProgress;
        proc.OnGetRequest     += ProcOnGetRequest;
        proc.OnProgress       += ProcOnProgress;
        proc.OnGetCompleted   += ProcOnGetCompleted;
    }

    // ==================================================================
    //  OBEX OPP events
    // ==================================================================
    private void ProcOnConnect(object sender, string description)
    {
        var proc = (wclObexOppServer)sender;
        var conn = (wclRfCommServerClientConnection)proc.Connection;
        long address = conn.Address;

        // The WinForms version asked the user here. In Unity UI a blocking
        // dialog from a synchronous callback is not possible, so we use a toggle.
        bool accept = autoAcceptToggle != null && autoAcceptToggle.isOn;
        Trace("Client " + address.ToString("X12") + " requests connection: " + description
              + " > " + (accept ? "Accepted" : "Rejected"));

        Int32 res;
        if (accept)
        {
            res = proc.Accept("Accepted");
            if (res == wclErrors.WCL_E_SUCCESS)
            {
                var rowGO = Instantiate(clientRowPrefab, clientsContent);
                var row = rowGO.GetComponent<TableRowUI>();
                row.Init(OnClientRowClicked);
                row.SetCell(0, address.ToString("X12"));

                _clients.Add(new ClientEntry { Proc = proc, Row = row, Address = address });
                Trace("OBEX session opened for client: " + address.ToString("X12"));
            }
        }
        else
        {
            res = proc.Reject("Rejected");
        }

        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Operation failed: 0x" + res.ToString("X8"));
            proc.Connection.Disconnect();
        }
    }

    private void ProcOnDisconnected(object sender, int reason, string description)
    {
        var proc = (wclObexOppServer)sender;
        var conn = (wclRfCommServerClientConnection)proc.Connection;
        long address = conn.Address;

        Trace("Client " + address.ToString("X12") + " closed OBEX session with reason: 0x"
              + reason.ToString("X8") + ". Description: " + description);

        RemoveClient(proc);
    }

    private void ProcOnPutBegin(object sender, string name, string description,
        string mime, uint length, out bool accept)
    {
        var proc = (wclObexOppServer)sender;
        var conn = (wclRfCommServerClientConnection)proc.Connection;

        Trace("Client " + conn.Address.ToString("X12") + " sends file " + name
              + " (" + length + " bytes, " + mime + ")");

        accept = autoAcceptToggle != null && autoAcceptToggle.isOn;
        if (accept)
        {
            _lastFileName = SanitizeFileName(name);
            Trace4("Accepted, will save to: " + _lastFileName);
        }
        else
        {
            Trace4("Rejected");
        }
    }

    private void ProcOnPutProgress(object sender, uint position, uint length, out bool @continue)
    {
        // Do not log every progress event - otherwise the log will drown.
        @continue = true;
    }

    private void ProcOnPutCompleted(object sender, int error, Stream stream, out bool accept)
    {
        if (error != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Send file error: 0x" + error.ToString("X8"));
            accept = false;
            return;
        }

        accept = autoAcceptToggle != null && autoAcceptToggle.isOn;
        if (!accept)
        {
            Trace("Received file rejected");
            if (stream != null) { stream.Close(); stream.Dispose(); }
            return;
        }

        try
        {
            string target = Path.Combine(Application.persistentDataPath,
                string.IsNullOrEmpty(_lastFileName) ? "received.bin" : _lastFileName);

            stream.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);

            File.WriteAllBytes(target, bytes);
            Trace("File saved to: " + target);
        }
        catch (Exception e)
        {
            Trace("Save error: " + e.Message);
        }
        finally
        {
            if (stream != null) { stream.Close(); stream.Dispose(); }
        }
    }

    private void ProcOnGetRequest(object sender, string aType,
        out wclObexServerOperationResult result, out Stream stream)
    {
        if (aType == "text/x-vCard" || aType == "x-obex/capability")
        {
            Trace("Requested file type: " + aType);

            string name = (aType == "text/x-vCard") ? "my.vcf" : "caps.xml";
            string path = Path.Combine(Application.persistentDataPath, name);

            if (File.Exists(path))
            {
                stream = new FileStream(path, FileMode.Open);
                result = wclObexServerOperationResult.orSuccess;
                Trace4("Sending " + path);
            }
            else
            {
                Trace4("File not found: " + path);
                stream = null;
                result = wclObexServerOperationResult.orForbidden;
            }
        }
        else
        {
            Trace("Requested unsupported type: " + aType);
            result = wclObexServerOperationResult.orUnsupportedMedia;
            stream = null;
        }
    }

    private void ProcOnProgress(object sender, uint length, uint position)
    {
        // Progress of sending a file to the client. Log rarely.
        if (position == 0 || position == length)
            Trace("Sending file: " + position + " from " + length);
    }

    private void ProcOnGetCompleted(object sender, int error, Stream stream)
    {
        Trace("Get operation completed with result: 0x" + error.ToString("X8"));
        if (stream != null)
        {
            stream.Close();
            stream.Dispose();
        }
    }

    // ==================================================================
    //  Buttons
    // ==================================================================
    private void OnListenClicked()
    {
        Int32 res = _manager.Open();
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Bluetooth manager open failed: 0x" + res.ToString("X8"));
            return;
        }

        wclBluetoothRadio radio;
        res = _manager.GetClassicRadio(out radio);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Get working radio failed: 0x" + res.ToString("X8"));
            _manager.Close();
            return;
        }

        radio.SetDiscoverable(true);
        radio.SetConnectable(true);

        res = _server.Listen(radio);
        if (res != wclErrors.WCL_E_SUCCESS)
        {
            Trace("Failed to start listening: 0x" + res.ToString("X8"));
            _manager.Close();
        }
    }

    private void OnCloseClicked()
    {
        Int32 res = _server.Close();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Close failed: 0x" + res.ToString("X8"));
        else
            _manager.Close();
    }

    private void OnDisconnectClicked()
    {
        var entry = _selectedRow?.Tag as ClientEntry;
        if (entry == null) { Trace("Select client"); return; }

        Int32 res = entry.Proc.Connection.Disconnect();
        if (res != wclErrors.WCL_E_SUCCESS)
            Trace("Disconnect failed: 0x" + res.ToString("X8"));
    }

    private void OnClientRowClicked(TableRowUI row)
    {
        if (_selectedRow != null) _selectedRow.SetSelected(false);
        _selectedRow = row;
        if (_selectedRow != null) _selectedRow.SetSelected(true);

        disconnectButton.interactable = _selectedRow != null;
    }

    // ==================================================================
    //  Helpers
    // ==================================================================
    private void RemoveClient(wclObexOppServer proc)
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            if (ReferenceEquals(_clients[i].Proc, proc))
            {
                if (_selectedRow == _clients[i].Row)
                {
                    _selectedRow = null;
                    disconnectButton.interactable = false;
                }
                Destroy(_clients[i].Row.gameObject);
                _clients.RemoveAt(i);
                return;
            }
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "received.bin";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // ==================================================================
    //  Log
    // ==================================================================
    private void Trace(string msg)
    {
        Debug.Log("[OppServer] " + msg);
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

    private void Trace4(string msg) => Trace("    " + msg);
    private void Trace8(string msg) => Trace("        " + msg);
}