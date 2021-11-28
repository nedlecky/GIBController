using System.Collections.Generic;
using System.IO;

namespace GibController
{
    class Inventory
    {
        MainForm myForm;
        string inventoryFolder;
        string infeedBackupFile;
        string bufferBackupFile;
        private string _infeedInventory;
        private List<string> _bufferInventory = new List<string>();
        string _status;

        public string status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value != _status)
                {
                    _status = value;
                    myForm.Crawl("Inventory: Status change: " + _status);
                }
            }
        }

        public string infeedInventory
        {
            get
            {
                return _infeedInventory;
            }
            set
            {
                if (value != _infeedInventory)
                {
                    _infeedInventory = value;
                    myForm.Crawl("Inventory: Infeed inventory changed to: " + _infeedInventory);
                    File.WriteAllText(infeedBackupFile, _infeedInventory);
                }
            }
        }

        public List<string> bufferInventory
        {
            get
            {
                return _bufferInventory;
            }
            set
            {
                _bufferInventory = value;
                string report = "Buffer inventory changed to: ";
                using (TextWriter tw = new StreamWriter(bufferBackupFile))
                {
                    bool isFirst = true;
                    foreach (string s in _bufferInventory)
                    {
                        tw.WriteLine(s);
                        if (!isFirst)
                            report += ", ";
                        isFirst = false;
                        report += s;
                    }
                }
                status = report;
            }
        }

        public Inventory(MainForm form, string _inventoryFolder)
        {
            myForm = form;
            inventoryFolder = _inventoryFolder;
            infeedBackupFile = inventoryFolder + "/InfeedInventory.txt";
            bufferBackupFile = inventoryFolder + "/BufferInventory.txt";
            status = "Initialized";

            Load();
        }

        public void Load()
        {
            _infeedInventory = File.ReadAllText(infeedBackupFile);
            _bufferInventory = new List<string>();
            using (var sr = new StreamReader(bufferBackupFile))
            {
                while (sr.Peek() >= 0)
                    _bufferInventory.Add(sr.ReadLine());
            }
            status = "Loaded from files";
            Report();
        }

        void Crawl(string msg)
        {
            myForm.Crawl(msg);
        }

        public void Report()
        {
            Crawl("Inventory: Infeed: " + infeedInventory);
            string report = "Inventory: Buffering " + bufferInventory.Count.ToString() + " totes: ";
            bool isFirst = true;
            foreach (string s in bufferInventory)
            {
                if (!isFirst)
                    report += ", ";
                isFirst = false;
                report += s;
            }

            Crawl(report);
        }

        public bool AreThereDuplicates()
        {
            for (int i = 0; i < bufferInventory.Count; i++)
            {
                string toteId = bufferInventory[i];
                int lastIndex = bufferInventory.LastIndexOf(toteId);
                if (lastIndex != i)
                {
                    myForm.CrawlError("Duplicate inventory " + toteId);
                    return true;
                }
            }

            return false;
        }

        public bool AreThereExtras(int nExpected)
        {
            return bufferInventory.Count > nExpected;
        }

        public bool AreAnyMissing(int nExpected)
        {
            return bufferInventory.Count < nExpected;
        }
    }
}
