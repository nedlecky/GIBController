using Google.Protobuf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GibController
{
    public partial class ClientForm : Form
    {
        static ManualResetEvent allDone = new ManualResetEvent(false);
        TcpClient client;
        NetworkStream stream;
        const int inputBufferLen = 128000;
        byte[] inputBuffer = new byte[inputBufferLen];
        int nCyclesComplete = 0;

        bool fAbort = false;
        string softwareVersion = "unknown";

        public ClientForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            softwareVersion = Assembly.GetExecutingAssembly().GetName().Name.ToString() + " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

#if DEBUG
            softwareVersion += " RUNNING IN DEBUG MODE";
#endif
            Text = softwareVersion;
            Crawl("Starting " + softwareVersion);

            Left = 0;
            Top = 50;

            // Pull setup info from registry.... these are overwritten with the Save button on the maintenance screen!
            // Note default values are specified here as well
            RegistryKey SoftwareKey = Registry.CurrentUser.OpenSubKey("Software", true);
            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("GibController");
            InterfaceTesterIpTxt.Text = (string)AppNameKey.GetValue("InterfaceTesterIp", "192.168.1.103");
            InterfaceTesterPortTxt.Text = (string)AppNameKey.GetValue("InterfaceTesterPort", "1000");

            Crawl("READY");

            MessageTmr.Interval = 100;
            MessageTmr.Enabled = true;

            InitTmr.Interval = 400;
            InitTmr.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Disconnect();
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

        bool Connect(string IP, string port)
        {
            Crawl("Connect(" + IP.ToString() + ", " + port.ToString() + ")");
            if (client != null) Disconnect();

            try
            {
                Ping ping = new Ping();
                PingReply PR = ping.Send(IP);
                Crawl("Connect Ping: " + PR.Status.ToString());
            }
            catch
            {
                CrawlError("Ping failed");
                return false;
            }

            IPAddress ipAddress = IPAddress.Parse(IP);
            IPEndPoint remoteEP = new IPEndPoint(IPAddressToLong(ipAddress), Int32.Parse(port));

            try
            {
                client = new TcpClient();
                client.Connect(remoteEP);
                stream = client.GetStream();
            }
            catch
            {
                CrawlError("Could not connect");
                return false;
            }

            Crawl("Connected");
            return true;

        }
        void Disconnect()
        {
            Crawl("Disconnect()");

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

        int sendErrorCount = 0;
        bool fSendBusy = false;
        void Send(InductBufferRequest request)
        {
            while (fSendBusy)
                Thread.Sleep(10);
            fSendBusy = true;
            if (stream == null)
            {
                CrawlError("Not connected... stream==null");
                ++sendErrorCount;
                if (sendErrorCount > 5)
                {
                    CrawlError("Trying to bounce socket to GIB");
                    Disconnect();
                    Connect(InterfaceTesterIpTxt.Text, InterfaceTesterPortTxt.Text);
                    sendErrorCount = 0;
                }
                fSendBusy = false;
                return;
            }

            int len = request.CalculateSize();
            int lenNW = IPAddress.HostToNetworkOrder(len);
            byte[] bytes = BitConverter.GetBytes(lenNW);
            //Crawl("==> Request len(NW) " + len.ToString() + "(" + lenNW.ToString() + ")");
            if (request.GetStatus == null) Crawl("==> " + request.ToString());
            try
            {
                stream.Write(bytes, 0, 4);
                request.WriteTo(stream);
            }
            catch
            {
                CrawlError("Send() failed");
                ++sendErrorCount;
                if (sendErrorCount > 5)
                {
                    CrawlError("Trying to bounce socket to GIB");
                    Disconnect();
                    Connect(InterfaceTesterIpTxt.Text, InterfaceTesterPortTxt.Text);
                    sendErrorCount = 0;
                }
            }
            fSendBusy = false;
        }

        public void Receive()
        {
            if (stream != null)
            {
                int length = 0;
                while (stream.DataAvailable && length < inputBufferLen) inputBuffer[length++] = (byte)stream.ReadByte();
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
                        int lenNW = BitConverter.ToInt32(inputBuffer, nDecoded);
                        int len = IPAddress.NetworkToHostOrder(lenNW);
                        nDecoded += 4;
                        if (length - nDecoded < len) // Not enough data in command... don't check for == because could have two or more input messages!
                        {
                            CrawlError("<== Len Provided(NW) actual " + " " + len.ToString() + "(" + lenNW.ToString() + ") " + length.ToString());
                            // Dump whatever is left
                            stream.FlushAsync();
                            nDecoded = length;
                        }
                        else
                        {

                            try
                            {
                                InductBufferEvent response = InductBufferEvent.Parser.ParseFrom(inputBuffer, nDecoded, len);
                                nDecoded += len;
                                InterpretResponse(response);
                            }
                            catch
                            {
                                CrawlError("<== Received message could not be built into proper InductBufferEvent");
                            }
                        }
                    }
                }
            }
        }

        GetStatusResult.Types.Command currentCommand;
        string currentCommandString = "unknown";
        string lastEventId = "unknown";
        string lastRequestId = "unknown";
        ResultCode lastSendToDockResultCode;
        ResultCode lastSendToBufferResultCode;
        ResultCode lastScanContentsResult;
        bool fShowNextGetStatus = true;
        void InterpretResponse(InductBufferEvent response)
        {
            if (response.GetStatusResult == null)
                Crawl("<== Response: " + response.ToString());
            else if (fShowNextGetStatus)
            {
                Crawl("<== Status: " + response.ToString());
                fShowNextGetStatus = false;
            }

            if (response.GetStatusResult != null)
            {
                currentCommand = response.GetStatusResult.CurrentCommand;
                if (currentCommandString != currentCommand.ToString())
                {
                    Crawl("<== GetStatus: New CurrentCommand = " + currentCommand.ToString());
                    currentCommandString = currentCommand.ToString();
                }
                if (lastEventId != response.GetStatusResult.LastEventId)
                {
                    Crawl("<== GetStatus: New LastEventId = " + response.GetStatusResult.LastEventId);
                    lastEventId = response.GetStatusResult.LastEventId;
                }
            }
            else if (response.DockEvent != null)
            {
                Crawl("<== DockEvent: " + response.DockEvent.ToString());
            }
            else if (response.SendToDockResult != null)
            {
                lastSendToDockResultCode = response.SendToDockResult.ResultCode;
            }
            else if (response.SendToBufferResult != null)
            {
                lastSendToBufferResultCode = response.SendToBufferResult.ResultCode;
            }
            else if (response.ScanContentsResult != null)
            {
                lastScanContentsResult = response.ScanContentsResult.ResultCode;
            }
        }

        static Queue<string> crawlMessages = new Queue<string>();

        static void Crawl(string message)
        {
            string datetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            crawlMessages.Enqueue(datetime + " " + message);
        }

        static void CrawlError(string message)
        {
            Crawl("ERROR: " + message);
        }
        private void LimitRTBLength(RichTextBox rtb, int maxLength)
        {
            int currentLength = rtb.TextLength;

            if (currentLength > maxLength)
            {
                rtb.Select(0, currentLength - maxLength);
                rtb.SelectedText = "";
            }
        }

        void FlushCrawl()
        {
            while (crawlMessages.Count() > 0)
            {
                string message = crawlMessages.Dequeue();

                if (message.Contains("ERROR:"))
                {
                    CrawlerRTB.SelectionColor = Color.Red;
                }
                LimitRTBLength(CrawlerRTB, 1000000);
                CrawlerRTB.AppendText(message + "\n");
                CrawlerRTB.ScrollToCaret();
                CrawlerRTB.SelectionColor = System.Drawing.Color.Black;

                try
                {
                    File.AppendAllText(LogfileTxt.Text, message + "\r\n");
                }
                catch
                {

                }

                // Add message to CommRTB as well if it begins with <== or ==>
                if (message.Contains("<==") || message.Contains("==>"))
                {
                    LimitRTBLength(CommRTB, 1000000);
                    //CommRTB.AppendText(datetime);
                    CommRTB.AppendText(message + "\n");
                    CommRTB.ScrollToCaret();
                }
            }
        }

        const int autoGetStatusIntervalMs = 1000;
        int nSinceLastAutoGetStatus = 0;
        private void MessageTmr_Tick(object sender, EventArgs e)
        {
            Receive();
            if (autoGetStatus && !pauseAutoGetStatus)
            {
                if (nSinceLastAutoGetStatus++ > autoGetStatusIntervalMs / MessageTmr.Interval) // GetStatus automatically every autoGetStatusIntervalMs Ms
                {
                    GetStatus();
                    nSinceLastAutoGetStatus = 0;
                }
            }
            FlushCrawl();
            CyclesCompleteLbl.Text = nCyclesComplete.ToString();
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (ConnectBtn.Text == "Connect")
            {
                if (Connect(InterfaceTesterIpTxt.Text, InterfaceTesterPortTxt.Text))
                    ConnectBtn.Text = "Disconnect";
            }
            else
            {
                ConnectBtn.Text = "Connect";
                Disconnect();
            }
        }

        int messageIndex = 1;
        private void AcceptFromAgvBtn_Click(object sender, EventArgs e)
        {
            InductBufferRequest request = new InductBufferRequest();
            AcceptToteFromAgvRequest acceptToteFromAgvRequest = new AcceptToteFromAgvRequest();
            lastRequestId = acceptToteFromAgvRequest.MessageId = "AcceptFromAgv" + messageIndex++.ToString("00000");
            request.AcceptFromAgv = acceptToteFromAgvRequest;

            Send(request);
        }

        private void SendToAGVBtn_Click(object sender, EventArgs e)
        {
            InductBufferRequest request = new InductBufferRequest();
            EjectToteToAgvRequest ejectToteToAgvRequest = new EjectToteToAgvRequest();
            lastRequestId = ejectToteToAgvRequest.MessageId = "SendToAgv" + messageIndex++.ToString("00000");
            request.SendToAgv = ejectToteToAgvRequest;

            Send(request);
        }

        private void SendToDockBtn_Click(object sender, EventArgs e)
        {
            InductBufferRequest request = new InductBufferRequest();
            SendToDockRequest sendToDockRequest = new SendToDockRequest();
            lastRequestId = sendToDockRequest.MessageId = "SendToDock" + messageIndex++.ToString("00000");
            sendToDockRequest.ToteId = DesiredDockToteTxt.Text;
            request.SendToDock = sendToDockRequest;

            Send(request);
        }

        private void ScanRequestBtn_Click(object sender, EventArgs e)
        {
            InductBufferRequest request = new InductBufferRequest();
            ScanToteContentsRequest scanToteContentsRequest = new ScanToteContentsRequest();
            lastRequestId = scanToteContentsRequest.MessageId = "ScanTote" + messageIndex++.ToString("00000");

            // Determine ToteSource
            if (DockRad.Checked)
            {
                scanToteContentsRequest.ToteSource = ScanToteContentsRequest.Types.ToteSource.Dock;
            }
            else if (BufferRad.Checked)
            {
                scanToteContentsRequest.ToteSource = ScanToteContentsRequest.Types.ToteSource.Buffer;
                scanToteContentsRequest.ToteId = DesiredScanToteTxt.Text;
            }
            else if (ScannerRad.Checked)
            {
                scanToteContentsRequest.ToteSource = ScanToteContentsRequest.Types.ToteSource.Scannable;
            }

            // Determine scanType
            scanToteContentsRequest.ScanType = ScanToteContentsRequest.Types.ScanType.FullScan;
            if (ScanTypeAllRad.Checked)
                scanToteContentsRequest.ScanType = ScanToteContentsRequest.Types.ScanType.FullScan;
            else if (ScanTypeIdRad.Checked)
                scanToteContentsRequest.ScanType = ScanToteContentsRequest.Types.ScanType.ToteBarcodeOnly;
            else if (ScanTypeNothingRad.Checked)
                scanToteContentsRequest.ScanType = ScanToteContentsRequest.Types.ScanType.Nothing;

            request.ScanContents = scanToteContentsRequest;
            Send(request);
        }

        private void SendToBufferBtn_Click(object sender, EventArgs e)
        {
            InductBufferRequest request = new InductBufferRequest();
            SendToBufferRequest sendToBufferRequest = new SendToBufferRequest();
            lastRequestId = sendToBufferRequest.MessageId = "SendToBuffer" + messageIndex++.ToString("00000");
            request.SendToBuffer = sendToBufferRequest;
            sendToBufferRequest.ToteId = SpecifiedToteNameTxt.Text;

            Send(request);
        }

        private void GetStatus()
        {
            InductBufferRequest request = new InductBufferRequest();
            GetStatusRequest gs = new GetStatusRequest();
            gs.MessageId = "GetStatus" + messageIndex++.ToString("00000");
            request.GetStatus = gs;
            Send(request);
        }

        private void GetStatusBtn_Click(object sender, EventArgs e)
        {
            GetStatus();
            fShowNextGetStatus = true;
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            RegistryKey SoftwareKey = Registry.CurrentUser.OpenSubKey("Software", true);
            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("GibController");

            AppNameKey.SetValue("InterfaceTesterIp", InterfaceTesterIpTxt.Text);
            AppNameKey.SetValue("InterfaceTesterPort", InterfaceTesterPortTxt.Text);
        }

        private void InitTmr_Tick(object sender, EventArgs e)
        {
            ConnectBtn_Click(null, null);

            InitTmr.Enabled = false;

        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Stress1Btn_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 100; i++)
                GetStatusBtn_Click(null, null);
        }

        bool pauseAutoGetStatus = false;
        bool WaitForStart()
        {
            pauseAutoGetStatus = true;
            bool fDone = false;
            Crawl("AutoCycle: WaitForStart...");
            while (!fDone)
            {
                GetStatus();
                Thread.Sleep(200);
                if (fAbort) fDone = true;
                else if (lastRequestId == lastEventId) // Command is already done!
                    fDone = true;
                else if (currentCommand.MessageId != null)
                {
                    if (currentCommand.MessageId.Length > 1)
                        fDone = true;
                }
            }
            Crawl("AutoCycle: Started");
            pauseAutoGetStatus = false;
            return !fAbort;
        }

        bool WaitForComplete()
        {
            pauseAutoGetStatus = true;
            bool fDone = false;
            Crawl("AutoCycle: WaitForComplete...");
            while (!fDone)
            {
                Thread.Sleep(500);
                GetStatus();
                if (fAbort) fDone = true;
                else if (currentCommand.MessageId == null) fDone = true;
                else if (currentCommand.MessageId.Length < 1) fDone = true;
            }
            Crawl("AutoCycle: Complete");
            pauseAutoGetStatus = false;
            return !fAbort;
        }

        Random random = new Random();

        bool autoGetStatus = true;
        private void AllCycleBtn_Click(object sender, EventArgs e)
        {
            autoGetStatus = false;

            GetStatusBtn_Click(null, null);
            DesiredDockToteTxt.Text = "";
            SpecifiedToteNameTxt.Text = "";
            DockRad.Checked = true;
            nCyclesComplete = 0;

            Crawl("AutoCycle: AllCycle starting");
            fAbort = false;
            int position = 0;
            Task task = new Task(() =>
            {
                while (!fAbort)
                {
                    try
                    {
                        position = int.Parse(PositionNumberTxt.Text);
                    }
                    catch { }
                    Crawl("AutoCycle: Cycling Position " + position.ToString());
                    DesiredDockToteTxt.Text = "p" + position.ToString();

                    lastSendToDockResultCode = ResultCode.CommandAlreadyExecuting;
                    while (!fAbort && lastSendToDockResultCode != ResultCode.Ok)
                    {
                        SendToDockBtn_Click(null, null);
                        WaitForStart();
                        if (lastSendToDockResultCode != ResultCode.Ok) Thread.Sleep(500);
                        WaitForComplete();
                    }

                    if (!fAbort && random.Next(1, 2) == 1)
                    {
                        lastScanContentsResult = ResultCode.CommandAlreadyExecuting;
                        while (!fAbort && lastScanContentsResult != ResultCode.Ok)
                        {
                            ScanRequestBtn_Click(null, null);
                            WaitForStart();
                            if (lastScanContentsResult != ResultCode.Ok) Thread.Sleep(500);
                            WaitForComplete();
                        }

                        lastSendToDockResultCode = ResultCode.CommandAlreadyExecuting;
                        while (!fAbort && lastSendToDockResultCode != ResultCode.Ok)
                        {
                            SendToDockBtn_Click(null, null);
                            WaitForStart();
                            if (lastSendToDockResultCode != ResultCode.Ok) Thread.Sleep(500);
                            WaitForComplete();
                        }
                    }

                    lastSendToBufferResultCode = ResultCode.CommandAlreadyExecuting;
                    while (!fAbort && lastSendToBufferResultCode != ResultCode.Ok)
                    {
                        SendToBufferBtn_Click(null, null);
                        WaitForStart();
                        if (lastSendToBufferResultCode != ResultCode.Ok) Thread.Sleep(500);
                        WaitForComplete();
                    }

                    int increment = 1;
                    try
                    {
                        increment = int.Parse(PositionIncrementTxt.Text);
                    }
                    catch { }

                    int posMax = 15;
                    try
                    {
                        posMax = int.Parse(PositionMaxTxt.Text);
                    }
                    catch { }

                    int posMin = 0;
                    try
                    {
                        posMin = int.Parse(PositionMinTxt.Text);
                    }
                    catch { }

                    position += increment;

                    if (position < posMin) position = posMin;
                    if (position > posMax) position = posMin;

                    PositionNumberTxt.Text = position.ToString();
                    nCyclesComplete++;
                    Crawl("AutoCycle: AllCycle completed " + nCyclesComplete.ToString() + " cycles");

                    autoGetStatus = true;
                }
            });
            task.Start();
        }

        private void DockBounceBtn_Click(object sender, EventArgs e)
        {
            autoGetStatus = false;
            GetStatusBtn_Click(null, null);
            DesiredDockToteTxt.Text = "";
            SpecifiedToteNameTxt.Text = "";
            DockRad.Checked = true;
            nCyclesComplete = 0;

            Crawl("AutoCycle: DockBouce starting");
            fAbort = false;
            Task task = new Task(() =>
            {
                while (!fAbort)
                {
                    lastSendToDockResultCode = ResultCode.CommandAlreadyExecuting;
                    while (!fAbort && lastSendToDockResultCode != ResultCode.Ok)
                    {
                        SendToDockBtn_Click(null, null);
                        WaitForStart();
                        if (lastSendToDockResultCode != ResultCode.Ok) Thread.Sleep(500);
                        WaitForComplete();
                    }

                    if (!fAbort) //&& random.Next(1, 4) == 1)
                    {
                        lastScanContentsResult = ResultCode.CommandAlreadyExecuting;
                        while (!fAbort && lastScanContentsResult != ResultCode.Ok)
                        {
                            ScanRequestBtn_Click(null, null);
                            WaitForStart();
                            if (lastScanContentsResult != ResultCode.Ok) Thread.Sleep(500);
                            WaitForComplete();
                        }

                    }

                    nCyclesComplete++;
                    Crawl("AutoCycle: DockBounce completed " + nCyclesComplete.ToString() + " cycles");
                    autoGetStatus = true;
                }
            });
            task.Start();
        }

        private void AbortBtn_Click(object sender, EventArgs e)
        {
            fAbort = true;
            Crawl("Abort");
        }
    }
}
