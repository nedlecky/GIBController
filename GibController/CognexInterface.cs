using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace GibController
{
    public class CognexInterface
    {
        const int BufferSize = 1024;

        private IPEndPoint remoteEndPoint;
        private Socket socket = null;
        IPAddress myIP;
        int myPort;
        MainForm myForm;


        public CognexInterface(MainForm form, string IP, string port)
        {
            myForm = form;

            // Log the IP and Port into the interface for future reinits
            bool pass;
            pass = IPAddress.TryParse(IP, out myIP);
            pass = pass & int.TryParse(port, out myPort);
            // Establish the remote endpoint for the socket.  
            if (pass) remoteEndPoint = new IPEndPoint(myIP, myPort);
            else throw new ArgumentException("Cognex Error: Cannot parse IP Address and Port strings");
        }

        public int Open()
        {

            // Ping the IP and make sure it is online
            if (!Ping()) return -1;

            // Initialize the Socket
            if (socket != null) Close();
            Init();

            // Connect the socket to the remote endpoint, with a few retries
            bool connected = false;
            int retryCount = 0;
            int maxRetries = 3;
            while (!connected & retryCount < maxRetries)
            {
                myForm.Crawl("Cognex connect attempt #" + retryCount);
                connected = Connect();
            }
            if (connected)
            {
                myForm.Crawl("Cognex connected");
                return 0;
            }
            else
            {
                myForm.CrawlError("Cognex connection failed");
                return 1;
            }
        }

        public string Trigger(string cmd)
        {
            int retryCount = 0;
            int maxretry = 10;
            int status = 0;
            string response = null;

            while ((response == null | response == "") & retryCount < maxretry)
            {
                //myForm.Crawl("Cognex Trigger attempt #" + retryCount);
                if (status != 0 | socket == null)
                {
                    myForm.CrawlError("Reopening Cognex connection");
                    status = Open();
                }

                if (status == 0 & socket != null)
                {
                    // Send the data through the socket
                    Send(cmd);
                    // Receive the response from the remote device
                    response = Receive();
                }

                // After a few failed triggers, time to flag the socket for reinit
                if (retryCount == 3)
                {
                    myForm.CrawlError("Cognex connection lost");
                    status = -1;
                }

                retryCount++;
            }

            if (response == null)
            {
                myForm.CrawlError("Cognex camera disconnected, too many trigger retries");
            }

            return response;
        }

        private bool Ping()
        {
            // Quick ping of the address to make sure we're connected
            // and don't waste time with a socket if we dont need to.
            Ping ping = new Ping();
            PingReply PR = ping.Send(myIP);
            myForm.Crawl("Pinging Cognex " + PR.Status.ToString());

            return PR.Status == IPStatus.Success;
        }

        private void Init()
        {
            // Create a TCP/IP socket.  
            socket = new Socket(myIP.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp)
            {
                ExclusiveAddressUse = true,
                LingerState = new LingerOption(true, 10),
                NoDelay = true,
                ReceiveTimeout = 750,
                SendTimeout = 100,
                Ttl = 32,
            };
        }
        private bool Connect()
        {
            // Connect to a remote device.  
            myForm.Crawl("Connecting to Cognex sensor " + myIP.ToString() + " " + myPort.ToString() + "...");
            try
            {
                // Connect to the remote endpoint.  
                socket.Connect(remoteEndPoint);
                return true;
            }

            catch (SocketException)
            {
                //this is a failed-to-connect exception, we should just return false and mute the error reporting
                //myForm.CrawlError(e.ToString());
                return false;
            }
            catch (Exception e)
            {
                // Other exeption types might be more interesting... 
                // but at the end of the day we just failed to connect and should retry
                myForm.CrawlError("Cognex Connect() Error: " + e.ToString());
                return false;
            }
        }

        public int Close()
        {
            myForm.Crawl("Close Cognex sensor " + myIP + "...");
            try
            {
                // Release the socket.  
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;
                return 0;
            }
            catch (ObjectDisposedException)
            {
                // This just means the socket was closed before we got here.
                // Claim victory and return 0
                socket = null;
                return 0;
            }
            catch (Exception e)
            {
                // If something strange happens while closing, we'd like to know about it.
                // ... but call the socket null and continue with the reinit
                myForm.CrawlError("Cognex Close() error: " + e.ToString());
                socket = null;
                return 0;
            }
        }

        public bool Send(string msg)
        {
            try
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(msg);
                // Send data to the remote device.  
                while (socket.Available > 0) Receive();
                socket.Send(byteData);
                return true;
            }
            catch (Exception e)
            {
                myForm.CrawlError("Cognex Send() error: " + e.ToString());
                return false;
            }
        }

        public string Receive()
        {

            // Connect to a remote device.  
            try
            {
                // Receive the response from the remote device.  
                byte[] byteData = new byte[BufferSize];
                socket.Receive(byteData);
                string response = Encoding.ASCII.GetString(byteData);


                // Write the response to the console.  
                //myForm.Crawl("Cognex Response received: " + response);
                return response;

            }
            catch (SocketException)
            {
                //myForm.CrawlError(e.ToString());
                // This just means the Socket.Receive timed out... 
                // so lets give a less verbose explanation for the logs
                myForm.CrawlError("Cognex socket Receive() timeout");
                return null;
            }
            catch (Exception e)
            {
                myForm.CrawlError(e.ToString());
                return null;
            }
        }
    }
}
