using System;
using System.Collections.Generic;

namespace GibController
{


    public partial class VfxControl
    {
        public CognexInterface insight_vfx;
        private const int nBarcdeReaderThreads = 1;
        public bool vfxScanContinuous = false;
        public bool vfxScanTrigger = false;
        public bool vfxScanAbort = false;
        public List<CameraResult> VFXResults;
        public List<string> RawResultsList;

        public struct CameraResult
        {
            public DateTime CaptureTime;
            public string Serial;
            public String Info;
            public float Rotation;
            public int X;
            public int Y;
            public int R;
            public bool resultsValid;
        }

        public int ConnectBarcodes(string ip, string port)
        {
            insight_vfx = new CognexInterface(myForm, ip, port);
            if (insight_vfx != null)
                return insight_vfx.Open();
            else return 1;
        }

        public void DisconnectBarcodes()
        {
            if (insight_vfx != null)
            {
                insight_vfx.Close();
                insight_vfx = null;
            }
        }
        string DecodeBarcodeResult(string result, Queue<String> queue)
        {
            if (result.Length > 2 && !result.Contains("bc1,#ERR"))
            {
                string datetime = DateTime.UtcNow.ToString("MM:ss.fff ");

                queue.Enqueue(datetime + ": " + result);
                return result;
            }
            else return "";
        }

        public CameraResult ParseDiskBarcodeResult(string result)
        {

            CameraResult thisResult = new CameraResult();
            if (result.Length > 2 && !result.Contains("bc1,#ERR"))
            {
                String[] results = result.Split(',');

                String bc1 = results[1];
                float rotation1 = float.Parse(results[2]);
                String bc2 = results[3];

                thisResult.CaptureTime = DateTime.UtcNow;
                if (bc1.Length == 8 || bc1.Length == 9)
                {
                    thisResult.Serial = bc1;
                    thisResult.Rotation = rotation1;
                    if (!bc2.Contains("#ERR")) thisResult.Info = bc2;
                }
                else if (bc2.Length == 8 || bc2.Length == 9)
                {
                    thisResult.Serial = bc2;
                    thisResult.Rotation = rotation1;
                    if (!bc1.Contains("#ERR")) thisResult.Info = bc1;
                }
            }
            return thisResult;
        }

        public CameraResult ParseToteBarcodeResult(string result)
        {

            CameraResult thisResult = new CameraResult();
            if (result.Length > 2 && !result.Contains("bc2,#ERR"))
            {
                String[] results = result.Split(',');

                String bc1 = results[1];

                thisResult.CaptureTime = DateTime.UtcNow;
                if (bc1.Length >= 5 & bc1[0] == 'T') //grab Tote ID from side of tote
                {
                    thisResult.Serial = bc1;
                    thisResult.Rotation = 0;
                }
            }

            return thisResult;
        }

        public CameraResult ParseFiducialResult(string result)
        {

            CameraResult thisResult = new CameraResult();
            if (result.Length > 2 && !result.Contains("fid1,#ERR"))
            {
                String[] results = result.Split(',');

                try
                {
                    thisResult.X = (int)(float.Parse(results[1]) * 1000);
                    thisResult.Y = (int)(float.Parse(results[2]) * 1000);
                    thisResult.R = (int)(float.Parse(results[3]) * 1000);
                    thisResult.resultsValid = (thisResult.X != 0 && thisResult.Y != 0);
                }
                catch
                {
                    CrawlError("Cognex fiducial read return string format error: " + result);
                    thisResult.X = 0;
                    thisResult.Y = 0;
                    thisResult.R = 0;
                    thisResult.resultsValid = false;
                }
            }
            return thisResult;
        }
    }
}
