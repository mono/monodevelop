//
// ProcessProgressStatus.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Specialized;
using System.IO;

namespace Mono.Addins.Database
{
	internal class ProcessProgressStatus: MarshalByRefObject, IProgressStatus
	{
		bool canceled;
		int logLevel;
		
		public ProcessProgressStatus (int logLevel)
		{
			this.logLevel = logLevel;
		}
		
		public void SetMessage (string msg)
		{
			Console.WriteLine ("process-ps-msg:" + Encode (msg));
		}
		
		public void SetProgress (double progress)
		{
			Console.WriteLine ("process-ps-progress:" + progress.ToString ());
		}
		
		public void Log (string msg)
		{
			if (msg.StartsWith ("plog:"))
				// This is an special type of log that will be provided to the
				// main process in case of a crash in the setup process
				Console.WriteLine ("process-ps-plog:" + Encode (msg.Substring (5)));
			else
				Console.WriteLine ("process-ps-log:" + Encode (msg));
		}
		
		public void ReportWarning (string message)
		{
			Console.WriteLine ("process-ps-warning:" + Encode (message));
		}
		
		public void ReportError (string message, Exception exception)
		{
			if (message == null) message = string.Empty;
			string et;
			if (logLevel > 1)
				et = exception != null ? exception.ToString () : string.Empty;
			else
				et = exception != null ? exception.Message : string.Empty;
			
			Console.WriteLine ("process-ps-exception:" + Encode (et));
			Console.WriteLine ("process-ps-error:" + Encode (message));
		}
		
		public bool IsCanceled {
			get { return canceled; }
		}
		
		public int LogLevel {
			get { return logLevel; }
		}
		
		public void Cancel ()
		{
			canceled = true;
			Console.WriteLine ("process-ps-cancel:");
		}
		
		static string Encode (string msg)
		{
			msg = msg.Replace ("&", "&a");
			return msg.Replace ("\n", "&n");
		}
		
		static string Decode (string msg)
		{
			msg = msg.Replace ("&n", "\n");
			return msg.Replace ("&a", "&");
		}
		
		public static void MonitorProcessStatus (IProgressStatus monitor, TextReader reader, StringCollection progessLog)
		{
			string line;
			string exceptionText = null;
			while ((line = reader.ReadLine ()) != null) {
				int i = line.IndexOf (':');
				if (i != -1) {
					string tag = line.Substring (0, i);
					string txt = line.Substring (i+1);
					bool wasTag = true;
					
					switch (tag) {
						case "process-ps-msg":
							monitor.SetMessage (Decode (txt));
							break;
						case "process-ps-progress":
							monitor.SetProgress (double.Parse (txt));
							break;
						case "process-ps-log":
							monitor.Log (Decode (txt));
							break;
						case "process-ps-warning":
							monitor.ReportWarning (Decode (txt));
							break;
						case "process-ps-exception":
							exceptionText = Decode (txt);
							if (exceptionText == string.Empty)
								exceptionText = null;
							break;
						case "process-ps-error":
							string err = Decode (txt);
							if (err == string.Empty) err = null;
							monitor.ReportError (err, exceptionText != null ? new Exception (exceptionText) : null);
							break;
						case "process-ps-cancel":
							monitor.Cancel ();
							break;
						case "process-ps-plog":
							progessLog.Add (Decode (txt));
							break;
						default:
							wasTag = false;
							break;
					}
					if (wasTag)
						continue;
				}
				Console.WriteLine (line);
			}
		}
	}
}
