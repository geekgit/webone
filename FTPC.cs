using System;
using System.Collections.Generic;
using System.Text;

namespace WebOne
{
    /// <summary>
    /// FTP control stream operation client
    /// </summary>
    public class FtpOperation
    {
        LogWriter Log;
        public FtpOperation(string ServerName, LogWriter Log)
        {
            this.Log = Log;
            Log.WriteLine("Connecting: {0}", ServerName);
            //undone!
        }
    }
}
