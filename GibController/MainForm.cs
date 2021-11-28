// MainForm for the C# Induct Buffer interface
using LibplctagWrapper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GibController
{
    public partial class MainForm : Form
    {
        TcpServer commandServer;
        Libplctag libPlcClient;
        public List<GibZone> gibZones = new List<GibZone>();
        Tag GibSystemState;
        Tag[] conveyorSensors = new Tag[8];
        Tag E_Stop_Pressed;
        Tag HMI_AutoMode;
        Tag HMI_PB_Reset;
        Tag HMI_PB_Start;
        Tag HMI_PB_Stop;
        Tag HMI_Alarm_Silence;
        Tag HMI_Infeed_Mode;
        Tag HMI_Outfeed_Single;
        Tag HMI_Outfeed_Batch;
        Tag HMI_Outfeed_Auto;
        Tag Motion_Complete;
        Tag[] zoneEnableTags = new Tag[8];
        Thread plcGetDataThread;
        bool EStop = true; // Copy of Gib's E-Stop state
        bool GibMotionComplete = false; // Copy of Gib's Motion_Complete tag
        string GibStateName; // Copy of GibState

        // VFX Interface
        public CellContents kittingCell;
        public TextBox[,] vfxSlotTextBoxes;
        VfxControl vfxControl;
        Task currentVfxTask;

        // System housekeeping
        public string softwareVersion = "unknown";  // Set at load
        public DateTime startDatetime;
        public TimeSpan uptime;

        // Inventory System
        Inventory inventory;
        int currentBufferToteCount = 0;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            softwareVersion = Assembly.GetExecutingAssembly().GetName().Name.ToString() + " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

#if DEBUG
            softwareVersion += " RUNNING IN DEBUG MODE";
#endif
            Text = softwareVersion;
            Crawl("Starting " + softwareVersion);

            Left = 750;
            Top = 50;

            startDatetime = DateTime.UtcNow;
            StartDatetimeLbl.Text = startDatetime.ToString("yyyy-MM-ddTHH:mm:ssZ");

            Connect();
        }

        bool forceClose = false;
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (forceClose) return;

            string message = "Really Exit?";
            string caption = "Closing " + Assembly.GetExecutingAssembly().GetName().Name.ToString();
            var result = MessageBox.Show(message, caption,
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);

            e.Cancel = (result == DialogResult.No);

            if (!e.Cancel)
            {
                AutoReconnectChk.Checked = false;
                CloseTmr.Interval = 1001;
                CloseTmr.Enabled = true;
                e.Cancel = true; // Let the close out timer shut us down
            }
        }

        private void Connect()
        {
            Crawl("Connect()");
            ConnectBtn.Enabled = false;
            DisconnectBtn.Enabled = true;

            // Pull setup info from registry.... these are overwritten with the Save button on the maintenance tab
            // Note default values are specified here as well
            RegistryKey SoftwareKey = Registry.CurrentUser.OpenSubKey("Software", true);
            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("GibController");
            GibIdTxt.Text = (string)AppNameKey.GetValue("GibId", "GIB1");
            GibCapacityTxt.Text = (string)AppNameKey.GetValue("GibCapacity", "16");
            GibPlcIpTxt.Text = (string)AppNameKey.GetValue("GibPlcIp", "192.168.1.40");
            VfxPlcIpTxt.Text = (string)AppNameKey.GetValue("VfxPlcIp", "192.168.1.10");
            VfxInsightIpTxt.Text = (string)AppNameKey.GetValue("VfxInsightIp", "192.168.1.60");
            VfxInsightPortTxt.Text = (string)AppNameKey.GetValue("VfxInsightPort", "3000");
            CommandServerIpTxt.Text = (string)AppNameKey.GetValue("CommandServerIp", "192.168.1.103");
            CommandServerPortTxt.Text = (string)AppNameKey.GetValue("CommandServerPort", "1000");
            InventoryFolderTxt.Text = (string)AppNameKey.GetValue("InventoryFolder", "C:/Users/GIB1/Desktop/Inventory");
            LogfileTxt.Text = (string)AppNameKey.GetValue("Logfiler", "C:/Users/GIB1/Desktop/giblog.txt");

            inventory = new Inventory(this, InventoryFolderTxt.Text);
            LoadInventoryBtn_Click(null, null);

            // Bring up the PLC interface
            libPlcClient = new Libplctag();
            plcTagReadFail = 0;
            plcTagReadRetry = 0;
            plcTagWriteFail = 0;
            plcTagWriteRetry = 0;

            // Setup system tags
            GibSystemState = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "System_State", DataType.Int32, 1);
            libPlcClient.AddTag(GibSystemState);

            E_Stop_Pressed = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "E_Stop_Pressed", DataType.Int8, 1);
            libPlcClient.AddTag(E_Stop_Pressed);

            HMI_AutoMode = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_AutoMode", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_AutoMode);

            HMI_PB_Reset = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_PB_Reset", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_PB_Reset);

            HMI_PB_Start = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_PB_Start", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_PB_Start);

            HMI_PB_Stop = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_PB_Stop", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_PB_Stop);

            HMI_Alarm_Silence = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Alarm_Silence", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_Alarm_Silence);

            // Other HMI controls of interest
            HMI_Infeed_Mode = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Infeed_Mode", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_Infeed_Mode);

            HMI_Outfeed_Single = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Outfeed_Single", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_Outfeed_Single);

            HMI_Outfeed_Batch = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Outfeed_Batch", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_Outfeed_Batch);

            HMI_Outfeed_Auto = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Outfeed_Auto", DataType.Int8, 1);
            libPlcClient.AddTag(HMI_Outfeed_Auto);

            Motion_Complete = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Motion_Complete", DataType.Int8, 1);
            libPlcClient.AddTag(Motion_Complete);

            // Build the zone tracker controls
            int xSpacing = 5;
            int ySpacing = 5;
            int width = 80;
            int height = 20;
            int startX = 10;
            int startY = 205;
            int tabIndex = 100;
            // Infeed
            for (int zone = 1; zone <= 4; zone++)
            {
                Label label1 = new Label();
                label1.Text = "Infeed_Zone" + (5 - zone).ToString() + "_PE"; // Turned these around when we replaced LWST with VFX
                label1.Name = label1.Text + "_Lbl";
                label1.Size = new Size(width, height);
                label1.Location = new Point(startX + (width + xSpacing) * (zone - 1), startY);
                label1.TabIndex = tabIndex++;
                label1.TextAlign = ContentAlignment.MiddleCenter;
                this.RunTabPage.Controls.Add(label1);

                Label label2 = new Label();
                label2.Text = label1.Text;
                label2.Size = label1.Size;
                label2.Location = new Point(label1.Location.X, label1.Location.Y - height);
                label2.TextAlign = ContentAlignment.MiddleCenter;
                this.RunTabPage.Controls.Add(label2);

                GibZone gibzone = new GibZone();
                gibzone.toteID = "";
                gibzone.Label1 = label1;
                gibzone.Label2 = label2;
                gibzone.fStorageZone = false;
                gibZones.Add(gibzone);
            }

            // Lift
            for (int i = 1; i <= 2; i++)
            {
                Label label1 = new Label();
                label1.Text = "Lift_Conv_PE" + i.ToString();
                label1.Name = label1.Text + "_Lbl";
                label1.Size = new Size(width, height);
                label1.Location = new Point(startX + (width + xSpacing) * (i + 3), startY);
                label1.TabIndex = tabIndex++; ;
                label1.TextAlign = ContentAlignment.MiddleCenter;
                this.RunTabPage.Controls.Add(label1);

                Label label2 = new Label();
                label2.Text = label1.Text;
                label2.Size = label1.Size;
                label2.Location = new Point(label1.Location.X, label1.Location.Y - height);
                label2.TextAlign = ContentAlignment.MiddleCenter;
                this.RunTabPage.Controls.Add(label2);

                GibZone gibzone = new GibZone();
                gibzone.toteID = "";
                gibzone.Label1 = label1;
                gibzone.Label2 = label2;
                gibzone.fStorageZone = false;
                gibZones.Add(gibzone);
            }

            // Buffer
            int maxStorage = 16;
            try
            {
                maxStorage = Int32.Parse(GibCapacityTxt.Text);
            }
            catch { }
            int nStorage = 0;
            for (int level = 1; level <= 5; level++)
                for (int zone = 4; zone >= 1; zone--)
                {
                    if (nStorage < maxStorage)
                    {
                        Label label1 = new Label();
                        label1.Text = "L" + level.ToString() + "_Zone" + zone.ToString() + "_PE";
                        label1.Name = label1.Text + "_Lbl";
                        label1.Size = new Size(width, height);
                        label1.Location = new Point(startX + (width + xSpacing) * (zone + 5), startY - (level - 1) * (height + ySpacing));
                        label1.TabIndex = tabIndex++;
                        label1.TextAlign = ContentAlignment.MiddleCenter;
                        this.RunTabPage.Controls.Add(label1);

                        GibZone gibzone = new GibZone();
                        if (level > 1)
                        {
                            gibzone.outfeedTag = AddTag8("L" + level.ToString() + "_Z" + zone.ToString() + "_Outfeed");
                            gibzone.fStorageZone = true;
                            nStorage++;
                        }
                        gibzone.toteID = "";
                        gibzone.Label1 = label1;
                        gibzone.Label2 = null;
                        gibZones.Add(gibzone);
                    }
                }

            // New PE testing tags
            // Fixing for backwards VFX conveyor!
            conveyorSensors[1] = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Infeed_Conv:I.Data[0]", DataType.Int8, 1);
            libPlcClient.AddTag(conveyorSensors[1]);
            conveyorSensors[2] = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Lift_Conv:I.Data[0]", DataType.Int8, 1);
            libPlcClient.AddTag(conveyorSensors[2]);
            for (int i = 1; i <= 5; i++)
            {
                Tag t = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "L" + i.ToString() + "_Conv:I.Data[0]", DataType.Int8, 1);
                conveyorSensors[2 + i] = t;
                libPlcClient.AddTag(t);
            }

            ConveyorTagSetup();

            // Start timers (used only for non-critical items
            HeartbeatTmr.Interval = 1000;
            HeartbeatTmr.Enabled = true;
            MessageTmr.Interval = 100;
            MessageTmr.Enabled = true;

            // VFX
            kittingCell = new CellContents();
            vfxControl = new VfxControl(this, VfxPlcIpTxt.Text, libPlcClient, kittingCell, VfxInsightIpTxt.Text, VfxInsightPortTxt.Text);
            BuildControlArrays();
            VfxControlTmr.Interval = 250;
            VfxControlTmr.Enabled = true;

            // Start PLC EIP polling thread
            plcGetDataThread = new Thread(new ThreadStart(PlcGetDataProcess));
            plcGetDataThread.Start();

            Crawl("System ready.");

            // This will launch the TCP server
            CommandServerChk.Checked = true;
        }

        private void StopProcessing()
        {
            Crawl("StopProcessing()...");
            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;

            CommandServerChk.Checked = false;

            TerminateAnyAPITask();

            if (libPlcClient != null)
            {
                StopInfeedConveyor();
                WaitMotionComplete();
            }

            if (plcGetDataThread != null)
            {
                abortPlcPollingThread = true;
                //PlcGetDataThread.Abort();

                //while (plcPollingThreadRunning)
                //{
                //    Thread.Sleep(50);
                //}

                plcGetDataThread = null;
            }
        }

        private void Disconnect()
        {
            Crawl("Disconnect()...");
            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;

            Crawl("Closing VFX interface...");
            VfxControlTmr.Enabled = false;
            if (vfxControl != null)
            {
                vfxControl.DisconnectBarcodes();
                vfxControl = null;
            }
            if (kittingCell != null)
            {
                kittingCell = null;
            }

            Crawl("Closing PLC interface...");
            if (libPlcClient != null)
            {
                libPlcClient.Dispose();
                libPlcClient = null;
            }

            Crawl("Closing command interface...");
            if (commandServer != null)
            {
                commandServer = null;
            }

            // Tear down all the controls we created
            foreach (GibZone zone in gibZones)
            {
                if (zone.Label1 != null)
                {
                    this.RunTabPage.Controls.Remove(zone.Label1);
                    zone.Label1 = null;
                }
                if (zone.Label2 != null)
                {
                    this.RunTabPage.Controls.Remove(zone.Label2);
                    zone.Label2 = null;
                }
            }
            gibZones.Clear();

            //MessageTmr.Enabled = false;
            //HeartbeatTmr.Enabled = false;

            Crawl("Disconnect() complete");
        }

        private void CloseTmr_Tick(object sender, EventArgs e)
        {
            if (plcGetDataThread != null)
                StopProcessing();
            else
            {
                Disconnect();
                MessageTmr_Tick(null, null);
                CloseTmr.Enabled = false;
                if (CloseTmr.Interval == 1001)
                {
                    forceClose = true;
                    this.Close();
                }
            }
        }

        private void TerminateAnyAPITask()
        {
            // Any API task running... terminate
            if (currentApiTask != null)
            {
                fAbort = true;
                Thread.Sleep(1000);
                currentApiTask = null;
            }
        }

        private void BuildControlArrays()
        {
            vfxSlotTextBoxes = new TextBox[12, 2];

            //put kitting slot textboxes in an addressable array for easier coding

            //same goes for VFX slots
            vfxSlotTextBoxes[11, 1] = slot0Txt;
            vfxSlotTextBoxes[10, 1] = slot1Txt;
            vfxSlotTextBoxes[9, 1] = slot2Txt;
            vfxSlotTextBoxes[8, 1] = slot3Txt;
            vfxSlotTextBoxes[7, 1] = slot4Txt;
            vfxSlotTextBoxes[6, 1] = slot5Txt;
            vfxSlotTextBoxes[5, 1] = slot6Txt;
            vfxSlotTextBoxes[4, 1] = slot7Txt;
            vfxSlotTextBoxes[3, 1] = slot8Txt;
            vfxSlotTextBoxes[2, 1] = slot9Txt;
            vfxSlotTextBoxes[1, 1] = slot10Txt;
            vfxSlotTextBoxes[0, 1] = slot11Txt;
            vfxSlotTextBoxes[11, 0] = slot12Txt;
            vfxSlotTextBoxes[10, 0] = slot13Txt;
            vfxSlotTextBoxes[9, 0] = slot14Txt;
            vfxSlotTextBoxes[8, 0] = slot15Txt;
            vfxSlotTextBoxes[7, 0] = slot16Txt;
            vfxSlotTextBoxes[6, 0] = slot17Txt;
            vfxSlotTextBoxes[5, 0] = slot18Txt;
            vfxSlotTextBoxes[4, 0] = slot19Txt;
            vfxSlotTextBoxes[3, 0] = slot20Txt;
            vfxSlotTextBoxes[2, 0] = slot21Txt;
            vfxSlotTextBoxes[1, 0] = slot22Txt;
            vfxSlotTextBoxes[0, 0] = slot23Txt;
        }

        DateTime startPushTime = DateTime.UtcNow;
        private void HeartbeatTmr_Tick(object sender, EventArgs e)
        {
            // Update datetime fields
            DateTime now = DateTime.UtcNow;
            DatetimeLbl.Text = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            uptime = now - startDatetime;
            UptimeLbl.Text = String.Format("{0} days, {1:00}:{2:00}:{3:00}", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds);
            DateTime nowlocal = now.ToLocalTime();
            DatetimeLocalLbl.Text = nowlocal.ToString("yyyy-MM-ddTHH:mm:ss");

            // Update error counts
            ReadFailLbl.Text = plcTagReadFail.ToString();
            ReadRetryLbl.Text = plcTagReadRetry.ToString();
            WriteFailLbl.Text = plcTagWriteFail.ToString();
            WriteRetryLbl.Text = plcTagWriteRetry.ToString();
            if (commandServer != null)
            {
                GetStatusRequestCountLbl.Text = commandServer.nGetStatusRequests.ToString();
                GetStatusResponseCountLbl.Text = commandServer.nGetStatusResponses.ToString();
                BadCommLenCountLbl.Text = commandServer.nBadCommLenErrors.ToString();
            }

            // Implement automatic start.... don't try more than once every 10 seconds
            const double secondsUntilAutostart = 10;
            // Implement automatic stop and reinitialize if PLC read tag errors
            if (gibZones.Count == 0 && AutoReconnectChk.Checked) // We're torn down!
            {
                Crawl("Automatically reconnecting to everything");
                Connect();
                return;
            }
            else if (plcTagReadFail > 10)
            {
                CrawlError("Too many PLC Read Tag errors. Stopping and Reinitializing.");
                HMI_PB_StopBtn_Click(null, null);
                CloseTmr.Interval = 1000;
                CloseTmr.Enabled = true;
                return;
            }
            else
            {
                if (EStop)
                {
                    startPushTime = DateTime.UtcNow;
                }
                else if ((GIBStateLbl.Text == "STOPPED" || GIBStateLbl.Text == "READY") && AutoStartChk.Checked)
                {
                    double nSeconds = (now - startPushTime).TotalSeconds;
                    if (nSeconds > secondsUntilAutostart)
                    {
                        Crawl("AUTOSTARTING");
                        startPushTime = now;
                        HMI_PB_StartBtn_Click(null, null);
                    }
                    else Crawl("AUTOSTARTING IN " + ((int)(secondsUntilAutostart - nSeconds + 0.5)).ToString() + "S");
                }

                // Inventory monitor
                if (!EStop && currentApiTask == null && GibReady())
                {
                    if (inventory.AreThereExtras(currentBufferToteCount))
                    {
                        Crawl("Handle extra  inventory...");
                    }
                    if (inventory.AreAnyMissing(currentBufferToteCount))
                    {
                        Crawl("Handle missing inventory...");
                    }
                    if (inventory.AreThereDuplicates())
                    {
                        Crawl("Handle duplicate inventory...");
                    }
                }
            }
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ClearCrawlBtn_Click(object sender, EventArgs e)
        {
            CrawlerRTB.Clear();
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            Crawl("Disconnecting.....");
            ClearErrorCrawlBtn_Click(null, null);
            ClearInventoryCrawlBtn_Click(null, null);
            ClearVfxCrawlBtn_Click(null, null);
            ClearCommandCrawlBtn_Click(null, null);

            CloseTmr.Interval = 1000;
            CloseTmr.Enabled = true;
        }
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            Crawl("Connecting.....");
            Connect();
        }

        private void SetInfeed(bool f)
        {
            PLCWriteBool(HMI_Infeed_Mode, f);
        }

        private void InfeedRad_CheckedChanged(object sender, EventArgs e)
        {
            if (InfeedRad.Checked)
                SetInfeed(true);
        }

        private void OutfeedRad_CheckedChanged(object sender, EventArgs e)
        {
            if (OutfeedRad.Checked)
                SetInfeed(false);
        }

        public void ClearVfxCrawlBtn_Click(object sender, EventArgs e)
        {
            VfxRTB.Clear();
        }

        private void HMI_PB_StartBtn_Click(object sender, EventArgs e)
        {
            // PLC system needs to be in Single, Infeed to start
            TerminateAnyAPITask();
            ModeSingleRad.Checked = true;
            InfeedRad.Checked = true;
            PLCWriteBool(HMI_AutoMode, true);
            Thread.Sleep(250);

            HMI_PB_StopBtn_Click(null, null);
            HMI_PB_ResetBtn_Click(null, null);
            Crawl("START");
            Thread.Sleep(250);
            PLCWriteBool(HMI_PB_Start, true);
            Thread.Sleep(100);
            PLCWriteBool(HMI_PB_Start, false);
        }

        private void HMI_PB_StopBtn_Click(object sender, EventArgs e)
        {
            Crawl("STOP");
            TerminateAnyAPITask();
            PLCWriteBool(HMI_PB_Stop, true);
            Thread.Sleep(100);
            PLCWriteBool(HMI_PB_Stop, false);

            startPushTime = DateTime.UtcNow; // TODO weird logic here, but prevents auto restart immediately after you hit stop!
        }

        private void HMI_PB_ResetBtn_Click(object sender, EventArgs e)
        {
            Crawl("RESET");
            TerminateAnyAPITask();
            PLCWriteBool(HMI_AutoMode, true);
            PLCWriteBool(HMI_PB_Reset, true);
            Thread.Sleep(100);
            PLCWriteBool(HMI_PB_Reset, false);
        }
        private void SetMode(int n)
        {
            switch (n)
            {
                case 0:
                    PLCWriteBool(HMI_Outfeed_Auto, true);
                    break;
                case 1:
                    PLCWriteBool(HMI_Outfeed_Batch, true);
                    break;
                case 2:
                    PLCWriteBool(HMI_Outfeed_Single, true);
                    break;
            }
        }

        private void ModeAutoRad_CheckedChanged(object sender, EventArgs e)
        {
            SetMode(0);
        }

        private void ModeBatchRad_CheckedChanged(object sender, EventArgs e)
        {
            SetMode(1);
        }

        private void ModeSingleRad_CheckedChanged(object sender, EventArgs e)
        {
            SetMode(2);
        }

        // API functions spawn tasks to run the machine through the operation
        public Task currentApiTask = null; // System-wide task allows restricting to one at a time
        public void SendToBufferBtn_Click(object sender, EventArgs e)
        {
            if (currentApiTask != null)
            {
                CrawlError("Another API Task already running");
                return;
            }
            if (!GibReady())
            {
                CrawlError("Gib not ready");
                return;
            }
            currentApiTask = new Task(() =>
            {
                ResultCode ret = APISendToBuffer(SpecifiedToteIDTxt.Text);
                if (ret == ResultCode.Ok)
                    Invoke(new Action(() =>
                    {
                        SpecifiedToteIDTxt.Text = "";
                    }));

                currentApiTask = null;
            });
            currentApiTask.Start();
        }

        public void SendToDockBtn_Click(object sender, EventArgs e)
        {
            if (currentApiTask != null)
            {
                CrawlError("Another API Task already running");
                return;
            }
            // Gib must be calm if this is a fetch from buffer
            if (!GibReady() && DesiredDockToteTxt.Text.Length > 0)
            {
                CrawlError("Gib not ready");
                return;
            }
            currentApiTask = new Task(() => { APISendToInfeed(DesiredDockToteTxt.Text); currentApiTask = null; });
            currentApiTask.Start();
        }

        public void AcceptFromAgvBtn_Click(object sender, EventArgs e)
        {
            if (currentApiTask != null)
            {
                CrawlError("Another API Task already running");
                return;
            }
            currentApiTask = new Task(() => { APIAcceptFromAgv(); currentApiTask = null; });
            currentApiTask.Start();
        }

        public void SendToAgvBtn_Click(object sender, EventArgs e)
        {
            if (currentApiTask != null)
            {
                CrawlError("Another API Task already running");
                return;
            }
            currentApiTask = new Task(() => { APISendToAgv(); currentApiTask = null; });
            currentApiTask.Start();
        }

        // Gib ready to do a new operation?
        public bool GibReady()
        {
            return GibStateName == "RUNNING" && GibMotionComplete;
        }

        public void ScanRequestBtn_Click(object sender, EventArgs e)
        {
            if (currentApiTask != null)
            {
                CrawlError("Another API Task already running");
                return;
            }

            // Determine toteSource
            ScanToteContentsRequest.Types.ToteSource toteSource = new ScanToteContentsRequest.Types.ToteSource();
            if (DockRad.Checked)
                toteSource = ScanToteContentsRequest.Types.ToteSource.Dock;
            else if (BufferRad.Checked)
            {
                if (!GibReady())
                {
                    CrawlError("Gib not ready");
                    return;
                }
                toteSource = ScanToteContentsRequest.Types.ToteSource.Buffer;
            }
            else if (ScannerRad.Checked)
                toteSource = ScanToteContentsRequest.Types.ToteSource.Scannable;

            // Determine scanType
            ScanToteContentsRequest.Types.ScanType scanType = new ScanToteContentsRequest.Types.ScanType();
            scanType = ScanToteContentsRequest.Types.ScanType.FullScan;
            if (ScanTypeAllRad.Checked)
                scanType = ScanToteContentsRequest.Types.ScanType.FullScan;
            else if (ScanTypeIdRad.Checked)
                scanType = ScanToteContentsRequest.Types.ScanType.ToteBarcodeOnly;
            else if (ScanTypeNothingRad.Checked)
                scanType = ScanToteContentsRequest.Types.ScanType.Nothing;

            currentApiTask = new Task(() => { APIScanRequest(DesiredScanToteTxt.Text, toteSource, scanType); currentApiTask = null; });
            currentApiTask.Start();
        }

        private void AbortBtn_Click(object sender, EventArgs e)
        {
            StopInfeedConveyor();
            VFXAbortBtn_Click(null, null);
            if (currentApiTask != null)
            {
                fAbort = true;
            }
        }

        void JogOut(int inches)
        {
            if (RunConveyorInChk.Checked || RunConveyorOutChk.Checked) return;

            for (int motor = 4; motor >= 1; motor--)
                PLCWriteBool(motorFWD[motor], true);
            Thread.Sleep((inches * 60) + 500);
            StopInfeedConveyor();
        }
        void JogIn(int inches)
        {
            if (RunConveyorInChk.Checked || RunConveyorOutChk.Checked) return;

            for (int motor = 1; motor <= 4; motor++)
                PLCWriteBool(motorREV[motor], true);
            Thread.Sleep((inches * 60) + 500);
            StopInfeedConveyor();
        }

        // Test functions to just allow conveyor jogging
        private void ToteOutBtn_Click(object sender, EventArgs e)
        {
            JogOut(24);
        }

        private void ToteInBtn_Click(object sender, EventArgs e)
        {
            JogIn(24);
        }

        private void JogOutBtn_Click(object sender, EventArgs e)
        {
            JogOut(6);
        }

        private void JogInBtn_Click(object sender, EventArgs e)
        {
            JogIn(6);
        }
        private void NudgeOutBtn_Click(object sender, EventArgs e)
        {
            JogOut(0);
        }

        private void NudgeInBtn_Click(object sender, EventArgs e)
        {
            JogIn(0);
        }

        private void SetJoggable(bool f)
        {
            NudgeInBtn.Enabled = f;
            JogInBtn.Enabled = f;
            ToteInBtn.Enabled = f;
            NudgeOutBtn.Enabled = f;
            JogOutBtn.Enabled = f;
            ToteOutBtn.Enabled = f;
            if (f)
            {
                RunConveyorOutChk.Enabled = true;
                RunConveyorInChk.Enabled = true;
            }
        }

        private void RunConveyorOutChk_CheckedChanged(object sender, EventArgs e)
        {
            if (RunConveyorOutChk.Checked)
            {
                SetJoggable(false);
                RunConveyorInChk.Enabled = false;
                for (int motor = 4; motor >= 1; motor--)
                    PLCWriteBool(motorFWD[motor], true);
            }
            else
            {
                StopInfeedConveyor();
                SetJoggable(true);
            }
        }

        private void RunConveyorInChk_CheckedChanged(object sender, EventArgs e)
        {
            if (RunConveyorInChk.Checked)
            {
                SetJoggable(false);

                RunConveyorOutChk.Enabled = false;
                for (int motor = 4; motor >= 1; motor--)
                    PLCWriteBool(motorREV[motor], true);
            }
            else
            {
                StopInfeedConveyor();
                SetJoggable(true);
            }
        }

        // Turn DCSW TCP Listener on or off
        private void CommandServerChk_CheckedChanged(object sender, EventArgs e)
        {
            if (CommandServerChk.Checked)
            {
                commandServer = new TcpServer(this);
                commandServer.StartServer(CommandServerIpTxt.Text, CommandServerPortTxt.Text);
            }
            else
            {
                if (commandServer != null)
                {
                    commandServer.StopServer();
                    commandServer = null;
                }
            }

        }

        // Launch TCPClient to assist in testing
        Process proc;
        private void StartClientBtn_Click(object sender, EventArgs e)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.Arguments = "";
#if DEBUG
            start.FileName = "C:\\google3\\induct_buffer\\controls\\csharp\\interfacetester\\bin\\Debug\\GIB Interface Tester.exe";
#else
            start.FileName = "C:\\google3\\induct_buffer\\controls\\csharp\\interfacetester\\bin\\Release\\GIB Interface Tester.exe";
#endif
            Crawl("Starting " + start.FileName);
            try
            {
                proc = Process.Start(start);
            }
            catch
            {
                CrawlError("Could not start " + start.FileName);
            }
        }

        private int VFXClear()
        {
            Crawl("VFX: Clear()");
            kittingCell.EjectBox(kittingCell.boxes.Find(o => o.ConveyorID == 7));
            new CellContents.Box(kittingCell.boxTypes.Find(o => o.Name == "tote"), kittingCell.boxes, kittingCell.slots)
            {
                ConveyorID = 7,
                IsInitialized = false,
                IsLocated = false,
                Zone = CellContents.KittingZone.VFX
            };
            return 0;
        }

        int VFXFind(int nStressTestReads = 0)
        {
            Crawl("VFX: Find(" + nStressTestReads + ")");
            CellContents.Box thisBox = null;
            try
            {
                thisBox = kittingCell.boxes.Find(o => o.ConveyorID == 7);
            }
            catch (Exception ex)
            {
                CrawlError("thisBox Find Error in VFXFind ex: " + ex.ToString());
                thisBox = null;
                return 1;
            }

            if (thisBox == null)
            {
                CrawlError("NULL thisbox");
                return 2;
            }
            else
            {
                Crawl("thisBox=" + thisBox.ToString());
                if (0 == vfxControl.FindBox(thisBox))
                {
                    string barcode = vfxControl.ScanLabel(thisBox, nStressTestReads);

                    Crawl("barcode=" + barcode);
                    if (barcode.Length > 2)
                    {
                        // This is now our infeed inventory ID
                        // Should be Tnnnn
                        // It may have a -1 --> -4 on the end which we'll truncate
                        if (barcode[0] == 'T')
                        {
                            int len = barcode.Length;

                            if (barcode[len - 2] == '-')
                                inventory.infeedInventory = barcode.Substring(0, len - 2);
                            else
                                inventory.infeedInventory = barcode;
                        }
                    }
                }
            }

            return 0;
        }

        private int VFXSafe()
        {
            Crawl("VFX: Safe()");
            CellContents.Box thisBox = null;

            try
            {
                thisBox = kittingCell.boxes.Find(o => o.ConveyorID == 7);
            }
            catch (Exception ex)
            {
                CrawlError("thisBox Find Error in VFXSafe ex: " + ex.ToString());
                thisBox = null;
                return 1;
            }

            if (thisBox == null)
            {
                CrawlError("NULL thisbox");
                return 2;
            }
            else
                vfxControl.SafeZ(thisBox);
            return 0;
        }

        private int VFXCheck()
        {
            Crawl("VFX: Check()");

            int nSlots = 0;
            pauseTagPolling = true; // Keep maintenance updates off during the scan.... makes movement crisp
            foreach (CellContents.Slot slot in kittingCell.slots.Where(o => o.Box.ConveyorID == 7).ToList())
            {
                if (vfxControl.AbortCheck()) break;
                vfxControl.CheckSlot(slot);
                nSlots++;
            }
            pauseTagPolling = false;

            return 1;
        }

        private int VFXScan()
        {
            Crawl("VFX: Scan()");
            pauseTagPolling = true; // Keep maintenance updates off during the scan.... makes movement crisp
            foreach (CellContents.Slot slot in kittingCell.slots.Where(o => o.Box.ConveyorID == 7 & o.Contains == CellContents.Contents.Part).ToList())
            {
                if (vfxControl.AbortCheck()) break;
                vfxControl.ScanSlot(slot);
            }
            pauseTagPolling = false;

            return 1;
        }

        private void VFXClearBtn_Click(object sender, EventArgs e)
        {
            VFXClear();
        }

        private void VFXFindBtn_Click(object sender, EventArgs e)
        {
            int nStressReads = 0;
            try
            {
                nStressReads = int.Parse(FindStressTxt.Text);
            }
            catch { }
            currentVfxTask = new Task(() =>
            {
                VFXFind(nStressReads);
                currentVfxTask = null;
            });
            currentVfxTask.Start();
        }

        private void VFXSafeBtn_Click(object sender, EventArgs e)
        {
            currentVfxTask = new Task(() =>
            {
                VFXSafe();
                currentVfxTask = null;
            });
            currentVfxTask.Start();
        }


        private void VFXCheckBtn_Click(object sender, EventArgs e)
        {
            currentVfxTask = new Task(() =>
            {
                VFXCheck();
                currentVfxTask = null;
            });
            currentVfxTask.Start();
        }

        private void VFXScanBtn_Click(object sender, EventArgs e)
        {
            currentVfxTask = new Task(() =>
            {
                VFXScan();
                currentVfxTask = null;
            });
            currentVfxTask.Start();
        }

        private void VFXDemoBtn_Click(object sender, EventArgs e)
        {
            Crawl("VFX: Cycle Test()");

            int nStressReads = 0;
            try
            {
                nStressReads = int.Parse(FindStressTxt.Text);
            }
            catch { }

            currentVfxTask = new Task(() =>
            {
                while (!AbortCheck())
                {
                    if (!fAbort) VFXClear();
                    if (!fAbort) VFXFind(nStressReads);
                    //VFXSafe();
                    if (!fAbort) VFXCheck();
                    if (!fAbort) VFXScan();
                }
                currentVfxTask = null;
            });
            currentVfxTask.Start();
        }

        private void VFXAbortBtn_Click(object sender, EventArgs e)
        {
            Crawl("VFX: Abort");
            vfxControl.Abort();
            fAbort = true;
        }


        private void UpdateVFXControls()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            CellContents.Box vfxbox = kittingCell.boxes.Find(o => o.ConveyorID == 7);
            if (vfxbox != null)
            {
                if (vfxbox.Type.Name == "tote")
                    vfxContentsGrp.BackColor = Color.DarkGray;
                else if (vfxbox.Type.Name == "carton1")
                    vfxContentsGrp.BackColor = Color.Brown;

                ToteIdLbl.Text = vfxbox.DunnageID;
                ToteIdFailCountLbl.Text = vfxControl.toteIdFailCount.ToString();

                if (currentVfxTask == null) vfxGroup.Enabled = true;
                else if (currentVfxTask.IsCompleted) vfxGroup.Enabled = true;
                else vfxGroup.Enabled = false;

                for (int row = 0; row < vfxSlotTextBoxes.GetLength(0); row++)
                    for (int col = 0; col < vfxSlotTextBoxes.GetLength(1); col++)
                    {
                        TextBox tb = vfxSlotTextBoxes[row, col];
                        if (row < vfxbox.Type.NumRows)
                        {
                            tb.Visible = true;

                            CellContents.Slot s = kittingCell.slots.Find(o =>
                            o.Box == vfxbox &
                            o.RowNum == row &
                            o.ColNum == col
                            );
                            if (s != null)
                            {
                                CellContents.Part p = kittingCell.parts.Find(o => o.Slot == s);
                                if (s.Contains == CellContents.Contents.Empty)
                                {
                                    tb.BackColor = Color.LightGray;
                                    tb.ForeColor = Color.DarkGray;
                                    tb.Text = "Empty";
                                }
                                else if (s.Contains == CellContents.Contents.FOD)
                                {
                                    tb.BackColor = Color.DarkRed;
                                    tb.ForeColor = Color.Yellow;
                                    tb.Text = "?? FOD ??";
                                }
                                else if (s.Contains == CellContents.Contents.Part)
                                {
                                    tb.BackColor = Color.Black;
                                    tb.ForeColor = Color.White;
                                    if (p != null)
                                    {
                                        if (Math.Abs(p.Orientation) > 180)
                                            tb.BackColor = Color.DarkOrange;
                                        else
                                            tb.BackColor = Color.DarkBlue;
                                        tb.Text = p.Barcode + " " + p.Orientation.ToString("000.0");
                                        if (p.Barcode2 != null)
                                            if (p.Barcode2 != "")
                                                tb.Text += " " + p.Barcode2;
                                    }
                                    else
                                    {
                                        tb.Text = "Disk Present";
                                    }
                                }
                                else
                                {
                                    tb.BackColor = Color.DarkGray;
                                    tb.ForeColor = Color.DarkGray;
                                    tb.Text = "???";
                                }
                                if (LaserCheckbox.Checked) tb.Text = tb.Text + " (L:" + s.LaserDistance + ")";
                            }
                        }
                        else tb.Visible = false;
                    }
            }

            //if (kittingTask.Status.ToString() == "0") kittingGroup.Enabled = false;
            //else kittingGroup.Enabled = true;

            watch.Stop();
            //Crawl("UpdateVFXControls execution time: " + watch.ElapsedMilliseconds.ToString() + "mS");
        }

        private void VfxControlTmr_Tick(object sender, EventArgs e)
        {
            UpdateVFXControls();
        }

        private void saveAllBtn_Click(object sender, EventArgs e)
        {
            RegistryKey SoftwareKey = Registry.CurrentUser.OpenSubKey("Software", true);
            RegistryKey AppNameKey = SoftwareKey.CreateSubKey("GibController");

            AppNameKey.SetValue("GibId", GibIdTxt.Text);
            AppNameKey.SetValue("GibCapacity", GibCapacityTxt.Text);
            AppNameKey.SetValue("GibPlcIp", GibPlcIpTxt.Text);
            AppNameKey.SetValue("VfxPlcIp", VfxPlcIpTxt.Text);
            AppNameKey.SetValue("VfxInsightIp", VfxInsightIpTxt.Text);
            AppNameKey.SetValue("VfxInsightPort", VfxInsightPortTxt.Text);
            AppNameKey.SetValue("CommandServerIp", CommandServerIpTxt.Text);
            AppNameKey.SetValue("CommandServerPort", CommandServerPortTxt.Text);
            AppNameKey.SetValue("InventoryFolder", InventoryFolderTxt.Text);
            AppNameKey.SetValue("Logfile", LogfileTxt.Text);
        }


        private void SaveInventoryBtn_Click(object sender, EventArgs e)
        {
            try
            {
                inventory.infeedInventory = InfeedInventoryTxt.Text.Trim();
                List<string> newBufferInventory = new List<string>();
                foreach (string s in BufferInventoryTxt.Lines)
                    if (s.Length > 0) newBufferInventory.Add(s.Trim());
                inventory.bufferInventory = newBufferInventory;
                Crawl("Inventory files saved");
                LoadInventoryBtn_Click(null, null);
            }
            catch
            {
                CrawlError("Could not save Inventory files");
            }
        }

        private void LoadInventoryBtn_Click(object sender, EventArgs e)
        {
            try
            {
                inventory.Load();
                InfeedInventoryTxt.Text = inventory.infeedInventory;
                BufferInventoryTxt.Text = String.Join(Environment.NewLine, inventory.bufferInventory);
                Crawl("Inventory files loaded");
            }
            catch
            {
                CrawlError("Could not load inventory files");
            }
        }

        private void ClearCommandCrawlBtn_Click(object sender, EventArgs e)
        {
            CommRTB.Clear();
        }

        private void ClearErrorCrawlBtn_Click(object sender, EventArgs e)
        {
            ErrorsRTB.Clear();
        }

        private void DryRunChk_CheckedChanged(object sender, EventArgs e)
        {
            commandServer.DryRun = DryRunChk.Checked;
        }

        private void MachineTab_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage.Name == "MaintenanceTabPage") LoadInventoryBtn_Click(null, null);
        }

        private void GetFromBufferBtn_Click(object sender, EventArgs e)
        {
            if (currentApiTask != null)
            {
                CrawlError("Another API Task already running");
                return;
            }
            if (!GibReady())
            {
                CrawlError("Gib not ready");
                return;
            }
            currentApiTask = new Task(() => { APISendToInfeed(GetFromBufferIdTxt.Text, false); currentApiTask = null; });
            currentApiTask.Start();
        }

        private void ClearInventoryCrawlBtn_Click(object sender, EventArgs e)
        {
            InventoryRTB.Clear();
        }

        private void SilenceAlarmsBtn_Click(object sender, EventArgs e)
        {
            PLCWriteBool(HMI_Alarm_Silence, true);
            Thread.Sleep(100);
            PLCWriteBool(HMI_Alarm_Silence, false);
        }

        private void inventoryStatusLbl_DoubleClick(object sender, EventArgs e)
        {
            ManualConveyorGrp.Enabled = !ManualConveyorGrp.Enabled;
        }

        private void AutoStartChk_CheckedChanged(object sender, EventArgs e)
        {
            startPushTime = DateTime.UtcNow;
        }

        private void PlcErrorClearBtn_Click(object sender, EventArgs e)
        {
            plcTagReadFail = 0;
            plcTagReadRetry = 0;
            plcTagWriteFail = 0;
            plcTagWriteRetry = 0;
        }

        private void ClearGetStatusRequestCountBtn_Click(object sender, EventArgs e)
        {
            if (commandServer != null)
            {
                commandServer.nGetStatusRequests = 0;
                commandServer.nGetStatusResponses = 0;
                commandServer.nBadCommLenErrors = 0;
            }
        }

        private void RunTabPage_Click(object sender, EventArgs e)
        {

        }
    }
}
