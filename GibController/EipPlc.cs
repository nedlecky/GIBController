// Interface from C# to PLC
using LibplctagWrapper;
using System;
using System.Collections;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GibController
{
    public partial class MainForm : Form
    {
        const int tagReadWriteDelay = 100;

        // Make an Int8 (called SINT on the PLC) tag reference
        private Tag AddTag8(string name)
        {

            Tag tag = new Tag(GibPlcIpTxt.Text, "1, 0", CpuType.LGX, name, DataType.Int8, 1);
            libPlcClient.AddTag(tag);
            while (libPlcClient.GetStatus(tag) == Libplctag.PLCTAG_STATUS_PENDING)
            {
                Thread.Sleep(100);
            }
            if (libPlcClient.GetStatus(tag) != Libplctag.PLCTAG_STATUS_OK)
            {
                CrawlError("Could not set up tag internal state. Error: " + libPlcClient.DecodeError(libPlcClient.GetStatus(tag)));
                return null;
            }

            return tag;
        }


        public int plcTagReadRetry = 0;
        public int plcTagReadFail = 0;
        public bool ReadTag(Tag tag)
        {
            int readResult = Libplctag.PLCTAG_ERR_BAD_DATA;
            for (int i = 0; i < 3 && readResult != Libplctag.PLCTAG_STATUS_OK; i++)
            {
                try
                {
                    readResult = libPlcClient.ReadTag(tag, tagReadWriteDelay);
                }
                catch
                {
                    CrawlError("Realtime: ReadTag error");
                }
                if (readResult != Libplctag.PLCTAG_STATUS_OK)
                {
                    CrawlError("Realtime: ReadTag retry");
                    plcTagReadRetry++;
                    Thread.Sleep(50);
                }
            }

            if (readResult != Libplctag.PLCTAG_STATUS_OK)
            {
                CrawlError("Unable to read PLC tag " + tag.Name + "  Error: " + libPlcClient.DecodeError(readResult));
                plcTagReadFail++;
                return false;
            }
            return true;
        }

        public int plcTagWriteRetry = 0;
        public int plcTagWriteFail = 0;
        public bool WriteTag(Tag tag)
        {
            int writeResult = Libplctag.PLCTAG_ERR_BAD_DATA;

            for (int i = 0; i < 3 && writeResult != Libplctag.PLCTAG_STATUS_OK; i++)
            {
                try
                {
                    writeResult = libPlcClient.WriteTag(tag, tagReadWriteDelay);
                }
                catch
                {
                    CrawlError("Realtime: WriteTag error");
                }
                if (writeResult != Libplctag.PLCTAG_STATUS_OK) Thread.Sleep(100);
            }

            if (writeResult != Libplctag.PLCTAG_STATUS_OK)
            {
                CrawlError("Unable to write PLC tag " + tag.Name + "  Error: " + libPlcClient.DecodeError(writeResult));
                return false;
            }
            return true;
        }

        // Write to a bool tag
        private void PLCWriteBool(Tag t, bool f)
        {
            libPlcClient.SetInt8Value(t, 0, (sbyte)(f ? 1 : 0));
            WriteTag(t);
        }

        // Write to an int32 tag
        private void PLCWriteInt(Tag t, int x)
        {
            libPlcClient.SetInt32Value(t, 0, x);
            WriteTag(t);
        }

        public void WriteOutputDINT(Tag tagOutputDINT, int[] data)
        {

            // set values on the tag buffer
            for (int i = 0; i < data.Length; i++)
            {
                libPlcClient.SetInt32Value(tagOutputDINT, i * tagOutputDINT.ElementSize, data[i]); // write 10 on TestDINTArray[0]
            }

            WriteTag(tagOutputDINT);
        }


        // Read a bool tag
        bool PLCReadBool(Tag tag)
        {
            if (ReadTag(tag))
                return libPlcClient.GetInt8Value(tag, 0) > 0;
            else
                return false;
        }

        // Read an int8/SINT tag
        sbyte PLCReadInt8(Tag tag)
        {
            if (ReadTag(tag))
                return libPlcClient.GetInt8Value(tag, 0);
            else
                return -1;
        }

        // Read an int32 tag
        int PLCReadInt32(Tag tag)
        {
            if (ReadTag(tag))
                return libPlcClient.GetInt32Value(tag, 0);
            else
                return -1;
        }

        public int[] ReadInputDINT(Tag tagInputDINT, int numDINTs)
        {
            int[] InputArray = new int[numDINTs];
            //Crawl("Realtime: RID1 " + numDINTs.ToString());
            if (!ReadTag(tagInputDINT))
                CrawlError("Realtime: RID2fail ");
            else
            {
                //Crawl("Realtime: RID2ok ");

                // Convert the data
                for (int i = 0; i < InputArray.Length; i++)
                {
                    InputArray[i] = libPlcClient.GetInt32Value(tagInputDINT, i * tagInputDINT.ElementSize); // multiply with tag.ElementSize to keep indexes consistant with the indexes on the plc
                    //Crawl("Realtime: RID3 " + i.ToString());
                }
            }

            return InputArray;
        }

        public String[] ReadInputStrings(Tag tagStrings, int numStrings)
        {
            String[] InputString = new string[numStrings];

            ReadTag(tagStrings);

            // Convert the data
            for (int k = 0; k < numStrings; k++)
            {
                InputString[k] = "";
                int StringLen = libPlcClient.GetInt32Value(tagStrings, 0);
                for (int i = 0; i < StringLen; i++)
                {
                    short ascii = libPlcClient.GetInt8Value(tagStrings, 88 * k + (4 + i)); // multiply with tag.ElementSize to keep indexes consistant with the indexes on the plc
                    InputString[k] = InputString[k] + Convert.ToChar(ascii);
                }
            }
            return InputString;
        }


        public int GetIntFromBitArray(bool[] bits)
        {

            if (bits.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            BitArray arr = new BitArray(bits);
            int[] data = new int[1];
            arr.CopyTo(data, 0);
            return data[0];

        }
        public bool[] GetBitArrayfromInt(Int32 myInt)
        {
            bool[] bits = new bool[32];
            char[] myChars = Convert.ToString(myInt, 2).ToCharArray();
            for (var c = myChars.Length - 1; c >= 0; c--)
            {
                switch (myChars[c])
                {
                    case '0':
                        bits[myChars.Length - 1 - c] = false;
                        break;
                    case '1':
                        bits[myChars.Length - 1 - c] = true;
                        break;
                }
            }
            return bits;
        }

        static public bool pauseTagPolling = false;
        static public bool tagPollingSleeping = false;
        static public bool abortPlcPollingThread = false;
        static public bool plcPollingThreadRunning = false;

        bool inventoryUpdated = false;
        void WaitInventoryUpdate()
        {
            inventoryUpdated = false;

            while (!inventoryUpdated)
                Thread.Sleep(100);
        }

        // PLC tag reading thread
        static int loopCount = 0;
        private void PlcGetDataProcess()
        {
            abortPlcPollingThread = false;
            plcPollingThreadRunning = true;

            while (!abortPlcPollingThread)
            {
                try
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    loopCount++;
                    if (!pauseTagPolling || loopCount % 2 == 0) // Pause really just slows by a factor of 2....
                    {
                        // Read PEs and populate appropriate GIBzones
                        int z = 0;
                        bool stillGoing = true;
                        for (int i = 1; i <= 7 && stillGoing; i++)
                        {
                            sbyte pe = PLCReadInt8(conveyorSensors[i]);

                            switch (i)
                            {
                                case 1: // Infeed
                                    bool dockState = (pe & 8) == 0;
                                    // Handles Async dock state messages
                                    if (commandServer != null) commandServer.SendDockMonitorAsync(dockState);
                                    gibZones[z++].occupied = dockState;
                                    gibZones[z++].occupied = (pe & 4) == 0;
                                    gibZones[z++].occupied = (pe & 2) == 0;
                                    gibZones[z++].occupied = (pe & 1) == 0;
                                    break;
                                case 2: // Lift
                                    gibZones[z++].occupied = (pe & 1) == 0;
                                    gibZones[z++].occupied = (pe & 2) == 0;
                                    break;
                                default: // Storage levels... 16 max, we may run out
                                    try
                                    {
                                        gibZones[z++].occupied = (pe & 8) == 0;
                                        gibZones[z++].occupied = (pe & 4) == 0;
                                        gibZones[z++].occupied = (pe & 2) == 0;
                                        gibZones[z++].occupied = (pe & 1) == 0;
                                    }
                                    catch
                                    {
                                        // Ran out of zones... means we've set capacity to < 16!
                                        stillGoing = false;
                                    }
                                    break;
                            }
                        }

                        // Set toteIDs per zone based on inventory
                        int nZone = 0;
                        int nStored = 0;

                        foreach (GibZone zone in gibZones)
                        {
                            zone.toteID = "";
                            if (zone.occupied)
                            {
                                if (nZone < 4)  // Anything in the infeed
                                    zone.toteID = inventory.infeedInventory;
                                else if (nZone < 6)  // The lift
                                    zone.toteID = "LIFT";
                                else if (nZone < 10) // Level 1 isn't tracked
                                {
                                    zone.toteID = "TMP";
                                    nStored++;
                                }
                                else
                                {
                                    // We store to zones 10-13 14-17 18-21 22-25
                                    try
                                    {
                                        if (inventory.bufferInventory.Count - 1 < nStored)
                                        {
                                            zone.toteID = "No Inventory";
                                            inventory.status = "More totes seen than inventory";
                                        }
                                        else
                                        {
                                            zone.toteID = inventory.bufferInventory[nStored++];
                                            //inventory.status = "OK";
                                        }
                                    }
                                    catch
                                    {
                                        inventory.status = "Inventory parsing error";
                                    }
                                }
                            }
                            nZone++;
                        }
                        inventoryUpdated = true;
                        currentBufferToteCount = nStored;
                        if (nStored < inventory.bufferInventory.Count)
                            inventory.status = "More totes in inventory (" + inventory.bufferInventory.Count.ToString() + ") than seen (" + nStored.ToString() + ")";

                        // Fill in all the UI controls with contents and occupied/not occupied
                        foreach (GibZone zone in gibZones)
                        {
                            if (zone.Label1 != null)
                                if (zone.occupied)
                                    Invoke(new Action(() =>
                                    {
                                        zone.Label1.BackColor = Color.LightGreen;
                                        zone.Label1.Text = zone.toteID;
                                    }));
                                else
                                    Invoke(new Action(() =>
                                    {
                                        zone.Label1.BackColor = Color.LightGray;
                                        zone.Label1.Text = zone.toteID;
                                    }));

                        }

                        // Get system state and color it, decide which pushbuttons to enable
                        int state = PLCReadInt32(GibSystemState);
                        GibStateName = "??";
                        Color statecolor = Color.LightGreen;
                        bool startEnable = false;
                        bool stopEnable = false;
                        bool resetEnable = false;
                        switch (state)
                        {
                            case 0: GibStateName = "STOPPED"; statecolor = Color.LightPink; startEnable = true; resetEnable = true; break;
                            case 1: GibStateName = "RESETTING"; statecolor = Color.LightYellow; break;
                            case 2: GibStateName = "READY"; statecolor = Color.Yellow; startEnable = true; stopEnable = true; resetEnable = true; break;
                            case 3: GibStateName = "STARTING"; statecolor = Color.LightGreen; break;
                            case 4: GibStateName = "RUNNING"; statecolor = Color.Lime; stopEnable = true; break;
                            case 5: GibStateName = "STOPPING"; statecolor = Color.LightPink; break;
                            default: GibStateName = "UNKNOWN"; resetEnable = true; break;
                        }
                        Invoke(new Action(() =>
                        {
                            GIBStateLbl.Text = GibStateName;
                            GIBStateLbl.BackColor = statecolor;
                            HMI_PB_StartBtn.Enabled = startEnable;
                            HMI_PB_StopBtn.Enabled = stopEnable;
                            HMI_PB_ResetBtn.Enabled = resetEnable;
                        }));

                        EStop = PLCReadBool(E_Stop_Pressed);
                        Invoke(new Action(() =>
                        {
                            if (EStop)
                            {
                                EstopLbl.Text = "E-STOP";
                                EstopLbl.BackColor = Color.Red;
                            }
                            else
                            {
                                EstopLbl.Text = "NO E-STOP";
                                EstopLbl.BackColor = Color.Green;
                            }
                        }));

                        GibMotionComplete = PLCReadBool(Motion_Complete);
                        Invoke(new Action(() =>
                        {
                            if (!GibMotionComplete)
                            {
                                MotionCompleteLbl.Text = "IN MOTION";
                                MotionCompleteLbl.BackColor = Color.Red;
                            }
                            else
                            {
                                MotionCompleteLbl.Text = "MOTION COMPLETE";
                                MotionCompleteLbl.BackColor = Color.Green;
                            }
                        }));

                        // Get Infeed/Outfeed mode
                        bool d = PLCReadBool(HMI_Infeed_Mode);
                        if (d)
                            Invoke(new Action(() => { InfeedRad.Checked = true; }));
                        else
                            Invoke(new Action(() => { OutfeedRad.Checked = true; }));

                        // Figure out batch, auto, or single
                        bool fSingle = PLCReadBool(HMI_Outfeed_Single);
                        if (fSingle)
                        {
                            Invoke(new Action(() => { ModeSingleRad.Checked = true; }));
                        }
                        else
                        {
                            bool fBatch = PLCReadBool(HMI_Outfeed_Batch);
                            if (fBatch)
                            {
                                Invoke(new Action(() => { ModeBatchRad.Checked = true; }));
                            }
                            else
                            {
                                bool fAuto = PLCReadBool(HMI_Outfeed_Auto);
                                if (fAuto)
                                    Invoke(new Action(() => { ModeAutoRad.Checked = true; }));
                            }

                        }
                    }

                    // VFX PLC Connection Keep-Alive!! You will crash if you don't do this regularly... the next time you do!
                    vfxControl.ReadDataNow();

                    watch.Stop();
                    //Crawl("PlcGetDataProcess execution time: " + watch.ElapsedMilliseconds.ToString() + "mS abort=" + abortPlcPollingThread);
                }
                catch (Exception e)
                {
                    CrawlError("PlcGetDataProcess bombed: " + e.ToString());
                }

                tagPollingSleeping = true;
                Thread.Sleep(250);
                tagPollingSleeping = false;
            }

            Crawl("PlcGetDataProcess ends");
            plcPollingThreadRunning = false;
        }

    }
}

