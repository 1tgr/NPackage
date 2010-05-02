using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NPackage.Core.Extensions;

namespace NPackage.Core
{
    public class DownloadWorkflow
    {
        private class DownloadAction
        {
            private readonly Uri uri;
            private readonly string filename;
            private readonly Action continuation;

            public DownloadAction(Uri uri, string filename, Action continuation)
            {
                this.uri = uri;
                this.filename = filename;
                this.continuation = continuation;
            }

            public Uri Uri
            {
                get { return uri; }
            }

            public string Filename
            {
                get { return filename; }
            }

            public Action Continuation
            {
                get { return continuation; }
            }
        }

        private readonly List<DownloadAction> actions = new List<DownloadAction>();

        public void Enqueue(Uri uri, string filename, Action continuation)
        {
            actions.Add(new DownloadAction(uri, filename, continuation));
        }

        public void Run()
        {
            while (actions.Count > 0)
            {
                var actionsByFilenameByUri = actions
                    .GroupBy(a => a.Uri)
                    .Select(g => new
                                     {
                                         Uri = g.Key,
                                         ActionsByFilename = g
                                     .GroupBy(a => a.Filename, StringComparer.InvariantCultureIgnoreCase)
                                     .Select(g2 => new { Filename = g2.Key, Actions = g2.ToArray() })
                                     .ToArray()
                                     })
                    .ToArray();

                actions.Clear();

                foreach (var uriPair in actionsByFilenameByUri)
                {
                    string firstFilename = null;
                    FileInfo firstFileInfo = null;

                    foreach (var filenamePair in uriPair.ActionsByFilename)
                    {
                        FileInfo fileInfo = new FileInfo(filenamePair.Filename);

                        if (firstFilename == null)
                        {
                            RaiseLog("Checking {0}", uriPair.Uri);

                            WebRequest request = WebRequest.Create(uriPair.Uri);
                            using (WebResponse response = request.GetResponse())
                            {
                                HttpWebResponse httpWebResponse = response as HttpWebResponse;
                                if (!fileInfo.Exists || (httpWebResponse != null && httpWebResponse.LastModified > fileInfo.LastWriteTime))
                                {
                                    using (Stream inputStream = response.GetResponseStream())
                                    {
                                        RaiseLog("Downloading from {0} to {1}", uriPair.Uri, filenamePair.Filename);
                                        inputStream.CopyTo(filenamePair.Filename);
                                        fileInfo.Refresh();

                                        if (httpWebResponse != null)
                                            fileInfo.LastWriteTime = httpWebResponse.LastModified;
                                    }
                                }
                            }

                            firstFilename = filenamePair.Filename;
                            firstFileInfo = fileInfo;
                        }
                        else
                        {
                            if (!fileInfo.Exists || firstFileInfo.LastWriteTime > fileInfo.LastWriteTime)
                            {
                                RaiseLog("Copying from {0} to {1}", firstFilename, filenamePair.Filename);
                                File.Copy(firstFilename, filenamePair.Filename, true);
                                fileInfo.LastWriteTime = firstFileInfo.LastWriteTime;
                            }
                        }

                        foreach (DownloadAction action in filenamePair.Actions)
                            action.Continuation();
                    }
                }
            }
        }

        private void RaiseLog(string format, params object[] args)
        {
            EventHandler<LogEventArgs> handler = Log;
            if (handler != null)
                handler(this, new LogEventArgs(string.Format(format, args)));
        }

        public event EventHandler<LogEventArgs> Log;
    }
}