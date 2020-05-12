using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebOne
{
	/// <summary>
	/// FTP Listener and Server
	/// </summary>
	class FTPServer
    {
		int Port = 21;
		TcpListener Listener;

		/// <summary>
		/// Start a new FTP Listener
		/// </summary>
		/// <param name="port">TCP port number</param>
		public FTPServer(int port)
		{
			new Task(() =>
			{
				Console.WriteLine("Starting FTP server...");
				Port = port;
				Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
				Listener.Start();
				Console.WriteLine("Listening for FTP on port {0}.", port);

				while (true)
				{
					TcpClient client = Listener.AcceptTcpClient();
					FTPTransit clientObject = new FTPTransit(client);
					Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
					clientThread.Start();
				}
			}).Start();
			//undone: add exception throwing out the task
		}
	}
}
