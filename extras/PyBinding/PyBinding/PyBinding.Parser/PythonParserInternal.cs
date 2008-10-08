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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

using PyBinding.Parser.Dom;

namespace PyBinding.Parser
{
	internal class PythonParserInternal
	{
		bool          m_Initialized    = false;
		int           m_Port           = 9987;
		bool          m_ProcessSuccess = false;
		Process       m_Process;

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
			var uri = String.Format ("http://127.0.0.1:{0}/", m_Port);
			var r = WebRequest.Create (uri);
			r.Method = "POST";
			r.ContentLength = content.Length;
			r.ContentType = "text/plain";
			var s = r.GetRequestStream ();
			var b = Encoding.ASCII.GetBytes (content);
			s.Write (b, 0, b.Length);
			s.Close ();

			var rs = r.GetResponse ();

			Stream rss = rs.GetResponseStream ();
			int read = 0;
			byte[] buffer = new byte[1024];
			MemoryStream ms = new MemoryStream ();

			while (0 < (read = rss.Read (buffer, 0, buffer.Length)))
				ms.Write (buffer, 0, read);

			ms.Seek (0, SeekOrigin.Begin);

			return new XmlTextReader (ms);
		}

		void Initialize ()
		{
			Assembly asm = Assembly.GetExecutingAssembly ();
			Stream   src = asm.GetManifestResourceStream ("completion.py");

			if (src == null)
				throw new InvalidOperationException ("Missing completion.py");

			m_Process = new Process ();
			m_Process.StartInfo.FileName = PythonHelper.Which ("python2.5");
			m_Process.StartInfo.Arguments = "-u -";
			m_Process.StartInfo.UseShellExecute = false;
			m_Process.StartInfo.RedirectStandardError = true;
			m_Process.StartInfo.RedirectStandardInput = true;
			m_Process.StartInfo.RedirectStandardOutput = true;
			m_Process.OutputDataReceived += delegate (object o, DataReceivedEventArgs e) {
				if (!m_ProcessSuccess) {
					if (e.Data.Trim ().StartsWith ("Listening on port ")) {
						m_Port = Int32.Parse (e.Data.Substring (18).Trim ());
						m_ProcessSuccess = true;
					}
				}
			};

			if (!m_Process.Start ())
				throw new InvalidOperationException ("Failed process");

			/* write our parsing module to standard input */
			using (TextReader reader = new StreamReader (src))
			{
				string line = String.Empty;
				while (null != (line = reader.ReadLine ()))
					m_Process.StandardInput.WriteLine (line);
				m_Process.StandardInput.Flush ();
				m_Process.StandardInput.Close ();
			}

			/* start reading so we can get our dynamic port */
			m_Process.BeginOutputReadLine ();

			m_Initialized = true;
		}
	}
}
