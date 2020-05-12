using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebOne
{
	/// <summary>
	/// FTP Proxy traffic transit
	/// </summary>
    public class FTPTransit
	{
		public TcpClient Client;
		NetworkStream ClientStream = null;
		EndPoint ClientIP = null;
		LogWriter Logger = new LogWriter();
		FtpOperation FtpClient = null;

		public FTPTransit(TcpClient TcpClient)
		{
			Client = TcpClient;
		}


		//http://www.ciscolab.ru/security/page,3,36-maloizvestnye-podrobnosti-raboty-nat.html

		/// <summary>
		/// Begin an FTP transit process
		/// </summary>
		public void Process()
		{
			ClientIP = Client.Client.RemoteEndPoint;
			Logger.WriteLine("FTP {0}: Incoming connection.", ClientIP);
			try
			{
				ClientStream = Client.GetStream();
				ReturnString("220 WebOne FTP Proxy Server ({0})\n",Environment.OSVersion.Platform);
				ReturnString("220 Please enter remote server name and your name:\n");
				ReturnString("220 ftp.example.com anonymous\n");

				byte[] DataBuffer = new byte[64];
				while (true)
				{
					//get the request string
					StringBuilder RequestBuilder = new StringBuilder();
					int Bytes = 0;
					do
					{
						Bytes = ClientStream.Read(DataBuffer, 0, DataBuffer.Length);
						RequestBuilder.Append(Encoding.ASCII.GetString(DataBuffer, 0, Bytes));
					}
					while (ClientStream.DataAvailable);

					string Request = RequestBuilder.ToString();
					Logger.WriteLine("FTP {0}: {1}.", ClientIP, Request);

					if (FtpClient == null) FtpClient = new FtpOperation("mirror.yandex.ru", Logger);
					//undone: here will be processing of FTP commands

					ReturnString("220 I'm sorry, but the FTP Proxy is not implemented yet.\n");
				}
			}
			catch (IOException)
			{
				Logger.WriteLine("FTP {0}: Connection lost.", ClientIP);
			}
			catch (Exception ex)
			{
				Logger.WriteLine("FTP {0}: Error: {1}.", ClientIP, ex.Message);
			}
			finally
			{
				if (ClientStream != null)
					ClientStream.Close();
				if (Client != null)
					Client.Close();
			}
		}

		/// <summary>
		/// Write a composite string to remote client.
		/// </summary>
		/// <param name="Response">A composite format string.</param>
		/// <param name="Args">An array of objects to write using format.</param>
		private void ReturnString(string Response, params object[] Args)
		{
			byte[] data = Encoding.ASCII.GetBytes(string.Format(Response, Args));
			ClientStream.Write(data, 0, data.Length);
		}
	}
}
