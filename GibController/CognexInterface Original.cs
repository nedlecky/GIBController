using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace GIBController
{
    public class CognexInterface
    {
        string myIP;
        MainForm myForm;
        Socket socket;
        byte[] bytes = new byte[1024];


        public CognexInterface(MainForm form)
        {
            myForm = form;
        }

        public int Open(string IP, string port)
        {

            myIP = IP;
            myForm.Crawl("Connecting to Cognex sensor " + IP + " " + port + "...");

            Ping ping = new Ping();
            PingReply PR = ping.Send(IP);
            myForm.Crawl("Ping: " + PR.Status.ToString());

            IPAddress ipAddress = IPAddress.Parse(IP);
            IPEndPoint remoteEP = new IPEndPoint(IPAddressToLong(ipAddress), Int32.Parse(port));

            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {
                socket.Connect(remoteEP);
                myForm.Crawl("Cognex socket connected to " + socket.RemoteEndPoint.ToString());
                return 0;
            }
            catch
            {
                // This can happen on powerup of the Cognex isn't online yet!
                myForm.CrawlError("Cognex socket failed to " + IP + ":" + port);
                return 1;

            }

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
        public int Close()
        {
            myForm.Crawl("Close Cognex sensor " + myIP + "...");
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            return 0;
        }

        public string Trigger(string cmd)
        {
            // Encode the data string into a byte array
            byte[] msg = Encoding.ASCII.GetBytes(cmd);

            // Send the data through the socket
            int bytesSent = socket.Send(msg);

            // Receive the response from the remote device
            int bytesRec = socket.Receive(bytes);
            string response = "";
            if (bytesRec > 2)
                response = Encoding.ASCII.GetString(bytes, 0, bytesRec - 2);

            return response;
        }
    }
}
