// Simple data structures to help associate UI elements with GIB zones and PLC tags
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibplctagWrapper;

namespace GibController
{
    public class GibZone
    {
        public bool fStorageZone;
        public bool occupied;
        public Tag outfeedTag;
        public string toteID;
        public System.Windows.Forms.Label Label1;
        public System.Windows.Forms.Label Label2;
    }

    // Simple class to hold empty/full states of each GIB level
    public class GibLevel
    {
        public Tag Empty;
        public Tag Full;
    }
}
