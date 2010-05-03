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
            private readonly Action<string> continuation;

            public DownloadAction(Uri uri, string filename, Action<string> continuation)
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

            public Action<string> Continuation
            {
                get { return continuation; }
            }
        }

        private readonly List<DownloadAction> actions = new List<DownloadAction>();

        public void Enqueue(Uri uri, string filename, Action<string> continuation)
        {
            actions.Add(new DownloadAction(uri, filename, continuation));
        }

        private static string AddFilename(string pathOrFilename, string filename)
        {
            bool isPath = pathOrFilename.EndsWith(Path.DirectorySeparatorChar.ToString())
                || pathOrFilename.EndsWith(Path.AltDirectorySeparatorChar.ToString());

            return isPath
                ? Path.Combine(pathOrFilename, filename)
                : pathOrFilename;
        }

        public bool Step()
        {
            var actionsByFilenameByUri = actions
                .GroupBy(a => a.Uri)
                .Select(g => new
                                 {
                                     Uri = g.Key,
                                     ActionsByFilename = g
                                 .GroupBy(a => a.Filename, StringComparer.InvariantCultureIgnoreCase)
                                 .Select(g2 => new { Filename = g2.Key, Actions = g2.ToArray() })
                                 .OrderBy(a => a.Filename, StringComparer.InvariantCultureIgnoreCase)
                                 .ToArray()
                                 })
                .OrderBy(a => a.Uri.ToString())
                .ToArray();

            actions.Clear();

            foreach (var uriPair in actionsByFilenameByUri)
            {
                string firstFilename = null;
                FileInfo firstFileInfo = null;
                string responseUriFilename = null;

                foreach (var filenamePair in uriPair.ActionsByFilename)
                {
                    if (firstFilename == null)
                    {
                        string responseFilename;
                        RaiseLog("Checking {0}", uriPair.Uri);

                        WebRequest request = WebRequest.Create(uriPair.Uri);
                        using (WebResponse response = request.GetResponse())
                        {
                            responseUriFilename = Path.GetFileName(response.ResponseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
                          
                            string contentDisposition = response.Headers["Content-Disposition"];
                            if (!string.IsNullOrEmpty(contentDisposition))
                            {
                                string[] parts = contentDisposition.Split(new[] { ';' }, 2);
                                if (parts.Length > 1)
                                {
                                    string part1 = parts[1].TrimStart();
                                    const string prefix = "filename=";
                                    if (part1.StartsWith(prefix))
                                        responseUriFilename = part1.Substring(prefix.Length).Trim();
                                }
                            }

                            responseFilename = AddFilename(filenamePair.Filename, responseUriFilename);
                            FileInfo fileInfo = new FileInfo(responseFilename);

                            HttpWebResponse httpWebResponse = response as HttpWebResponse;
                            if (!fileInfo.Exists || (httpWebResponse != null && httpWebResponse.LastModified > fileInfo.LastWriteTime))
                            {
                                using (Stream inputStream = response.GetResponseStream())
                                {
                                    RaiseLog("Downloading from {0} to {1}", response.ResponseUri, responseFilename);
                                    inputStream.CopyTo(responseFilename);
                                    fileInfo.Refresh();

                                    if (httpWebResponse != null)
                                        fileInfo.LastWriteTime = httpWebResponse.LastModified;
                                }
                            }
                            
                            firstFilename = responseFilename;
                            firstFileInfo = fileInfo;
                        }
                        
                        foreach (DownloadAction action in filenamePair.Actions)
                            action.Continuation(responseFilename);
                    }
                    else
                    {
                        string responseFilename = AddFilename(filenamePair.Filename, responseUriFilename);
                        FileInfo fileInfo = new FileInfo(responseFilename);

                        if (!fileInfo.Exists || firstFileInfo.LastWriteTime > fileInfo.LastWriteTime)
                        {
                            RaiseLog("Copying from {0} to {1}", firstFilename, responseFilename);
                            File.Copy(firstFilename, responseFilename, true);
                            fileInfo.LastWriteTime = firstFileInfo.LastWriteTime;
                        }

                        foreach (DownloadAction action in filenamePair.Actions)
                            action.Continuation(responseFilename);
                    }
                }
            }
            
            return actions.Count > 0;
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