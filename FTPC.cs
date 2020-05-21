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
	public class FtpOperation : IDisposable
	{
		//https://stackoverflow.com/questions/2321097/how-to-send-arbitrary-ftp-commands-in-c-sharp
		LogWriter Log;
		TcpClient tcpClient = new TcpClient();

		public FtpOperation(LogWriter Log)
		{
			this.Log = Log;
		}

		/// <summary>
		/// Connect to a FTP server
		/// </summary>
		/// <param name="serverName">Destination host name</param>
		/// <param name="port">Destination port</param>
		/// <returns>Welcome message of the server</returns>
		public string Connect(string serverName, int port)
		{
			Log.WriteLine("FTPC: connect to {0}", serverName);
			string resp;
			tcpClient.Connect(serverName, port);
			Log.WriteLine("FTPC: connected to {0}", serverName);
			resp = ReceiveResponse(tcpClient.GetStream());
			Flush(tcpClient);
			Log.WriteLine("FTPC: successful connect to {0}", serverName);
			return resp;
		}

		/// <summary>
		/// Send a FTP command to server
		/// </summary>
		/// <param name="command">The command</param>
		/// <returns>Server's response</returns>
		public string SendFtpCommand(string command)
		{
			return TransmitCommand(tcpClient, command);
		}

		/// <summary>
		/// Transmit a FTP command to server
		/// </summary>
		/// <param name="tcpClient">Link with the server</param>
		/// <param name="cmd">The command (without line feed on the end)</param>
		/// <returns>Server's response</returns>
		private string TransmitCommand(TcpClient tcpClient, string cmd)
		{
			var networkStream = tcpClient.GetStream();
			if (!networkStream.CanWrite || !networkStream.CanRead)
				return string.Empty;

			var sendBytes = Encoding.ASCII.GetBytes(cmd + "\r\n");
			networkStream.Write(sendBytes, 0, sendBytes.Length);
			
			return ReceiveResponse(networkStream);
		}

		/// <summary>
		/// Receive a line from response buffer
		/// </summary>
		/// <param name="networkStream"></param>
		/// <returns></returns>
		private string ReceiveResponseLine(NetworkStream networkStream)
		{
			if (!networkStream.CanWrite || !networkStream.CanRead)
				return string.Empty;

			var streamReader = new StreamReader(networkStream);
			string resp = streamReader.ReadLine() + "\r\n";
			if (resp == "\r\n") resp = string.Empty;
			Flush(tcpClient);
			return resp;
		}

		/// <summary>
		/// Get last full response from the remote server
		/// </summary>
		/// <param name="networkStream">Stream from the server</param>
		/// <returns>The server's response</returns>
		public string ReceiveResponse(NetworkStream networkStream)
		{
			string resp = string.Empty;
			string lastResp = ReceiveResponseLine(networkStream);
			try
			{
				while (lastResp != string.Empty)
				{
					resp += lastResp;
					lastResp = ReceiveResponseLine(networkStream);
				}
			}
			catch { /*Stop at end of the buffer*/ }

			return resp;
		}

		private string Flush(TcpClient tcpClient)
		{
			try
			{
				var networkStream = tcpClient.GetStream();
				if (!networkStream.CanWrite || !networkStream.CanRead)
					return string.Empty;

				var receiveBytes = new byte[tcpClient.ReceiveBufferSize];
				networkStream.ReadTimeout = 500;
				networkStream.Read(receiveBytes, 0, tcpClient.ReceiveBufferSize);

				return Encoding.ASCII.GetString(receiveBytes).Trim('\0');
			}
			catch
			{
				// Ignore all irrelevant exceptions
			}

			return string.Empty;
		}

		public void Dispose()
		{
			if (tcpClient.Connected)
				tcpClient.Close();
		}
	}
}
