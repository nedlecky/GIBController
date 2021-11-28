using LibplctagWrapper;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GibController
{
    public partial class VfxControl : Object
    {
        public CellContents kittingCell;
        public MainForm myForm;
        private bool cmdEnabled;
        private bool prgComplete;
        private bool prgRun;
        private int laserData;
        private int targetSlotCol;
        private int targetSlotRow;
        private int targetBoxNum;
        private int targetBoxType;
        private int targetFrameX;
        private int targetFrameY;
        private int targetFrameR;
        private FanucProgram targetPrg;
        private FanucProgram prgTarget;
        private int prgStart;
        private StyleResult prgResult;
        public int targetFiducial;

        public int toteIdFailCount = 0;

        bool fAbort = false;
        bool fLastAbort = false;

        Libplctag plcclient;
        private List<Tag> myTags = new List<Tag>();
        public Tag TagInputDINT;
        public Tag TagOutputDINT;


        private enum FanucProgram
        {
            StoreFrame = 1,
            UpdateFrame = 2,
            ScanSlot = 3,
            FindFiducial = 4,
            CheckSlot = 5,
            ScanLabel = 6,
            SafeZ = 7,
        }

        enum StyleResult
        {
            Failed = -1,
            Incomplete = 0,
            Success = 1
        }

        public VfxControl(MainForm form, string Ip, Libplctag client, CellContents k, string CognexIp, string CognexPort)
        {
            myForm = form;
            Crawl("VFX: VFXControl(...)");
            plcclient = client;
            InitPlc(Ip, 32);
            kittingCell = k;

            int barcodeConnectReturn = ConnectBarcodes(CognexIp, CognexPort);
            for (int i = 0; i < 20 && barcodeConnectReturn != 0; i++)
            {
                Crawl("VFXControl: Waiting a second to retry Cognex connection...");
                Thread.Sleep(1000);
                barcodeConnectReturn = ConnectBarcodes(CognexIp, CognexPort);
            }

        }

        public void InitPlc(string IP, int start)
        {
            myForm.Crawl("InitPLC() IP=" + IP);

            myTags.Add(TagInputDINT = new Tag(IP, "1, 0", CpuType.LGX, "IPC:O.Data[" + start + "]", DataType.Int32, 10));
            myTags.Add(TagOutputDINT = new Tag(IP, "1, 0", CpuType.LGX, "IPC:I.Data[" + start + "]", DataType.Int32, 10));

            // add the tags
            foreach (Tag thisTag in myTags)
            {
                // add the Output tag
                plcclient.AddTag(thisTag);
                // check that the tag has been added, if it returns pending we have to retry
                while (plcclient.GetStatus(thisTag) == Libplctag.PLCTAG_STATUS_PENDING)
                {
                    Thread.Sleep(100);
                }
                // if the status is not ok, we have to handle the error
                if (plcclient.GetStatus(thisTag) != Libplctag.PLCTAG_STATUS_OK)
                {
                    myForm.CrawlError("Could not set up tag internal state. Error: " + plcclient.DecodeError(plcclient.GetStatus(TagInputDINT)));
                    return;
                }

            }
        }

        public void Abort()
        {
            fAbort = true;
        }

        public bool AbortCheck()
        {
            fLastAbort = fAbort;
            if (fAbort)
            {
                Crawl("VFX: Command Aborted");
                fAbort = false;
                return true;
            }
            return false;
        }

        private void StartRobotProgram()
        // This is generic and should work as long as IO is mapped in a similar way
        {
            //Crawl("VFX: StartRobotProgram ()");

            bool originalPauseTagPolling = MainForm.pauseTagPolling;

            if (!MainForm.pauseTagPolling) // If main form tag polling isn't already sleeping, pause it!
            {
                // Wait for the PLC tag polling thread to sleep, then pause it
                while (!MainForm.tagPollingSleeping)
                    Thread.Sleep(10);

                MainForm.pauseTagPolling = true;
            }

            ReadDataNow(); // Start with fresh data!
            //Crawl("Realtime: SRP1");
            while (prgTarget != targetPrg)
            {
                Thread.Sleep(20);
                WriteDataNow();
                ReadDataNow();
            }

            //Crawl("Realtime: SRP2");
            while (!prgRun)
            {
                prgStart = 1;
                WriteDataNow();
                Thread.Sleep(20);
                prgStart = 0;
                WriteDataNow();
                ReadDataNow();
            }

            //Crawl("Realtime: SRP3");
            while (!prgComplete)
            {
                Thread.Sleep(20); // 20 is OK if controller already running
                ReadDataNow();
                WriteDataNow();
            }

            //Crawl("Realtime: SRP4");
            while (prgResult != StyleResult.Success)
            {
                Thread.Sleep(20);
                ReadDataNow();
            }
            //Crawl("Realtime: SRP5");
            MainForm.pauseTagPolling = originalPauseTagPolling;
        }

        private void Crawl(string msg)
        {
            myForm.Crawl(msg);
        }
        private void CrawlError(string msg)
        {
            Crawl("ERROR: " + msg);
        }

        public int FindBox(CellContents.Box target)
        {

            Crawl("VFX: Finding Fiducial 1 " + target.ConveyorID + "...");
            targetPrg = FanucProgram.FindFiducial;
            //skipPerch = 0;
            targetBoxNum = target.ConveyorID;
            targetBoxType = target.Type.ID;
            targetFiducial = 1;

            StartRobotProgram();

            string insightvfxResult;
            int nRetry = 0;
            int maxRetries = 3;

            CameraResult cameraResult = new CameraResult { resultsValid = false };
            while (!AbortCheck() && !cameraResult.resultsValid && nRetry < maxRetries)
            {
                insightvfxResult = insight_vfx.Trigger("fid1\r\n");
                cameraResult = ParseFiducialResult(insightvfxResult);
                nRetry++;
            }


            if (cameraResult.resultsValid)
            {
                Crawl("VFX: Updating Frame...");
                targetPrg = FanucProgram.UpdateFrame;
                targetBoxNum = target.ConveyorID;
                targetBoxType = target.Type.ID;
                targetFiducial = 0;
                targetFrameX = cameraResult.X;
                targetFrameY = cameraResult.Y;
                targetFrameR = cameraResult.R;

                double dX = targetFrameX / 1000.0;
                double dY = targetFrameY / 1000.0;

                StartRobotProgram();

                target.IsLocated = true;
                Crawl("VFX: Found Box " + target.ConveyorID + " at X=" + dX.ToString("0.00") + " Y=" + dY.ToString("0.00"));
                return 0;
            }
            else
            {
                CrawlError("VFX: Failed to locate box");
                return 1;
            }
        }

        public void CheckSlot(CellContents.Slot target)
        {

            Crawl("VFX: Checking Box " + target.Box.ConveyorID + " Slot (" + target.RowNum + ", " + target.ColNum + ")...");

            ReadDataNow();
            //if (!target.Box.IsLocated) FindBox(target.Box);

            targetPrg = FanucProgram.CheckSlot;
            //skipPerch = 1;
            targetBoxNum = target.Box.ConveyorID;
            targetBoxType = target.Box.Type.ID;
            targetSlotRow = target.RowNum;
            targetSlotCol = target.ColNum;

            StartRobotProgram();

            Thread.Sleep(75); //let the robot settle before taking a measurement
            ReadDataNow(); //get the freshest laser data
            target.IsChecked = true;
            target.Contains = GetContents(target, laserData);
            target.LaserDistance = laserData / 100;
        }

        public bool SafeZ(CellContents.Box target)
        {
            ReadDataNow();
            targetPrg = FanucProgram.SafeZ;
            StartRobotProgram();
            return true;
        }

        public string ScanLabel(CellContents.Box target, int nStressTestReads = 0)
        {
            Crawl("VFX: Scanning Label on Box #" + target.ConveyorID);

            //if (!target.Box.IsLocated) FindBox(target.Box);

            targetPrg = FanucProgram.ScanLabel;
            targetFiducial = 0;
            targetBoxNum = target.ConveyorID;
            targetBoxType = target.Type.ID;
            targetSlotRow = 0;
            targetSlotCol = 0;


            StartRobotProgram();

            int nRetry = 0;
            int maxRetries = 3;
            string insightvfxResult = "";
            CameraResult cameraResult = new CameraResult { Serial = null };

            while (!AbortCheck() && cameraResult.Serial == null && nRetry < maxRetries)
            {
                for (int i = 0; i < 1 + nStressTestReads; i++)
                    insightvfxResult = insight_vfx.Trigger("bc2\r\n");
                cameraResult = ParseToteBarcodeResult(insightvfxResult);
                nRetry++;

            }

            target.DunnageID = cameraResult.Serial;


            if (cameraResult.Serial == null)
            {
                CrawlError("VFX: Failed to read ToteID");
                toteIdFailCount++;
                return "";
            }
            else
            {
                // This is now our infeed inventory ID
                // Should be Tnnnn
                // It may have a -1 --> -4 on the end which we'll truncate
                if (cameraResult.Serial[0] == 'T')
                {
                    int len = cameraResult.Serial.Length;

                    if (cameraResult.Serial[len - 2] == '-')
                        cameraResult.Serial = cameraResult.Serial.Substring(0, len - 2);

                    target.DunnageID = cameraResult.Serial;
                }

                Crawl("VFX: Read ToteID " + cameraResult.Serial);

                return cameraResult.Serial;
            }


        }

        public void ScanSlot(CellContents.Slot target)
        {
            Crawl("VFX: Scanning Box " + target.Box.ConveyorID + " Slot (" + target.RowNum + ", " + target.ColNum + ")...");

            //if (!target.Box.IsLocated) FindBox(target.Box);

            targetPrg = FanucProgram.ScanSlot;
            targetFiducial = 0;
            targetBoxNum = target.Box.ConveyorID;
            targetBoxType = target.Box.Type.ID;
            targetSlotRow = target.RowNum;
            targetSlotCol = target.ColNum;

            StartRobotProgram();

            string insightvfxResult;
            int nRetry = 0;
            int maxRetries = 3;

            CameraResult cameraResult = new CameraResult { Serial = null };
            while (cameraResult.Serial == null && nRetry < maxRetries)
            {
                insightvfxResult = insight_vfx.Trigger("bc1\r\n");
                cameraResult = ParseDiskBarcodeResult(insightvfxResult);
                nRetry++;

            }

            if (cameraResult.Serial == null)
                Crawl("Disk barcode not found");
            else
            {
                CellContents.Part thisComponent = new CellContents.Part
                {
                    Barcode = cameraResult.Serial,
                    Orientation = cameraResult.Rotation,
                    Barcode2 = cameraResult.Info,
                    ProductType = target.Box.Type.ProductType,
                    Slot = target

                };
                kittingCell.parts.Add(thisComponent);
            }
        }

        public CellContents.Contents GetContents(CellContents.Slot slot, int laserData)
        {
            if (laserData > slot.Box.Type.EmptyDistance)
            {
                return CellContents.Contents.Empty;
            }
            else if (laserData < slot.Box.Type.ProductDistance)
            {
                return CellContents.Contents.Part;
            }
            else
            {
                return CellContents.Contents.FOD;
            }
        }

        public void WriteDataNow()
        {
            int[] OutputArray = new int[10];
            OutputArray[0] = prgStart;
            OutputArray[1] = (int)targetPrg;
            OutputArray[2] = targetBoxType;
            OutputArray[3] = targetBoxNum;
            OutputArray[4] = targetSlotRow;
            OutputArray[5] = targetSlotCol;
            OutputArray[6] = targetFrameX;
            OutputArray[7] = targetFrameY;
            OutputArray[8] = targetFrameR;
            OutputArray[9] = targetFiducial;

            myForm.WriteOutputDINT(TagOutputDINT, OutputArray);
        }

        public void ReadDataNow()
        {
            //Crawl("Realtime: RDN1");
            int[] InputArray = myForm.ReadInputDINT(TagInputDINT, 10);
            if (InputArray == null)
            {
                CrawlError("Realtime: ReadDataNow Readinput failure ");
                return;
            }
            //Crawl("Realtime: RDN2 " + InputArray.Length);
            bool[] InputBits = myForm.GetBitArrayfromInt(InputArray[0]);
            cmdEnabled = InputBits[0];
            prgComplete = InputBits[1];
            prgRun = InputBits[2];
            prgTarget = (FanucProgram)InputArray[1];
            laserData = InputArray[8];
            prgResult = (StyleResult)InputArray[9];
            //Crawl("Realtime: RDN3");
        }
    }
}
