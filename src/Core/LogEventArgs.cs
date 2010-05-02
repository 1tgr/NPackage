using System;

namespace NPackage.Core
{
    public class LogEventArgs : EventArgs
    {
        private readonly string message;

        public LogEventArgs(string message)
        {
            this.message = message;
        }

        public string Message
        {
            get { return message; }
        }
    }
}