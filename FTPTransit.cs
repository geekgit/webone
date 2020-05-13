using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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

		string UserName = "";
		string Password = "";

		public FTPTransit(TcpClient TcpClient)
		{
			Client = TcpClient;
		}


		//http://www.ciscolab.ru/security/page,3,36-maloizvestnye-podrobnosti-raboty-nat.html
		/*
		220-Welcome to Yandex Mirror FTP service. Your served by: mirror01vla.mds.yandex
		.net
		220
		USER anonymous
		331 Please specify the password.
		PASS test
		530 Login incorrect.
		USER anonymous
		331 Please specify the password.
		PASS test
		230 Login successful.
		421 Timeout.


		Подключение к узлу утеряно.

		*/

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
				ReturnString("220 WebOne FTP Proxy Server ({0})\r\n",Environment.OSVersion.Platform);
				ReturnString("220 Please enter remote server name:\r\n");
				//ReturnString("220 ftp.example.com anonymous\n");

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
					if (Request == "") { DataBuffer = new byte[64]; continue; }
					Logger.WriteLine("FTP {0}: {1}.", ClientIP, Request);

					string CmdName = Request;
					string CmdArgs = "";
					if (Request.Contains(" ")) CmdName = Request.Substring(0, Request.IndexOf(" "));
					if (Request.Contains(" ")) CmdArgs = Request.Substring(Request.IndexOf(" ") + 1);
					switch (CmdName)
					{
						case "USER":
							if (FtpClient == null)
							{
								UserName = CmdArgs.Substring(0, CmdArgs.Length - 2);
								ReturnString("331 WebOne: Please specify the password (any).\r\n");
							}
							else
								FtpClient.SendString(Request, ProcessResponse, ClientStream);
							DataBuffer = new byte[64];
							continue;
						case "PASS":
							Password = CmdArgs.Substring(0, CmdArgs.Length - 2);
							if (FtpClient == null)
							{
								FtpClient = new FtpOperation(UserName, Logger, ProcessResponse, ClientStream);
							}
							else
								FtpClient.SendString(Request, ProcessResponse, ClientStream);
							DataBuffer = new byte[64];
							continue;
						case "PORT":
							ReturnString("425 WebOne: Active to Passive converting is not currently implemented.\r\n");
							continue;
						default:
							if(FtpClient!=null)
							FtpClient.SendString(Request, ProcessResponse, ClientStream);
							DataBuffer = new byte[64];
							continue;
					}
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

		/// <summary>
		/// Process remote server's response.
		/// </summary>
		/// <param name="Response">The response from control stream.</param>
		private void ProcessResponse(string Response)
		{
			if (Response == "") return;
			Logger.WriteLine("FTP {0}: {1}.", "  < < <  ", Response);
			ReturnString("{0}\r\n", Response);
		}
	}
}
