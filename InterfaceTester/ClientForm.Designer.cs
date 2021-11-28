namespace GibController
{
    partial class ClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.CrawlerRTB = new System.Windows.Forms.RichTextBox();
            this.MessageTmr = new System.Windows.Forms.Timer(this.components);
            this.ConnectBtn = new System.Windows.Forms.Button();
            this.InterfaceTesterIpTxt = new System.Windows.Forms.TextBox();
            this.InterfaceTesterPortTxt = new System.Windows.Forms.TextBox();
            this.AcceptFromAgvBtn = new System.Windows.Forms.Button();
            this.SendToAGVBtn = new System.Windows.Forms.Button();
            this.SendToDockBtn = new System.Windows.Forms.Button();
            this.ScanRequestBtn = new System.Windows.Forms.Button();
            this.SendToBufferBtn = new System.Windows.Forms.Button();
            this.AbortBtn = new System.Windows.Forms.Button();
            this.DesiredDockToteTxt = new System.Windows.Forms.TextBox();
            this.GetStatusBtn = new System.Windows.Forms.Button();
            this.CommRTB = new System.Windows.Forms.RichTextBox();
            this.SaveBtn = new System.Windows.Forms.Button();
            this.InitTmr = new System.Windows.Forms.Timer(this.components);
            this.ScannerRad = new System.Windows.Forms.RadioButton();
            this.BufferRad = new System.Windows.Forms.RadioButton();
            this.DockRad = new System.Windows.Forms.RadioButton();
            this.ExitBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.DesiredScanToteTxt = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SpecifiedToteNameTxt = new System.Windows.Forms.TextBox();
            this.Stress1Btn = new System.Windows.Forms.Button();
            this.AllCycleBtn = new System.Windows.Forms.Button();
            this.PositionNumberTxt = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.LogfileTxt = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.CyclesCompleteLbl = new System.Windows.Forms.Label();
            this.ClearCycleCounterBtn = new System.Windows.Forms.Button();
            this.PositionIncrementTxt = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.PositionMaxTxt = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.PositionMinTxt = new System.Windows.Forms.TextBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.ScanTypeAllRad = new System.Windows.Forms.RadioButton();
            this.ScanTypeNothingRad = new System.Windows.Forms.RadioButton();
            this.ScanTypeIdRad = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.DockBounceBtn = new System.Windows.Forms.Button();
            this.groupBox7.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // CrawlerRTB
            // 
            this.CrawlerRTB.Location = new System.Drawing.Point(12, 599);
            this.CrawlerRTB.Name = "CrawlerRTB";
            this.CrawlerRTB.Size = new System.Drawing.Size(725, 446);
            this.CrawlerRTB.TabIndex = 0;
            this.CrawlerRTB.Text = "";
            this.CrawlerRTB.WordWrap = false;
            // 
            // MessageTmr
            // 
            this.MessageTmr.Tick += new System.EventHandler(this.MessageTmr_Tick);
            // 
            // ConnectBtn
            // 
            this.ConnectBtn.Location = new System.Drawing.Point(12, 9);
            this.ConnectBtn.Name = "ConnectBtn";
            this.ConnectBtn.Size = new System.Drawing.Size(128, 23);
            this.ConnectBtn.TabIndex = 2;
            this.ConnectBtn.Text = "Connect";
            this.ConnectBtn.UseVisualStyleBackColor = true;
            this.ConnectBtn.Click += new System.EventHandler(this.ConnectBtn_Click);
            // 
            // InterfaceTesterIpTxt
            // 
            this.InterfaceTesterIpTxt.Location = new System.Drawing.Point(12, 38);
            this.InterfaceTesterIpTxt.Name = "InterfaceTesterIpTxt";
            this.InterfaceTesterIpTxt.Size = new System.Drawing.Size(83, 20);
            this.InterfaceTesterIpTxt.TabIndex = 3;
            this.InterfaceTesterIpTxt.Text = "192.168.1.103";
            // 
            // InterfaceTesterPortTxt
            // 
            this.InterfaceTesterPortTxt.Location = new System.Drawing.Point(101, 38);
            this.InterfaceTesterPortTxt.Name = "InterfaceTesterPortTxt";
            this.InterfaceTesterPortTxt.Size = new System.Drawing.Size(39, 20);
            this.InterfaceTesterPortTxt.TabIndex = 4;
            this.InterfaceTesterPortTxt.Text = "1000";
            // 
            // AcceptFromAgvBtn
            // 
            this.AcceptFromAgvBtn.Location = new System.Drawing.Point(242, 13);
            this.AcceptFromAgvBtn.Name = "AcceptFromAgvBtn";
            this.AcceptFromAgvBtn.Size = new System.Drawing.Size(107, 22);
            this.AcceptFromAgvBtn.TabIndex = 5;
            this.AcceptFromAgvBtn.Text = "Accept From AGV";
            this.AcceptFromAgvBtn.UseVisualStyleBackColor = true;
            this.AcceptFromAgvBtn.Click += new System.EventHandler(this.AcceptFromAgvBtn_Click);
            // 
            // SendToAGVBtn
            // 
            this.SendToAGVBtn.Location = new System.Drawing.Point(242, 38);
            this.SendToAGVBtn.Name = "SendToAGVBtn";
            this.SendToAGVBtn.Size = new System.Drawing.Size(107, 22);
            this.SendToAGVBtn.TabIndex = 6;
            this.SendToAGVBtn.Text = "Send To AGV";
            this.SendToAGVBtn.UseVisualStyleBackColor = true;
            this.SendToAGVBtn.Click += new System.EventHandler(this.SendToAGVBtn_Click);
            // 
            // SendToDockBtn
            // 
            this.SendToDockBtn.Location = new System.Drawing.Point(374, 12);
            this.SendToDockBtn.Name = "SendToDockBtn";
            this.SendToDockBtn.Size = new System.Drawing.Size(107, 22);
            this.SendToDockBtn.TabIndex = 7;
            this.SendToDockBtn.Text = "Send To Dock";
            this.SendToDockBtn.UseVisualStyleBackColor = true;
            this.SendToDockBtn.Click += new System.EventHandler(this.SendToDockBtn_Click);
            // 
            // ScanRequestBtn
            // 
            this.ScanRequestBtn.Location = new System.Drawing.Point(507, 12);
            this.ScanRequestBtn.Name = "ScanRequestBtn";
            this.ScanRequestBtn.Size = new System.Drawing.Size(133, 22);
            this.ScanRequestBtn.TabIndex = 8;
            this.ScanRequestBtn.Text = "Scan Request";
            this.ScanRequestBtn.UseVisualStyleBackColor = true;
            this.ScanRequestBtn.Click += new System.EventHandler(this.ScanRequestBtn_Click);
            // 
            // SendToBufferBtn
            // 
            this.SendToBufferBtn.Location = new System.Drawing.Point(242, 105);
            this.SendToBufferBtn.Name = "SendToBufferBtn";
            this.SendToBufferBtn.Size = new System.Drawing.Size(107, 22);
            this.SendToBufferBtn.TabIndex = 9;
            this.SendToBufferBtn.Text = "Send To Buffer";
            this.SendToBufferBtn.UseVisualStyleBackColor = true;
            this.SendToBufferBtn.Click += new System.EventHandler(this.SendToBufferBtn_Click);
            // 
            // AbortBtn
            // 
            this.AbortBtn.Location = new System.Drawing.Point(164, 13);
            this.AbortBtn.Name = "AbortBtn";
            this.AbortBtn.Size = new System.Drawing.Size(72, 72);
            this.AbortBtn.TabIndex = 10;
            this.AbortBtn.Text = "Abort";
            this.AbortBtn.UseVisualStyleBackColor = true;
            this.AbortBtn.Click += new System.EventHandler(this.AbortBtn_Click);
            // 
            // DesiredDockToteTxt
            // 
            this.DesiredDockToteTxt.Location = new System.Drawing.Point(374, 50);
            this.DesiredDockToteTxt.Name = "DesiredDockToteTxt";
            this.DesiredDockToteTxt.Size = new System.Drawing.Size(107, 20);
            this.DesiredDockToteTxt.TabIndex = 11;
            // 
            // GetStatusBtn
            // 
            this.GetStatusBtn.Location = new System.Drawing.Point(242, 63);
            this.GetStatusBtn.Name = "GetStatusBtn";
            this.GetStatusBtn.Size = new System.Drawing.Size(107, 22);
            this.GetStatusBtn.TabIndex = 12;
            this.GetStatusBtn.Text = "Get Status";
            this.GetStatusBtn.UseVisualStyleBackColor = true;
            this.GetStatusBtn.Click += new System.EventHandler(this.GetStatusBtn_Click);
            // 
            // CommRTB
            // 
            this.CommRTB.Location = new System.Drawing.Point(12, 238);
            this.CommRTB.Name = "CommRTB";
            this.CommRTB.Size = new System.Drawing.Size(725, 355);
            this.CommRTB.TabIndex = 13;
            this.CommRTB.Text = "";
            this.CommRTB.WordWrap = false;
            // 
            // SaveBtn
            // 
            this.SaveBtn.Location = new System.Drawing.Point(12, 64);
            this.SaveBtn.Name = "SaveBtn";
            this.SaveBtn.Size = new System.Drawing.Size(128, 23);
            this.SaveBtn.TabIndex = 14;
            this.SaveBtn.Text = "Save";
            this.SaveBtn.UseVisualStyleBackColor = true;
            this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
            // 
            // InitTmr
            // 
            this.InitTmr.Tick += new System.EventHandler(this.InitTmr_Tick);
            // 
            // ScannerRad
            // 
            this.ScannerRad.AutoSize = true;
            this.ScannerRad.Location = new System.Drawing.Point(564, 35);
            this.ScannerRad.Name = "ScannerRad";
            this.ScannerRad.Size = new System.Drawing.Size(65, 17);
            this.ScannerRad.TabIndex = 44;
            this.ScannerRad.TabStop = true;
            this.ScannerRad.Text = "Scanner";
            this.ScannerRad.UseVisualStyleBackColor = true;
            // 
            // BufferRad
            // 
            this.BufferRad.AutoSize = true;
            this.BufferRad.Location = new System.Drawing.Point(507, 52);
            this.BufferRad.Name = "BufferRad";
            this.BufferRad.Size = new System.Drawing.Size(53, 17);
            this.BufferRad.TabIndex = 43;
            this.BufferRad.TabStop = true;
            this.BufferRad.Text = "Buffer";
            this.BufferRad.UseVisualStyleBackColor = true;
            // 
            // DockRad
            // 
            this.DockRad.AutoSize = true;
            this.DockRad.Checked = true;
            this.DockRad.Location = new System.Drawing.Point(507, 35);
            this.DockRad.Name = "DockRad";
            this.DockRad.Size = new System.Drawing.Size(51, 17);
            this.DockRad.TabIndex = 42;
            this.DockRad.TabStop = true;
            this.DockRad.Text = "Dock";
            this.DockRad.UseVisualStyleBackColor = true;
            // 
            // ExitBtn
            // 
            this.ExitBtn.Location = new System.Drawing.Point(677, 10);
            this.ExitBtn.Name = "ExitBtn";
            this.ExitBtn.Size = new System.Drawing.Size(60, 42);
            this.ExitBtn.TabIndex = 45;
            this.ExitBtn.Text = "Exit";
            this.ExitBtn.UseVisualStyleBackColor = true;
            this.ExitBtn.Click += new System.EventHandler(this.ExitBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(371, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 13);
            this.label1.TabIndex = 46;
            this.label1.Text = "Optional Desired Tote";
            // 
            // DesiredScanToteTxt
            // 
            this.DesiredScanToteTxt.Location = new System.Drawing.Point(564, 51);
            this.DesiredScanToteTxt.Name = "DesiredScanToteTxt";
            this.DesiredScanToteTxt.Size = new System.Drawing.Size(76, 20);
            this.DesiredScanToteTxt.TabIndex = 47;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(253, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 49;
            this.label2.Text = "Optional Tote ID";
            // 
            // SpecifiedToteNameTxt
            // 
            this.SpecifiedToteNameTxt.Location = new System.Drawing.Point(244, 145);
            this.SpecifiedToteNameTxt.Name = "SpecifiedToteNameTxt";
            this.SpecifiedToteNameTxt.Size = new System.Drawing.Size(107, 20);
            this.SpecifiedToteNameTxt.TabIndex = 48;
            // 
            // Stress1Btn
            // 
            this.Stress1Btn.Location = new System.Drawing.Point(12, 104);
            this.Stress1Btn.Name = "Stress1Btn";
            this.Stress1Btn.Size = new System.Drawing.Size(128, 23);
            this.Stress1Btn.TabIndex = 50;
            this.Stress1Btn.Text = "100 GetStatus";
            this.Stress1Btn.UseVisualStyleBackColor = true;
            this.Stress1Btn.Click += new System.EventHandler(this.Stress1Btn_Click);
            // 
            // AllCycleBtn
            // 
            this.AllCycleBtn.Location = new System.Drawing.Point(29, 31);
            this.AllCycleBtn.Name = "AllCycleBtn";
            this.AllCycleBtn.Size = new System.Drawing.Size(75, 23);
            this.AllCycleBtn.TabIndex = 52;
            this.AllCycleBtn.Text = "All Cycle";
            this.AllCycleBtn.UseVisualStyleBackColor = true;
            this.AllCycleBtn.Click += new System.EventHandler(this.AllCycleBtn_Click);
            // 
            // PositionNumberTxt
            // 
            this.PositionNumberTxt.Location = new System.Drawing.Point(110, 34);
            this.PositionNumberTxt.Name = "PositionNumberTxt";
            this.PositionNumberTxt.Size = new System.Drawing.Size(27, 20);
            this.PositionNumberTxt.TabIndex = 53;
            this.PositionNumberTxt.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 215);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 54;
            this.label3.Text = "Logfile";
            // 
            // LogfileTxt
            // 
            this.LogfileTxt.Location = new System.Drawing.Point(58, 212);
            this.LogfileTxt.Name = "LogfileTxt";
            this.LogfileTxt.Size = new System.Drawing.Size(237, 20);
            this.LogfileTxt.TabIndex = 55;
            this.LogfileTxt.Text = "C:\\Users\\GIB1\\Desktop\\clientlog.txt";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(111, 93);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 13);
            this.label4.TabIndex = 56;
            this.label4.Text = "Cycles Complete:";
            // 
            // CyclesCompleteLbl
            // 
            this.CyclesCompleteLbl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CyclesCompleteLbl.Location = new System.Drawing.Point(199, 89);
            this.CyclesCompleteLbl.Name = "CyclesCompleteLbl";
            this.CyclesCompleteLbl.Size = new System.Drawing.Size(58, 21);
            this.CyclesCompleteLbl.TabIndex = 57;
            this.CyclesCompleteLbl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ClearCycleCounterBtn
            // 
            this.ClearCycleCounterBtn.Location = new System.Drawing.Point(263, 90);
            this.ClearCycleCounterBtn.Name = "ClearCycleCounterBtn";
            this.ClearCycleCounterBtn.Size = new System.Drawing.Size(47, 22);
            this.ClearCycleCounterBtn.TabIndex = 58;
            this.ClearCycleCounterBtn.Text = "Clear";
            this.ClearCycleCounterBtn.UseVisualStyleBackColor = true;
            // 
            // PositionIncrementTxt
            // 
            this.PositionIncrementTxt.Location = new System.Drawing.Point(189, 34);
            this.PositionIncrementTxt.Name = "PositionIncrementTxt";
            this.PositionIncrementTxt.Size = new System.Drawing.Size(37, 20);
            this.PositionIncrementTxt.TabIndex = 59;
            this.PositionIncrementTxt.Text = "1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(164, 37);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(19, 13);
            this.label5.TabIndex = 60;
            this.label5.Text = "+=";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(244, 37);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(27, 13);
            this.label6.TabIndex = 62;
            this.label6.Text = "Max";
            // 
            // PositionMaxTxt
            // 
            this.PositionMaxTxt.Location = new System.Drawing.Point(273, 34);
            this.PositionMaxTxt.Name = "PositionMaxTxt";
            this.PositionMaxTxt.Size = new System.Drawing.Size(37, 20);
            this.PositionMaxTxt.TabIndex = 61;
            this.PositionMaxTxt.Text = "15";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(247, 60);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(24, 13);
            this.label7.TabIndex = 64;
            this.label7.Text = "Min";
            // 
            // PositionMinTxt
            // 
            this.PositionMinTxt.Location = new System.Drawing.Point(273, 57);
            this.PositionMinTxt.Name = "PositionMinTxt";
            this.PositionMinTxt.Size = new System.Drawing.Size(37, 20);
            this.PositionMinTxt.TabIndex = 63;
            this.PositionMinTxt.Text = "0";
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.ScanTypeAllRad);
            this.groupBox7.Controls.Add(this.ScanTypeNothingRad);
            this.groupBox7.Controls.Add(this.ScanTypeIdRad);
            this.groupBox7.Location = new System.Drawing.Point(487, 75);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(201, 33);
            this.groupBox7.TabIndex = 65;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Scan Type";
            // 
            // ScanTypeAllRad
            // 
            this.ScanTypeAllRad.AutoSize = true;
            this.ScanTypeAllRad.Checked = true;
            this.ScanTypeAllRad.Location = new System.Drawing.Point(6, 12);
            this.ScanTypeAllRad.Name = "ScanTypeAllRad";
            this.ScanTypeAllRad.Size = new System.Drawing.Size(36, 17);
            this.ScanTypeAllRad.TabIndex = 61;
            this.ScanTypeAllRad.TabStop = true;
            this.ScanTypeAllRad.Text = "All";
            this.ScanTypeAllRad.UseVisualStyleBackColor = true;
            // 
            // ScanTypeNothingRad
            // 
            this.ScanTypeNothingRad.AutoSize = true;
            this.ScanTypeNothingRad.Location = new System.Drawing.Point(136, 12);
            this.ScanTypeNothingRad.Name = "ScanTypeNothingRad";
            this.ScanTypeNothingRad.Size = new System.Drawing.Size(62, 17);
            this.ScanTypeNothingRad.TabIndex = 63;
            this.ScanTypeNothingRad.Text = "Nothing";
            this.ScanTypeNothingRad.UseVisualStyleBackColor = true;
            // 
            // ScanTypeIdRad
            // 
            this.ScanTypeIdRad.AutoSize = true;
            this.ScanTypeIdRad.Location = new System.Drawing.Point(45, 12);
            this.ScanTypeIdRad.Name = "ScanTypeIdRad";
            this.ScanTypeIdRad.Size = new System.Drawing.Size(85, 17);
            this.ScanTypeIdRad.TabIndex = 62;
            this.ScanTypeIdRad.Text = "Tote ID Only";
            this.ScanTypeIdRad.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.DockBounceBtn);
            this.groupBox1.Controls.Add(this.PositionMaxTxt);
            this.groupBox1.Controls.Add(this.AllCycleBtn);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.PositionNumberTxt);
            this.groupBox1.Controls.Add(this.PositionMinTxt);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.CyclesCompleteLbl);
            this.groupBox1.Controls.Add(this.ClearCycleCounterBtn);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.PositionIncrementTxt);
            this.groupBox1.Location = new System.Drawing.Point(401, 114);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(336, 118);
            this.groupBox1.TabIndex = 66;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Cycle Test";
            // 
            // DockBounceBtn
            // 
            this.DockBounceBtn.Location = new System.Drawing.Point(29, 87);
            this.DockBounceBtn.Name = "DockBounceBtn";
            this.DockBounceBtn.Size = new System.Drawing.Size(75, 23);
            this.DockBounceBtn.TabIndex = 65;
            this.DockBounceBtn.Text = "Dock Bounce";
            this.DockBounceBtn.UseVisualStyleBackColor = true;
            this.DockBounceBtn.Click += new System.EventHandler(this.DockBounceBtn_Click);
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(745, 1057);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.LogfileTxt);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.Stress1Btn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.SpecifiedToteNameTxt);
            this.Controls.Add(this.DesiredScanToteTxt);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ExitBtn);
            this.Controls.Add(this.ScannerRad);
            this.Controls.Add(this.BufferRad);
            this.Controls.Add(this.DockRad);
            this.Controls.Add(this.SaveBtn);
            this.Controls.Add(this.CommRTB);
            this.Controls.Add(this.GetStatusBtn);
            this.Controls.Add(this.DesiredDockToteTxt);
            this.Controls.Add(this.AbortBtn);
            this.Controls.Add(this.SendToBufferBtn);
            this.Controls.Add(this.ScanRequestBtn);
            this.Controls.Add(this.SendToDockBtn);
            this.Controls.Add(this.SendToAGVBtn);
            this.Controls.Add(this.AcceptFromAgvBtn);
            this.Controls.Add(this.InterfaceTesterPortTxt);
            this.Controls.Add(this.InterfaceTesterIpTxt);
            this.Controls.Add(this.ConnectBtn);
            this.Controls.Add(this.CrawlerRTB);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "ClientForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Client";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox CrawlerRTB;
        private System.Windows.Forms.Timer MessageTmr;
        private System.Windows.Forms.Button ConnectBtn;
        private System.Windows.Forms.TextBox InterfaceTesterIpTxt;
        private System.Windows.Forms.TextBox InterfaceTesterPortTxt;
        private System.Windows.Forms.Button AcceptFromAgvBtn;
        private System.Windows.Forms.Button SendToAGVBtn;
        private System.Windows.Forms.Button SendToDockBtn;
        private System.Windows.Forms.Button ScanRequestBtn;
        private System.Windows.Forms.Button SendToBufferBtn;
        private System.Windows.Forms.Button AbortBtn;
        private System.Windows.Forms.TextBox DesiredDockToteTxt;
        private System.Windows.Forms.Button GetStatusBtn;
        private System.Windows.Forms.RichTextBox CommRTB;
        private System.Windows.Forms.Button SaveBtn;
        private System.Windows.Forms.Timer InitTmr;
        private System.Windows.Forms.RadioButton ScannerRad;
        private System.Windows.Forms.RadioButton BufferRad;
        private System.Windows.Forms.RadioButton DockRad;
        private System.Windows.Forms.Button ExitBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox DesiredScanToteTxt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox SpecifiedToteNameTxt;
        private System.Windows.Forms.Button Stress1Btn;
        private System.Windows.Forms.Button AllCycleBtn;
        private System.Windows.Forms.TextBox PositionNumberTxt;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox LogfileTxt;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label CyclesCompleteLbl;
        private System.Windows.Forms.Button ClearCycleCounterBtn;
        private System.Windows.Forms.TextBox PositionIncrementTxt;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox PositionMaxTxt;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox PositionMinTxt;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.RadioButton ScanTypeAllRad;
        private System.Windows.Forms.RadioButton ScanTypeNothingRad;
        private System.Windows.Forms.RadioButton ScanTypeIdRad;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button DockBounceBtn;
    }
}

