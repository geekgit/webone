using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebOne
{
    /// <summary>
    /// FTP control stream operation client
    /// </summary>
    public class FtpOperation
    {
        LogWriter Log;
        TcpClient Client;
        NetworkStream ClientStream;

        public FtpOperation(string ServerName, LogWriter Log, Action<string> ResponseHandler, Stream userClientStream)
        {
            this.Log = Log;
            Log.WriteLine("Connecting: {0}", ServerName);

            Client = new TcpClient(ServerName, 21);
            ClientStream = Client.GetStream();

            //SendString("USER " + Username + "\n" + "PASS" + Password + "\n", ResponseHandler);
            //SendString("USER anonymous\r\nPASS test\r\n", ResponseHandler);

            /*"200 WebOne: Connection to " + ServerName + " established.\r\n"*/
            ReadResponse(ResponseHandler,  userClientStream);
        }

        public void ReadResponse(Action<string> ResponseHandler,  Stream userClientStream = null)
        {
            new Task(() =>
            {
                if (userClientStream != null)
                {
                    try
                    {
                        ClientStream.CopyTo(userClientStream);
                    }
                    catch (IOException)
                    {
                        Log.WriteLine("FTP <!<!<! Connection to the server lost.");
                    }
                }
            }).Start();
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Send a string to remote server.
        /// </summary>
        /// <param name="Request">The string.</param>
        /// <param name="ResponseHandler">Handler for server response.</param>
        public void SendString(string Request, Action<string> ResponseHandler, Stream userClientStream)
        {
            byte[] data = Encoding.ASCII.GetBytes(Request);
            ClientStream.Write(data, 0, data.Length);
            ReadResponse(ResponseHandler, userClientStream);
        }
    }
}
