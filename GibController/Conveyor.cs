// Infeed conveyor control code
using LibplctagWrapper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace GibController
{
    public partial class MainForm : Form
    {
        static int precisionPollRate = 50;
        static int inchingSpeed = 20;

        Tag[] zoneOccupied = new Tag[5];
        Tag[] liftOccupied = new Tag[3];
        Tag[] motorFWD = new Tag[5];
        Tag[] motorREV = new Tag[5];
        GibLevel[] GIBLevels = new GibLevel[6];
        Tag Lift_Current_Level;
        Tag HMI_Conv_FWD_Speed;
        Tag HMI_Conv_REV_Speed;
        bool fAbort = false;
        bool fLastAbort = false;

        // Turn off all four conveyor motors, both directions, and set default speed back to 100%
        void StopInfeedConveyor()
        {
            for (int motor = 1; motor <= 4; motor++)
            {
                PLCWriteBool(motorFWD[motor], false);
                PLCWriteBool(motorREV[motor], false);
            }
            PLCWriteInt(HMI_Conv_FWD_Speed, 100);
            PLCWriteInt(HMI_Conv_REV_Speed, 100);
        }

        // Has the fAbort bool raised?
        bool AbortCheck()
        {
            fLastAbort = fAbort;
            if (fAbort)
            {
                Crawl("Command Aborted");
                fAbort = false;
                return true;
            }
            return false;
        }

        // Build PLC tags for monitoring/controlling the conveyor through EIP to the PLC
        int ConveyorTagSetup()
        {
            for (int zone = 1; zone <= 4; zone++)
            {
                zoneOccupied[zone] = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Infeed_Zone" + zone.ToString() + ".Occupied", DataType.Int8, 1);
                libPlcClient.AddTag(zoneOccupied[zone]);

                motorFWD[zone] = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Infeed_Zone" + zone.ToString() + "_FWD", DataType.Int8, 1);
                libPlcClient.AddTag(motorFWD[zone]);

                motorREV[zone] = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Infeed_Zone" + zone.ToString() + "_REV", DataType.Int8, 1);
                libPlcClient.AddTag(motorREV[zone]);
            }

            for (int i = 1; i <= 2; i++)
            {
                liftOccupied[i] = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Lift_Conv_PE" + i.ToString(), DataType.Int8, 1);
                //plcclient.AddTag(liftOccupied[i]);
            }

            for (int level = 1; level <= 5; level++)
            {
                GIBLevels[level] = new GibLevel();
                GIBLevels[level].Empty = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Level_" + level.ToString() + "_Empty", DataType.Int8, 1);
                libPlcClient.AddTag(GIBLevels[level].Empty);

                GIBLevels[level].Full = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Level_" + level.ToString() + "_Full", DataType.Int8, 1);
                libPlcClient.AddTag(GIBLevels[level].Full);
            }

            Lift_Current_Level = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "Lift_Current_Level", DataType.Int32, 1);
            libPlcClient.AddTag(Lift_Current_Level);

            HMI_Conv_FWD_Speed = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Conv_FWD_Speed", DataType.Int32, 1);
            libPlcClient.AddTag(HMI_Conv_FWD_Speed);

            HMI_Conv_REV_Speed = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, "HMI_Conv_REV_Speed", DataType.Int32, 1);
            libPlcClient.AddTag(HMI_Conv_REV_Speed);
            StopInfeedConveyor();

            return 0;
        }

        // Read all the infeed zones and return total number of totes seen. Leaves results in occupiedSnapshot[1-4]
        bool[] occupiedSnapshot = new bool[5];
        public int SnapshotOccupied()
        {
            int n = 0;
            sbyte pe = PLCReadInt8(conveyorSensors[1]);
            for (int i = 1; i <= 4; i++)
            {
                bool f = (pe & (1 << (i - 1))) == 0;
                occupiedSnapshot[i] = f;
                if (f) n++;
            }
            return n;
        }

        // Check infeed sensor 1-4... True iff tote present
        bool InfeedSensor(int i)
        {
            bool present = ((PLCReadInt8(conveyorSensors[1]) & (1 << (i - 1))) == 0);

            return present;
        }

        // Check lift sensor 1-2... True iff tote present
        bool LiftSensor(int i)
        {
            bool present = ((PLCReadInt8(conveyorSensors[2]) & (1 << (i - 1))) == 0);

            return present;
        }

        // Physical sequences for each API call... these are intended to be run in threads so that they do not lock anything up
        public ResultCode APIAcceptFromAgv()
        {
            // Validates the preconditions for receiving a tote from an AGV:
            // 1.  Nothing can be on the conveyor.

            ResultCode ret = ResultCode.UnknownResultCode;

            Crawl("APIAcceptFromAgv begins");
            fAbort = false;
            pauseTagPolling = true;

            int n = SnapshotOccupied();
            if (n > 0)
            {
                CrawlError("APIAcceptFromAgv: Tote already on conveyor");
                ret = ResultCode.ToteAlreadyPresent;
            }
            else
            {
                ret = ResultCode.CommandInterrupted;

                // Pull in until tote breaks zone4 PE
                PLCWriteBool(motorREV[4], true);
                while (!InfeedSensor(4) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                if (!fLastAbort)
                {
                    // Now wait to see it exit the Zone4 entry PE
                    while (InfeedSensor(4) && !AbortCheck())
                    {
                        Thread.Sleep(precisionPollRate);
                    }
                    PLCWriteBool(motorREV[4], false);
                    if (!fLastAbort)
                    {

                        // Now run Zone4 forward slowly to get back into the PE!
                        PLCWriteInt(HMI_Conv_FWD_Speed, inchingSpeed * 2);
                        PLCWriteBool(motorFWD[4], true);
                        while (!InfeedSensor(4) && !AbortCheck())
                        {
                            Thread.Sleep(precisionPollRate);
                        }
                        if (!fLastAbort) ret = ResultCode.Ok;
                        ret = ResultCode.Ok;
                    }
                }
                StopInfeedConveyor();
            }

            if (ret == ResultCode.Ok)
            {
                inventory.infeedInventory = "T????";
            }
            Crawl("APIAcceptFromAgv ends ret=" + ret.ToString());
            pauseTagPolling = false;

            return ret;
        }

        public ResultCode APISendToAgv()
        {
            // Validate the preconditions for shipping a tote off the end of a conveyor
            // in the hopes an AGV will be there to catch it.
            // 1.  There must be a tote at the dock and nothing in the secure area of
            // the conveyor.

            ResultCode ret = ResultCode.UnknownResultCode;


            Crawl("APISendToAgv begins");
            fAbort = false;
            pauseTagPolling = true;

            int n = SnapshotOccupied();

            if (!occupiedSnapshot[4])
            {
                CrawlError("APISendToAgv: No tote on dock to send to AGV");
                ret = ResultCode.NotPresent;
            }
            else if (n > 1)
            {
                CrawlError("APISendToAgv: Other totes in secure area");
                ret = ResultCode.MultipleTotes;
            }
            else
            {
                ret = ResultCode.CommandInterrupted;
                PLCWriteBool(motorFWD[4], true);

                while (InfeedSensor(4) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                if (!fLastAbort)
                {
                    // Delay to ensure loaded to AGV... break up to allow for Abort checking
                    // TODO shold be a standard function !
                    int totalDelay = 0;
                    int stepDelay = 100;
                    while (!AbortCheck() && totalDelay < 1000)
                    {
                        Thread.Sleep(stepDelay);
                        totalDelay += stepDelay;
                    }
                    if (!fLastAbort) ret = ResultCode.Ok;
                }

                StopInfeedConveyor();
            }

            if (ret == ResultCode.Ok)
            {
                inventory.infeedInventory = "";
            }

            Crawl("APISendToAgv ends ret=" + ret.ToString());
            pauseTagPolling = false;
            return ret;
        }

        public bool WaitMotionComplete()
        {
            while (!PLCReadBool(Motion_Complete) && !AbortCheck())
            {
                Thread.Sleep(250);
            }

            return !fLastAbort;
        }

        // Go get toteId from the buffer
        // toteId must match the stored inventory list, unless it is in the form pnnn where n is a position number to retreive from!
        ResultCode RetrieveToteFromBuffer(string toteId)
        {
            ResultCode ret = ResultCode.CommandInterrupted;
            fAbort = false;

            // Find gibZone
            GibZone gz = null;
            if (toteId == null)
            {
                CrawlError("RetrieveTote(null)");
                return ResultCode.InvalidRequest;
            }
            if (toteId.Length < 1)
            {
                CrawlError("RetrieveTote(\"\")");
                return ResultCode.InvalidRequest;
            }

            // Get shuffling done!!
            if (WaitMotionComplete())
            {
                WaitInventoryUpdate();
                if (toteId[0] != 'p')
                    // Find a tote with toteId
                    gz = gibZones.Find(x => x.toteID == toteId);
                else
                {
                    // See if there is a tote at position nnn (string is pnnn)
                    int pos = 0;
                    try
                    {
                        pos = Int32.Parse(toteId.Remove(0, 1));
                        if (pos + 10 < gibZones.Count) gz = gibZones[pos + 10];
                        if (gz != null)
                            if (!gz.occupied)
                            {
                                CrawlError("Buffer position " + pos.ToString() + " not occupied");
                                gz = null;
                            }
                            else toteId = gz.toteID; // Copy the tote name from the buffer storage
                    }
                    catch
                    {

                    }
                }
            }

            if (gz != null && gz.outfeedTag != null)
            {
                Crawl("RetrieveTote: Need to retrieve " + gz.Label1.Name);
                inventory.infeedInventory = "";

                SetInfeed(false);
                Thread.Sleep(100);
                PLCWriteBool(motorFWD[1], true);
                PLCWriteBool(motorFWD[2], true);
                PLCWriteBool(gz.outfeedTag, true);
                while (!InfeedSensor(1) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                PLCWriteBool(gz.outfeedTag, false); // If you don't, the lift will go back up hoping for another from the same zone
                while (!InfeedSensor(2) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                PLCWriteBool(motorFWD[1], false);
                PLCWriteBool(motorFWD[2], false);

                // Adjust inventory
                try
                {
                    Crawl("Removing tote " + toteId + " from inventory");
                    List<string> bufferInventory = inventory.bufferInventory;
                    bufferInventory.Remove(toteId);
                    inventory.bufferInventory = bufferInventory;
                    inventory.infeedInventory = toteId;
                }
                catch
                {
                    CrawlError("Failed to remove inventory toteId=" + toteId);
                }

                // ORIGINAL: Now the weird stuff
                SetInfeed(true);
                Thread.Sleep(500);
                SetInfeed(false);
                Thread.Sleep(500);
                PLCWriteBool(motorFWD[1], true);
                PLCWriteBool(motorFWD[1], false);

                // NEW: Now the weird stuff
                //Crawl("Initiating end of fetch sequence");
                //SetInfeed(true);
                //Thread.Sleep(200);
                //WaitMotionComplete();
                //SetInfeed(false);
                //Thread.Sleep(200);
                //WaitMotionComplete();
                //PLCWriteBool(motorFWD[1], true);
                //PLCWriteBool(motorFWD[1], false);
                //Crawl("End of fetch sequence");

                if (fLastAbort)
                    ret = ResultCode.HardwareFault;
                else
                    ret = ResultCode.Ok;
            }
            else
            {
                CrawlError("RetrieveTote: Cannot find toteID [" + toteId + "] in buffer.");
                ret = ResultCode.NotPresent;
            }

            return ret;
        }

        public ResultCode APIScanRequest(string toteID, ScanToteContentsRequest.Types.ToteSource toteSource, ScanToteContentsRequest.Types.ScanType scanType)
        {
            // Validates requests for scanning a tote's contents.
            // 1. For totes on the conveyor, verify they are the only tote on the conveyor.
            // 2. For totes from the buffer, verify the conveyor is empty and the specified
            // tote ID is in the buffer.
            // 3. For totes aready in the secure area, verify they are the only tote on the
            // conveyor

            ResultCode ret = ResultCode.UnknownResultCode;

            Crawl("APIScanRequest begins");
            fAbort = false;
            pauseTagPolling = true;

            int n = SnapshotOccupied();
            switch (toteSource)
            {
                case ScanToteContentsRequest.Types.ToteSource.Unknown:
                    CrawlError("APIScanRequest: No idea how to scan tote from TOTE_SOURCE_UNKNOWN");
                    ret = ResultCode.InvalidRequest;
                    break;
                case ScanToteContentsRequest.Types.ToteSource.Dock:
                    if (n > 1)
                    {
                        CrawlError("APIScanRequest: Multiple totes on conveyor");
                        ret = ResultCode.MultipleTotes;
                    }
                    else if (!occupiedSnapshot[4])
                    {
                        CrawlError("APIScanRequest: No tote on dock");
                        ret = ResultCode.NotPresent;
                    }
                    else ret = ResultCode.Ok;
                    break;
                case ScanToteContentsRequest.Types.ToteSource.Scannable:
                    if (n > 1)
                    {
                        CrawlError("APIScanRequest: Multiple totes on conveyor");
                        ret = ResultCode.MultipleTotes;
                    }
                    else if (!occupiedSnapshot[1] && !occupiedSnapshot[2]) // Accept at eithe PE 1 or 2!
                    {
                        CrawlError("APIScanRequest: No tote in scanner");
                        ret = ResultCode.NotPresent;
                    }
                    else ret = ResultCode.Ok;
                    break;
                case ScanToteContentsRequest.Types.ToteSource.Buffer:
                    if (n != 0)
                    {
                        CrawlError("APIScanRequest: Tote(s) already on conveyor");
                        ret = ResultCode.ToteAlreadyPresent;
                    }
                    else
                    {
                        ret = RetrieveToteFromBuffer(toteID);
                    }
                    break;
            }

            // If all OK so far, we scan
            if (ret == ResultCode.Ok)
            {
                Crawl("MoveToteToScanPosition"); Thread.Sleep(1000);
                MoveToteToScanPosition();

                switch (scanType)
                {
                    case ScanToteContentsRequest.Types.ScanType.FullScan:
                        VFXClear();
                        VFXFind();
                        VFXCheck();
                        VFXScan();
                        break;

                    case ScanToteContentsRequest.Types.ScanType.Nothing:
                        VFXClear();
                        break;

                    case ScanToteContentsRequest.Types.ScanType.ToteBarcodeOnly:
                        VFXClear();
                        VFXFind();
                        break;

                    case ScanToteContentsRequest.Types.ScanType.Unknown:
                        CrawlError("Unknown ScanType specified");
                        ret = ResultCode.InvalidRequest;
                        break;
                }
            }

            Crawl("APIScanRequest ends ret=" + ret.ToString());
            pauseTagPolling = false;
            return ret;
        }

        int MoveToteToScanPosition()
        {
            int n = SnapshotOccupied();
            if (n != 1) return 1;
            fAbort = false;

            // If already looks like in position, will back up and re inch up anyway
            if (occupiedSnapshot[1])
            {
                // Need to push tote out
                PLCWriteBool(motorFWD[2], true);
                PLCWriteBool(motorFWD[1], true);
                while (InfeedSensor(1) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                PLCWriteBool(motorFWD[1], false);
                PLCWriteBool(motorFWD[2], false);
            }
            else //if(!occupiedSnapshot[2]) // Not in final position... speed forward until 2 breaks and reopens
            {
                PLCWriteBool(motorREV[2], true);
                PLCWriteBool(motorREV[3], true);
                PLCWriteBool(motorREV[4], true);
                while (!InfeedSensor(2) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                Thread.Sleep(100);
                while (InfeedSensor(2) && !AbortCheck())
                {
                    Thread.Sleep(precisionPollRate);
                }
                PLCWriteBool(motorREV[4], false);
                PLCWriteBool(motorREV[3], false);
                PLCWriteBool(motorREV[2], false);
            }

            // Now we're close... inch up to final spot
            PLCWriteInt(HMI_Conv_REV_Speed, inchingSpeed);
            PLCWriteBool(motorREV[2], true);
            while (!InfeedSensor(1) && !AbortCheck())
            {
                Thread.Sleep(precisionPollRate);
            }
            PLCWriteBool(motorREV[2], false);
            StopInfeedConveyor();

            return 0;
        }

        public ResultCode APISendToInfeed(string toteId, bool sendToDock = true)
        {
            // Validates the preconditions for sending a tote to the dock:
            // 1.  There must be a tote already at the secure area, or a tote ID for a tote
            // stored in the buffer (if there is a tote ID specified then their cant be
            // anything on the conveyor).
            // 2.  There cannot already be a tote at the dock.

            ResultCode ret = ResultCode.UnknownResultCode;

            Crawl("APISendToInfeed(\"" + toteId + "\"," + sendToDock.ToString() + ")");
            fAbort = false;
            pauseTagPolling = true;

            int nInfeedTotes = SnapshotOccupied();

            if (occupiedSnapshot[4])
            {
                CrawlError("APISendToInfeed: Another tote already on dock");
                ret = ResultCode.ToteAlreadyPresent;
            }
            else if (nInfeedTotes > 1)
            {
                CrawlError("APISendToInfeed: Multiple totes on conveyor");
                ret = ResultCode.MultipleTotes;
            }
            else if (nInfeedTotes == 0)
            {
                if (toteId == null || toteId == "")
                {
                    CrawlError("APISendToInfeed: No tote on conveyor and no toteId specified for retrieval");
                    ret = ResultCode.NotPresent;
                }
                else
                {
                    Crawl("APISendToInfeed: Need to retrieve tote " + toteId);
                    ret = RetrieveToteFromBuffer(toteId);
                    nInfeedTotes = SnapshotOccupied(); // Recompute 
                }
            }
            else if (nInfeedTotes == 1) /* inevitable */ ret = ResultCode.Ok;

            // Now we should have 1 tote on the infeed conveyor... was already there or we just fetched it
            if (ret == ResultCode.Ok && sendToDock)
            {
                if (nInfeedTotes < 1)
                {
                    CrawlError("APISendToDock: No tote found to move");
                    ret = ResultCode.NotPresent;
                }
                else if (nInfeedTotes > 1)
                {
                    CrawlError("APISendToDock: Too many totes on infeed");
                    ret = ResultCode.MultipleTotes;
                }
                else
                {
                    ret = ResultCode.CommandInterrupted;

                    // Speed until we see 3 break and reappear
                    PLCWriteBool(motorFWD[4], true);
                    PLCWriteBool(motorFWD[3], true);
                    PLCWriteBool(motorFWD[2], true);
                    PLCWriteBool(motorFWD[1], true);
                    while (!InfeedSensor(3) && !AbortCheck())
                    {
                        Thread.Sleep(precisionPollRate);
                    }
                    if (!fLastAbort)
                    {
                        while (InfeedSensor(3) && !AbortCheck())
                        {
                            Thread.Sleep(precisionPollRate);
                        }
                        if (!fLastAbort)
                        {
                            PLCWriteBool(motorFWD[4], false);
                            PLCWriteBool(motorFWD[3], false);
                            PLCWriteBool(motorFWD[2], false);
                            PLCWriteBool(motorFWD[1], false);

                            // Now inch*2 up to last sensor
                            PLCWriteInt(HMI_Conv_FWD_Speed, inchingSpeed * 2);
                            PLCWriteBool(motorFWD[4], true);
                            while (!InfeedSensor(4) && !AbortCheck())
                            {
                                Thread.Sleep(precisionPollRate);
                            }
                            PLCWriteBool(motorFWD[4], false);
                            if (!fLastAbort) ret = ResultCode.Ok;
                        }
                    }
                    StopInfeedConveyor();
                }
            }

            Crawl("APISendToInfeed ends ret=" + ret.ToString());
            pauseTagPolling = false;
            return ret;
        }

        public ResultCode APISendToBuffer(string toteId)
        {
            // Validates the preconditions of a moving from conveyor to the buffer request:
            // 1.  There is something on the conveyor.
            // 2. There is only one item on the conveyor:
            // 2a.  There is a tote at the dock available to load and nothing in the secure
            // area of the conveyor. Or:
            // 2b.  There is something at the secure area of the conveyor and nothing at the
            // dock.
            // 3.  There is room in the buffer.
            // 4.  A tote with the given ID isn't already in the buffer.

            ResultCode ret = ResultCode.UnknownResultCode;

            Crawl("APISendToBuffer begins");
            fAbort = false;
            pauseTagPolling = true;

            int n = SnapshotOccupied();

            // Are we full?
            bool GIBfull = (gibZones.Find(z => z.fStorageZone == true && z.occupied == false) == null);
            bool toteIdSpecified = toteId != null && toteId.Length > 0;

            if (n == 0)
            {
                CrawlError("APISendToBuffer: No tote on conveyor");
                ret = ResultCode.NotPresent;
            }
            else if (n > 1)
            {
                CrawlError("APISendToBuffer: More than one tote on conveyor");
                ret = ResultCode.MultipleTotes;
            }
            else if (GIBfull)
            {
                CrawlError("APISendToBuffer: GIB is full");
                ret = ResultCode.NoCapacity;
            }
            else if (toteIdSpecified && (gibZones.Find(z => z.fStorageZone == true && z.toteID == toteId) != null))
            {
                CrawlError("APISendToBuffer: Specified toteId is already in buffer: " + toteId);
                ret = ResultCode.ToteAlreadyPresent;
            }
            else if (!toteIdSpecified && (gibZones.Find(z => z.fStorageZone == true && z.toteID == inventory.infeedInventory) != null))
            {
                CrawlError("APISendToBuffer: Current infeed inventory toteId is already in buffer: " + inventory.infeedInventory);
                ret = ResultCode.ToteAlreadyPresent;
            }
            else
            {
                ret = ResultCode.CommandInterrupted;

                // Drive infeed inventory if toteId is specified
                if (toteId != null)
                    if (toteId.Length > 0)
                        inventory.infeedInventory = toteId;

                // Don't always need to run all motors!, but safer
                if (WaitMotionComplete())
                {
                    SetInfeed(true);
                    PLCWriteBool(motorREV[1], true);
                    PLCWriteBool(motorREV[2], true);
                    PLCWriteBool(motorREV[3], true);
                    PLCWriteBool(motorREV[4], true);
                    // Get it onto thew lift
                    while (!LiftSensor(2) && !AbortCheck())
                    {
                        Thread.Sleep(precisionPollRate);
                    }
                    StopInfeedConveyor();
                }

                // The abort process here is very unsafe
                if (!fLastAbort)
                {
                    // Wait for lift to depart level 1
                    while (PLCReadInt32(Lift_Current_Level) == 1 && !AbortCheck())
                    {
                        Thread.Sleep(50);
                    }
                    if (!fLastAbort)
                    {
                        // Wait for tote to exit lift
                        while ((LiftSensor(1) || LiftSensor(2)) && !AbortCheck())
                        {
                            Thread.Sleep(250);
                        }

                        // Inventory projection!
                        List<string> newBufferInventory = inventory.bufferInventory;
                        if (newBufferInventory.Count < 1)
                        {
                            inventory.status = "Adding first tote to buffer: " + inventory.infeedInventory;
                            newBufferInventory.Add(inventory.infeedInventory);
                        }
                        else
                        {
                            string lastToteExamined = "";
                            bool stillLooking = true;
                            // Find last tote in buffer and first empty zone
                            foreach (GibZone zone in gibZones)
                            {
                                if (zone.fStorageZone && stillLooking)
                                {
                                    if (zone.occupied)
                                    {
                                        lastToteExamined = zone.toteID;
                                        Crawl("InventoryProj: lastToteExamined: " + lastToteExamined);
                                    }
                                    else // This is where we will be stored, right after lastToteExamined
                                    {
                                        int index = newBufferInventory.FindIndex(x => x.Equals(lastToteExamined));
                                        Crawl("InventoryProj: Putting tote after " + lastToteExamined + " index=" + index.ToString());
                                        if (index == newBufferInventory.Count)
                                        {
                                            inventory.status = "InventoryProj: Add " + inventory.infeedInventory;
                                            newBufferInventory.Add(inventory.infeedInventory);
                                        }
                                        else
                                        {
                                            int insertPoint = index + 1;
                                            inventory.status = "InventoryProj: Insert " + inventory.infeedInventory + " at " + insertPoint.ToString();
                                            newBufferInventory.Insert(insertPoint, inventory.infeedInventory);
                                        }

                                        stillLooking = false;
                                    }
                                }
                            }
                        }
                        inventory.infeedInventory = ""; // We got rid of it
                        inventory.bufferInventory = newBufferInventory;
                        WaitMotionComplete(); // Need totes to settle into position since next ask might be for tote

                        ret = ResultCode.Ok;
                    }
                }
            }

            Crawl("APISendToBuffer ends ret=" + ret.ToString());
            pauseTagPolling = false;
            return ret;
        }
    }
}
