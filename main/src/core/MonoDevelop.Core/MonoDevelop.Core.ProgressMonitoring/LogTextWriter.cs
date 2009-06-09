//
// LogTextWriter.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Core.ProgressMonitoring
{
	public delegate void LogTextEventHandler (string writtenText);
	
	public class LogTextWriter: TextWriter
	{
		List<TextWriter> chainedWriters;
		
		public void ChainWriter (TextWriter writer)
		{
			if (chainedWriters == null) chainedWriters = new List<TextWriter> ();
			chainedWriters.Add (writer);
		}
		
		public void UnchainWriter (TextWriter writer)
		{
			if (chainedWriters != null) {
				chainedWriters.Remove (writer);
				if (chainedWriters.Count == 0)
					chainedWriters = null;
			}
		}
		
		public override Encoding Encoding {
			get { return Encoding.Default; }
		}
		
		public override void Close ()
		{
			if (Closed != null)
				Closed (this, null);
		}

		public override void Write (char[] buffer, int index, int count)
		{
			if (TextWritten != null)
				TextWritten (new string (buffer, index, count));
			if (chainedWriters != null)
				foreach (TextWriter cw in chainedWriters)
					cw.Write (buffer, index, count);
		}
		
		public override void Write (char value)
		{
			if (TextWritten != null)
				TextWritten (value.ToString ());
			if (chainedWriters != null)
				foreach (TextWriter cw in chainedWriters)
					cw.Write (value);
		}
		
		public override void Write (string value)
		{
			if (TextWritten != null)
				TextWritten (value);
			if (chainedWriters != null)
				foreach (TextWriter cw in chainedWriters)
					cw.Write (value);
		}
		
		public event LogTextEventHandler TextWritten;
		public event EventHandler Closed;
	}
}
