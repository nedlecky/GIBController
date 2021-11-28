// Error reporting/logging support
// Note that these are handled through queues so that any of the threads can queue up messages to be displayed. The main thread regularly prints the queues
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GibController
{
    public partial class MainForm : Form
    {
        Queue<string> crawlMessages = new Queue<string>();
        const int maxRtbLength = 1000000;

        // Schedule a standard message
        private static AutoResetEvent m_AutoReset = new AutoResetEvent(true);
        public void Crawl(string message)
        {
            string datetime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            string line = datetime + " " + message;

            // Add "Realtime:" into message to get immediate append/flush/close to file without scroll... intended for high-speed problematic bugs
            // This has rarely caused file open collisions, so should only be enabled when trying to debug highspeed issues
            //if (message.Contains("Realtime:"))
            //{
            //    m_AutoReset.WaitOne();
            //    FileStream fs = File.Open(LogfileTxt.Text, FileMode.Append);
            //    byte[] bytes = Encoding.ASCII.GetBytes(line + "\n");
            //    fs.Write(bytes, 0, bytes.Length);
            //    fs.Flush();
            //    fs.Close();
            //    m_AutoReset.Set();
            //}
            //else
                crawlMessages.Enqueue(line);
        }

        // Schedule an error message
        public void CrawlError(string message)
        {
            Crawl("ERROR: " + message);
        }

        // The scrolls can't grow (successfully) without bound. Cut them to maxLength chars
        private void LimitRTBLength(RichTextBox rtb, int maxLength)
        {
            int currentLength = rtb.TextLength;

            if (currentLength > maxLength)
            {
                rtb.Select(0, currentLength - maxLength);
                rtb.SelectedText = "";
            }
        }

        public void FlushCrawl()
        {
            while (crawlMessages.Count() > 0)
            {
                string message = crawlMessages.Dequeue();

                if (message.Contains("ERROR"))
                {
                    LimitRTBLength(ErrorsRTB, maxRtbLength);
                    CrawlerRTB.SelectionColor = Color.Red;
                    ErrorsRTB.AppendText(message + "\n");
                }
                LimitRTBLength(CrawlerRTB, maxRtbLength);
                CrawlerRTB.AppendText(message + "\n");
                try
                {
                    File.AppendAllText(LogfileTxt.Text, message + "\r\n");
                }
                catch
                {

                }
                CrawlerRTB.ScrollToCaret();
                CrawlerRTB.SelectionColor = System.Drawing.Color.Black;

                // Add message to CommRTB as well if it begins with <== or ==>
                if (message.Contains("<==") || message.Contains("==>"))
                {
                    LimitRTBLength(CommRTB, maxRtbLength);
                    CommRTB.AppendText(message + "\n");
                    CommRTB.ScrollToCaret();
                }

                // Add message to vfxRTB well if it contains VFX
                if (message.Contains("VFX:"))
                {
                    LimitRTBLength(VfxRTB, maxRtbLength);
                    VfxRTB.AppendText(message + "\n");
                    VfxRTB.ScrollToCaret();
                }

                // Add message to InventoryRTB well if it contains Inventory
                if (message.Contains("Inventory:"))
                {
                    LimitRTBLength(InventoryRTB, maxRtbLength);
                    InventoryRTB.AppendText(message + "\n");
                    InventoryRTB.ScrollToCaret();
                }
            }
        }

        // Dump the messages to their respective scroll lists, check for TCP commands, keep inventory status line fresh
        private void MessageTmr_Tick(object sender, EventArgs e)
        {
            FlushCrawl();
            inventoryStatusLbl.Text = inventory.status;

            // TODO is this a poor place to check for commands from DCSW?
            if (commandServer != null) commandServer.ReceiveCommand();
        }
    }
}