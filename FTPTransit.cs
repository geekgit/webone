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

		string ServerName = "localhost";
		string UserName = "anonymous";
		string UserPassword = "webone@github.com";

		public FTPTransit(TcpClient TcpClient)
		{
			Client = TcpClient;
		}


		/* About FTP ALG (on Russian):
		 * http://www.ciscolab.ru/security/page,3,36-maloizvestnye-podrobnosti-raboty-nat.html
		 * 
		 * Chat: (220, USER, 331, PASS, 230...421 :) )
		 * Client: PORT 192,16,0,101,4,211
		 * Server: 200 PORT command successful. Consider using PASV.
		 * Client: RETR file.zip
		 * Server: 150 Opening BINARY mode data connection for file.zip (1334109 bytes).
		 * 
		 * 192,16,0,101,4,211 -> IP:[(4*256) + 211 = 1235] -> 192.168.0.101:1235
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
				string WelcomeMessage = 
					"220 WebOne FTP Proxy Server.\r\n" +
					"220 Please enter  remote server name  or  user and server name:\r\n" +
					"220     ftp.example.com     or     user@ftp.example.com\r\n";
				ReturnString(WelcomeMessage);

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

					//extract command name and arguments
					string CmdName = Request;
					string CmdArgs = "";
					if (Request.Contains(" ")) CmdName = Request.Substring(0, Request.IndexOf(" "));
					if (CmdName.Contains("\r\n")) CmdName = CmdName.Substring(0, CmdName.Length - 2);
					if (Request.Contains(" ")) CmdArgs = Request.Substring(Request.IndexOf(" ") + 1);
					if (Request.Contains(" ")) CmdArgs = CmdArgs.Substring(0, CmdArgs.Length - 2);

					//parse commands
					switch (CmdName)
					{
						case "USER":
							if (FtpClient == null)
							{
								if(CmdArgs.ToLower() == "anonymous" || CmdArgs.Length < 1)
								{
									ReturnString("430 WebOne: Please use this syntax: USER username@ftp.example.com!\r\n");
									DataBuffer = new byte[64];
									continue;

								}
								if (CmdArgs.Contains("@"))
								{
									int delimeter = CmdArgs.IndexOf("@");
									ServerName = CmdArgs.Substring(delimeter + 1);
									UserName = CmdArgs.Substring(0, delimeter);
								}
								else ServerName = CmdArgs;
								ReturnString("331 WebOne: Please specify the password for '{0}' on '{1}'.\r\n", UserName, ServerName);
							}
							else
								ReturnString(FtpClient.SendFtpCommand(CmdName + " " + CmdArgs));
							DataBuffer = new byte[64];
							continue;
						case "PASS":
							UserPassword = CmdArgs.Substring(0, CmdArgs.Length - 2);
							if (FtpClient == null)
							{
								UserPassword = CmdArgs;
								FtpClient = new FtpOperation(Logger);
								string str = "211 WebOne: Connection successful.\r\n" + FtpClient.Connect(ServerName, 21) + "211 WebOne: Will use {0}/{1} account.\r\n";
								str += FtpClient.SendFtpCommand("USER " + UserName);
								str += FtpClient.SendFtpCommand("PASS " + UserPassword);
								ReturnString(str,UserName,UserPassword);
							}
							else
								ReturnString(FtpClient.SendFtpCommand(CmdName + " " + CmdArgs));
							DataBuffer = new byte[64];
							continue;
						case "PORT":
							ReturnString("425 WebOne: Active to Passive converting is not currently implemented.\r\n");
							continue;
						default:
							if (FtpClient != null)
								ReturnString(FtpClient.SendFtpCommand(CmdName + " " + CmdArgs));
							else ReturnString("530 WebOne: No uplink connection.\r\n");
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

				try
				{
					if (ClientStream != null)
					{ ReturnString("421 WebOne: An error occured: {0}.\r\n", ex.Message); }
				}
				catch { }
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
			ClientStream.Flush();
		}
	}
}
