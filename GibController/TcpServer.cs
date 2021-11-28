// TCP Server used to accept connections from DCSW (or tester TCPClient
using Google.Protobuf;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GibController
{
    public class TcpServer
    {
        static MainForm myForm;
        TcpListener server;
        TcpClient client;
        NetworkStream stream;
        string myIp;
        string myPort;
        int messageIdIndex = 1; // increments for every messageId
        GetStatusResult.Types.Command currentCommand = new GetStatusResult.Types.Command(); // name of cammand currently executing
        public bool DryRun { get; set; } = false;
        const int inputBufferLen = 128000;
        byte[] inputBuffer = new byte[inputBufferLen];
        public int nGetStatusRequests = 0;
        public int nGetStatusResponses = 0;
        public int nBadCommLenErrors = 0;
        bool delayDockEvents = false;

        public TcpServer(MainForm form)
        {
            myForm = form;
        }

        public bool StartServer(string IP, string port)
        {
            myIp = IP;
            myPort = port;

            myForm.Crawl("StartServer(" + IP + ", " + port + ")");
            if (server != null) StopServer();

            IPAddress ipAddress = IPAddress.Parse(IP);
            IPEndPoint remoteEP = new IPEndPoint(IPAddressToLong(ipAddress), Int32.Parse(port));
            try
            {
                server = new TcpListener(remoteEP);
                server.Start();
                server.BeginAcceptTcpClient(ClientConnected, server);
            }
            catch
            {
                myForm.CrawlError("Couldn't start server");
                return false;
            }
            myForm.Crawl("Server: Waiting for client...");
            return true;
        }

        private long IPAddressToLong(IPAddress address)
        {
            byte[] byteIP = address.GetAddressBytes();

            long ip = (long)byteIP[3] << 24;
            ip += (long)byteIP[2] << 16;
            ip += (long)byteIP[1] << 8;
            ip += (long)byteIP[0];
            return ip;
        }

        public void StopServer()
        {
            myForm.Crawl("StopServer()");
            CloseConnection();
            if (server != null)
            {
                server.Stop();
                server = null;
            }
        }

        void ClientConnected(IAsyncResult result)
        {
            try
            {
                TcpListener server = (TcpListener)result.AsyncState;
                if (server != null)
                {
                    try
                    {
                        client = server.EndAcceptTcpClient(result);
                        stream = client.GetStream();
                        myForm.Crawl("Client connected");

                        try
                        {
                            int maxStorage = 16;
                            try
                            {
                                maxStorage = Int32.Parse(myForm.GibCapacityTxt.Text);
                            }
                            catch { }

                            // Send ID message
                            InductBufferEvent gibevent = new InductBufferEvent();
                            Identification id = new Identification();
                            gibevent.Id = id;
                            id.BootTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(myForm.startDatetime);
                            id.BufferId = myForm.GibIdTxt.Text;
                            id.Capacity = maxStorage;
                            id.CurrentSystemTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
                            id.MessageId = "ID" + messageIdIndex++.ToString();
                            id.SoftwareVersion = myForm.softwareVersion;

                            Send(gibevent);
                        }
                        catch
                        {
                            myForm.CrawlError("ClientConnected: Can't build and send ID event to client");
                        }
                    }
                    catch
                    {
                        ;// myForm.CrawlError("Client connection error");
                    }
                }
            }
            catch
            {
            }
        }

        public bool IsConnected()
        {
            try
            {
                return !(server.Server.Poll(1, SelectMode.SelectRead) && (server.Server.Available == 0));
            }
            catch (SocketException) { return false; }
        }

        void CloseConnection()
        {
            myForm.Crawl("CloseConnection()");

            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }
        }

        public void ReceiveCommand()
        {
            if (stream != null)
            {
                if (!IsConnected())
                {
                    myForm.CrawlError("Have lost connection");
                    StopServer();
                    StartServer(myIp, myPort);
                    return;
                }

                int length = 0;
                while (stream.DataAvailable) inputBuffer[length++] = (byte)stream.ReadByte();
                if (length > 0)
                {
                    // Lazy bytes? since we can't resync.......
                    Thread.Sleep(50);
                    while (stream.DataAvailable) inputBuffer[length++] = (byte)stream.ReadByte();
                }

                if (length > 4)
                {
                    int nDecoded = 0;
                    while (nDecoded < length)
                    {

                        // Length of ProtoBuf in bytes should be first 4 bytes
                        int lenNW = BitConverter.ToInt32(inputBuffer, nDecoded);
                        int len = IPAddress.NetworkToHostOrder(lenNW);
                        nDecoded += 4;
                        if (length - nDecoded < len)
                        {
                            nBadCommLenErrors++;
                            myForm.CrawlError("Received command length error:  len (NW) actual " + len.ToString() + "(" + lenNW.ToString() + ")  " + length.ToString());
                            // Dump whatever is left
                            stream.FlushAsync();
                            nDecoded = length;
                        }
                        else
                        {
                            try
                            {
                                InductBufferRequest request = InductBufferRequest.Parser.ParseFrom(inputBuffer, nDecoded, len);
                                try
                                {
                                    ExecuteCommand(request);
                                }
                                catch
                                {
                                    myForm.CrawlError("Could not execute Request");
                                }
                            }
                            catch
                            {
                                myForm.CrawlError("Received message could not be built into InductBufferRequest");
                            }
                        }
                        nDecoded += len;
                    }
                }
            }
        }

        string lastExecutedCommandId = "";  // Last command we started executing
        string lastEventId = ""; // Last event we sent back
        void CommandComplete()
        {
            lastExecutedCommandId = currentCommand.MessageId;
            lastEventId = currentCommand.MessageId;
            currentCommand.MessageId = "";
        }

        public void ExecuteCommand(InductBufferRequest request)
        {
            // SHow all inbound commands except for GetStatus requests
            if (request.RequestsCase != InductBufferRequest.RequestsOneofCase.GetStatus)
                myForm.Crawl("<== Request: " + request.ToString());

            InductBufferEvent response = new InductBufferEvent();

            switch (request.RequestsCase)
            {
                case InductBufferRequest.RequestsOneofCase.GetStatus:
                    nGetStatusRequests++;
                    string messageId = request.GetStatus.MessageId;
                    GetStatusResult gs = new GetStatusResult();
                    response.GetStatusResult = gs;
                    gs.MessageId = messageId;

                    // Tell about the buffer inventory
                    int nStorage = 0;
                    foreach (GibZone zone in myForm.gibZones)
                    {
                        if (zone.fStorageZone && zone.occupied)
                        {
                            GetStatusResult.Types.InventoryItem item = new GetStatusResult.Types.InventoryItem();
                            item.LocationId = "loc" + nStorage++.ToString();
                            item.ToteId = zone.toteID;
                            gs.BufferInventory.Add(item);
                        }
                    }

                    gs.CurrentCommand = currentCommand;
                    gs.CurrentTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);

                    // Dock status
                    ZoneStatus zoneStatus = new ZoneStatus();
                    if (myForm.gibZones[0].occupied)
                        zoneStatus.TotePresence = ZonePresence.Occupied;
                    else
                        zoneStatus.TotePresence = ZonePresence.Empty;
                    gs.DockStatus = zoneStatus;

                    gs.LastEventId = lastEventId;
                    gs.LastExcutedCommandId = new GetStatusResult.Types.Command();
                    gs.LastExcutedCommandId.MessageId = lastExecutedCommandId;

                    // Infeed status
                    ZoneStatus zoneStatusInfeed = new ZoneStatus();
                    if (myForm.gibZones[3].occupied)
                        zoneStatusInfeed.TotePresence = ZonePresence.Occupied;
                    else
                        zoneStatusInfeed.TotePresence = ZonePresence.Empty;
                    gs.ScannableItem = zoneStatusInfeed;

                    Send(response);
                    nGetStatusResponses++;
                    break;

                case InductBufferRequest.RequestsOneofCase.AcceptFromAgv:
                    currentCommand.MessageId = request.AcceptFromAgv.MessageId;
                    AcceptToteFromAgvResult acceptToteFromAgvResult = new AcceptToteFromAgvResult();
                    response.AcceptFromAgvResult = acceptToteFromAgvResult;
                    acceptToteFromAgvResult.MessageId = currentCommand.MessageId;

                    // Can't be anything already running
                    if (myForm.currentApiTask != null)
                    {
                        myForm.CrawlError("Can't AcceptFromAgv: Another API Task already running");
                        acceptToteFromAgvResult.ResultCode = ResultCode.CommandAlreadyExecuting;
                        Send(response);
                        return;
                    }

                    // Looks good.. let's immediately respond, launch action
                    myForm.currentApiTask = new Task(() =>
                    {
                        // This command is a special case since we respond immediately.... only error would be totes already on conveyor so we check explicitly
                        int nTotesOnInfeed = myForm.SnapshotOccupied();
                        if (nTotesOnInfeed > 0)
                        {
                            myForm.CrawlError("AcceptFromAgv: Tote already on conveyor");
                            acceptToteFromAgvResult.ResultCode = ResultCode.ToteAlreadyPresent;
                        }
                        else
                            acceptToteFromAgvResult.ResultCode = ResultCode.Ok;

                        Send(response);
                        lastEventId = currentCommand.MessageId;
                        delayDockEvents = true;  // Don't wat the ARRIVED DockEvent sent until we are ready for next command!!
                        if (nTotesOnInfeed == 0 && !DryRun) myForm.APIAcceptFromAgv();
                        CommandComplete();
                        myForm.currentApiTask = null;
                        delayDockEvents = false;
                        // Command is done.... upstream will get DockEvent if/when tote arrives on dock
                    });
                    myForm.currentApiTask.Start();
                    break;

                case InductBufferRequest.RequestsOneofCase.SendToAgv:
                    currentCommand.MessageId = request.SendToAgv.MessageId;
                    EjectToteToAgvResult ejectToteToAgvResult = new EjectToteToAgvResult();
                    response.SendToAgvResult = ejectToteToAgvResult;
                    ejectToteToAgvResult.MessageId = currentCommand.MessageId;

                    // Can't be anything already running
                    if (myForm.currentApiTask != null)
                    {
                        myForm.CrawlError("Can't SendToAgv: Another API Task already running");
                        ejectToteToAgvResult.ResultCode = ResultCode.CommandAlreadyExecuting;
                        Send(response);
                        return;
                    }

                    // Looks good.. let's launch and then respond when done
                    myForm.currentApiTask = new Task(() =>
                    {
                        ResultCode ret = ResultCode.Ok;
                        if (!DryRun) ret = myForm.APISendToAgv();
                        ejectToteToAgvResult.ResultCode = ret;
                        Send(response);
                        CommandComplete();
                        myForm.currentApiTask = null;
                    });
                    myForm.currentApiTask.Start();
                    break;

                case InductBufferRequest.RequestsOneofCase.SendToDock:
                    currentCommand.MessageId = request.SendToDock.MessageId;
                    SendToDockResult sendToDockResult = new SendToDockResult();
                    response.SendToDockResult = sendToDockResult;
                    sendToDockResult.MessageId = currentCommand.MessageId;

                    // Can't be anything already running
                    if (myForm.currentApiTask != null)
                    {
                        myForm.CrawlError("Can't SendToDock: Another API Task already running");
                        sendToDockResult.ResultCode = ResultCode.CommandAlreadyExecuting;
                        Send(response);
                        return;
                    }

                    // If this is a SendToDock from Buffer, must ensure Gib motion complete
                    if (request.SendToDock.ToteId != null && request.SendToDock.ToteId.Length > 0)
                        if (!myForm.WaitMotionComplete())
                        {
                            myForm.CrawlError("GIB failed to WaitMotionComplete in TCP-SendToDock request");
                            sendToDockResult.ResultCode = ResultCode.CommandAlreadyExecuting;
                            Send(response);
                            CommandComplete();
                            return;
                        }

                    // Looks good... let's launch and then respond when done
                    myForm.currentApiTask = new Task(() =>
                    {
                        ResultCode ret = ResultCode.Ok;
                        if (!DryRun) ret = myForm.APISendToInfeed(request.SendToDock.ToteId);
                        sendToDockResult.ResultCode = ret;
                        Send(response);
                        CommandComplete();
                        myForm.currentApiTask = null;
                    });
                    myForm.currentApiTask.Start();
                    break;

                case InductBufferRequest.RequestsOneofCase.ScanContents:
                    currentCommand.MessageId = request.ScanContents.MessageId;
                    ScanToteContentsResult scanToteContentsResult = new ScanToteContentsResult();
                    response.ScanContentsResult = scanToteContentsResult;
                    scanToteContentsResult.MessageId = currentCommand.MessageId;

                    // Can't be anything already running
                    if (myForm.currentApiTask != null)
                    {
                        myForm.CrawlError("Can't ScanContents: Another API Task already running");
                        scanToteContentsResult.ResultCode = ResultCode.CommandAlreadyExecuting;
                        Send(response);
                        return;
                    }

                    // Looks good.. let's launch and then respond when done
                    myForm.currentApiTask = new Task(() =>
                    {
                        ResultCode ret = ResultCode.Ok;
                        if (!DryRun)
                        {
                            ret = myForm.APIScanRequest(request.ScanContents.ToteId, request.ScanContents.ToteSource, request.ScanContents.ScanType);
                            if (ret == ResultCode.Ok)
                                PrepareVfxReport(scanToteContentsResult);
                        }

                        scanToteContentsResult.ResultCode = ret;
                        Send(response);
                        CommandComplete();
                        myForm.currentApiTask = null;
                    });
                    myForm.currentApiTask.Start();
                    break;

                case InductBufferRequest.RequestsOneofCase.SendToBuffer:
                    currentCommand.MessageId = request.SendToBuffer.MessageId;
                    SendToBufferResult sendToBufferResult = new SendToBufferResult();
                    response.SendToBufferResult = sendToBufferResult;
                    sendToBufferResult.MessageId = currentCommand.MessageId;

                    // Can't be anything already running
                    if (myForm.currentApiTask != null)
                    {
                        myForm.CrawlError("Can't SendToBuffer: Another API Task already running");
                        sendToBufferResult.ResultCode = ResultCode.CommandAlreadyExecuting;
                        Send(response);
                        return;
                    }

                    // Looks good.. let's launch and then respond when done
                    myForm.currentApiTask = new Task(() =>
                    {
                        ResultCode ret = ResultCode.Ok;
                        if (!DryRun) ret = myForm.APISendToBuffer(request.SendToBuffer.ToteId);
                        sendToBufferResult.ResultCode = ret;
                        Send(response);
                        CommandComplete();
                        myForm.currentApiTask = null;
                    });
                    myForm.currentApiTask.Start();
                    break;

                default:
                    myForm.CrawlError("Unknown command");
                    break;
            }
        }

        void PrepareVfxReport(ScanToteContentsResult scanToteContentsResult)
        {
            scanToteContentsResult.Scan = new ScanResult();
            scanToteContentsResult.Scan.ToteContentsType = ToteContentsType.RobotechTote;

            // Pawing through the VFX data structures to load the ProtoBufs
            // This is pretty fragile code... POC grade
            int slot = 0;
            string decision;
            CellContents.Box vfxbox = myForm.kittingCell.boxes.Find(o => o.ConveyorID == 7);
            try
            {
                string toteID = vfxbox.DunnageID;
                scanToteContentsResult.Scan.BarCode = toteID;
            }
            catch
            {
                scanToteContentsResult.Scan.BarCode = "?";
            }

            // The 24 slots, enumerated 0-23
            for (int col = myForm.vfxSlotTextBoxes.GetLength(1) - 1; col >= 0; col--)
                for (int row = myForm.vfxSlotTextBoxes.GetLength(0) - 1; row >= 0; row--)
                {
                    SlotContent slotContent = new SlotContent();

                    CellContents.Slot s = myForm.kittingCell.slots.Find(o =>
                    o.Box == vfxbox &
                    o.RowNum == row &
                    o.ColNum == col
                    );
                    CellContents.Part p = myForm.kittingCell.parts.Find(o => o.Slot == s);

                    if (s.Contains == CellContents.Contents.Empty)
                    {
                        decision = "Empty";
                        slotContent.Contents = SlotContent.Types.Contents.Empty;
                    }
                    else if (s.Contains == CellContents.Contents.FOD)
                    {
                        decision = "FOD";
                        slotContent.Contents = SlotContent.Types.Contents.NonEmpty;
                    }
                    else if (s.Contains == CellContents.Contents.Part)
                    {
                        if (p != null)
                        {
                            decision = p.Barcode;
                            decision += " " + p.Orientation.ToString("000.0");

                            slotContent.Contents = SlotContent.Types.Contents.NonEmpty;
                            SlotContent.Types.BarCode barcode = new SlotContent.Types.BarCode();
                            barcode.Theta = p.Orientation;
                            barcode.BarCode_ = p.Barcode;
                            slotContent.BarCodes.Add(barcode);
                            if (p.Barcode2 != null && p.Barcode2 != "")
                            {
                                SlotContent.Types.BarCode barcode2 = new SlotContent.Types.BarCode();
                                barcode2.Theta = p.Orientation;
                                barcode2.BarCode_ = p.Barcode2;
                                slotContent.BarCodes.Add(barcode2);
                                decision += " " + p.Barcode2;
                            }
                        }
                        else
                        {
                            decision = "FOD";
                            slotContent.Contents = SlotContent.Types.Contents.NonEmpty;
                        }
                    }
                    else
                    {
                        decision = "FOD";
                        slotContent.Contents = SlotContent.Types.Contents.NonEmpty;
                    }

                    slotContent.SlotId = "slot-" + slot.ToString();
                    scanToteContentsResult.Scan.SlotContents.Add(slotContent);
                    //myForm.Crawl("Slot " + slot.ToString() + ": " + decision);
                    slot++;
                }
        }

        bool currentDockState = false;
        public void SendDockMonitorAsync(bool dockState)
        {
            if (stream == null) return;

            // Won't send changed state until any current command completes
            // Send dockEvent asynchronously except for AcceptFromAgv, which should wait until command complete
            if (!delayDockEvents && dockState != currentDockState)
            {
                InductBufferEvent inductBufferEvent = new InductBufferEvent();
                DockEvent dockEvent = new DockEvent();
                inductBufferEvent.DockEvent = dockEvent;
                dockEvent.MessageId = "DockEvent" + messageIdIndex++.ToString("000000");
                dockEvent.ChangeTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);

                if (dockState)
                {
                    dockEvent.TotePresence = PresenceEventType.ToteArrived;
                }
                else
                {
                    dockEvent.TotePresence = PresenceEventType.ToteDeparted;
                }

                Send(inductBufferEvent);
                Thread.Sleep(50); // Leave a gap in the comm stream.... the current protocol can't resync or retry
                lastEventId = dockEvent.MessageId;
                currentDockState = dockState;
            }
        }

        // Make reentrant to deal with AsyncDockEvent??
        bool fSendBusy = false;
        void Send(InductBufferEvent response)
        {
            while (fSendBusy)
                Thread.Sleep(10);
            fSendBusy = true;
            int len = response.CalculateSize();
            int lenNW = IPAddress.HostToNetworkOrder(len);
            byte[] bytes = BitConverter.GetBytes(lenNW);
            // Show responses other than GetStatus
            if (response.GetStatusResult == null)
                myForm.Crawl("==> " + response.ToString());
            try
            {
                stream.Write(bytes, 0, 4);
                response.WriteTo(stream);
            }
            catch
            {
                myForm.CrawlError("TcpServer.Send() could not write to socket");
            }
            fSendBusy = false;
        }
    }
}