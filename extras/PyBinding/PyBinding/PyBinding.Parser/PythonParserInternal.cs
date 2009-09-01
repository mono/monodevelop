// PythonParserInternal.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

using PyBinding.Parser.Dom;
using PyBinding.Runtime;

namespace PyBinding.Parser
{
	internal class PythonParserInternal: IDisposable
	{
		bool           m_Initialized    = false;
		int            m_Port           = 0;
		bool           m_ProcessSuccess = false;
		Process        m_Process        = null;
		IPythonRuntime m_Runtime        = null;
		object         m_syncRoot       = new object ();
		int            m_sinceCycle     = 0;
		
		public PythonParserInternal (IPythonRuntime runtime)
		{
			if (runtime == null)
				throw new ArgumentNullException ("runtime");
			m_Runtime = runtime;
		}
		
		~PythonParserInternal ()
		{
			Dispose ();
		}
		
		public IPythonRuntime Runtime {
			get { return m_Runtime; }
		}
		
		public void Dispose ()
		{
			if (m_Process != null && !m_Process.HasExited)
				m_Process.Kill ();
			m_Process = null;
		}
		
		public PythonParsedDocument Parse (string fileName, string content)
		{
			if (!m_Initialized)
				Initialize ();

			PythonParsedDocument doc = new PythonParsedDocument (fileName);

			if (m_ProcessSuccess)
				doc.Parse (GetXml (content), content);
			
			return doc;
		}

		XmlTextReader GetXml (string content)
		{
			if (Interlocked.Increment (ref m_sinceCycle) == 100)
				CycleProcess ();
			
			var uri = String.Format ("http://127.0.0.1:{0}/", m_Port);
			var r = WebRequest.Create (uri);
			r.Method = "POST";
			r.ContentLength = content.Length;
			r.ContentType = "text/plain";
			
			using (var s = r.GetRequestStream ()) {
				var b = Encoding.ASCII.GetBytes (content);
				s.Write (b, 0, b.Length);
			}
			
			var ms = new MemoryStream ();
			
			using (var rs = r.GetResponse ()) {
				Stream rss = rs.GetResponseStream ();
				int read = 0;
				byte[] buffer = new byte[1024];
				while (0 < (read = rss.Read (buffer, 0, buffer.Length)))
					ms.Write (buffer, 0, read);
			}
			
			ms.Seek (0, SeekOrigin.Begin);

			return new XmlTextReader (ms);
		}
		
		Process BuildProcess ()
		{
			string pypath;
			
			try {
				pypath = PythonHelper.FindPreferredPython ();
			}
			catch {
				LoggingService.LogError ("Cannot locate python executable. Disabling python parsing.");
				return null;
			}
			
			var process = new Process ();
			process.StartInfo.FileName = pypath;
			process.StartInfo.EnvironmentVariables ["WATCH_PID"] = Process.GetCurrentProcess ().Id.ToString ();
			process.StartInfo.Arguments = "-u -";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			var started = false;
			process.OutputDataReceived += delegate (object o, DataReceivedEventArgs e) {
				if (!started) {
					if (e.Data.Trim ().StartsWith ("Listening on port ")) {
						Monitor.Enter (m_syncRoot);
						m_Port = Int32.Parse (e.Data.Substring (18).Trim ());
						started = true;
						m_ProcessSuccess = true;
						Monitor.Pulse (m_syncRoot);
						Monitor.Exit (m_syncRoot);
						
						Process oldProcess = null;
						
						// Cycle to the new process
						do {
							oldProcess = m_Process;
						} while (Interlocked.CompareExchange<Process> (ref m_Process, process, oldProcess) != oldProcess);
						
						Interlocked.Exchange (ref m_sinceCycle, 0);
						
						// kill old process after 5 seconds
						if (oldProcess != null) {
							GLib.Timeout.Add (5000, delegate {
								oldProcess.Kill ();
								return false;
							});
						}
					}
				}
			};
			
			return process;
		}
		
		void CycleProcess ()
		{
			Assembly asm = Assembly.GetExecutingAssembly ();
			Stream   src = asm.GetManifestResourceStream ("completion.py");

			if (src == null)
				throw new InvalidOperationException ("Missing completion.py");

			Console.WriteLine ("Cycling Python Completion Process");
			
			var process = BuildProcess ();
			process.Start ();
			
			// write completion.py to stdin
			using (TextReader reader = new StreamReader (src))
			{
				string line = String.Empty;
				while (null != (line = reader.ReadLine ()))
					process.StandardInput.WriteLine (line);
				process.StandardInput.Flush ();
				process.StandardInput.Close ();
			}

			// start async read of stdout
			process.BeginOutputReadLine ();
		}
		
		void Initialize ()
		{
			m_Initialized = true;
			CycleProcess ();
			Monitor.Enter (m_syncRoot);
			if (m_Port == 0)
				Monitor.Wait (m_syncRoot);
			Monitor.Exit (m_syncRoot);
		}
	}
}
