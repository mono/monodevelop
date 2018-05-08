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
using System.Threading;

namespace MonoDevelop.Core.ProgressMonitoring
{
	public delegate void LogTextEventHandler (string writtenText);
	
	public class LogTextWriter: TextWriter
	{
		List<TextWriter> chainedWriters;
		SynchronizationContext context;

		public LogTextWriter () : this (null)
		{

		}

		internal LogTextWriter (SynchronizationContext context)
		{
			this.context = context;
		}
		
		public void ChainWriter (TextWriter writer)
		{
			if (chainedWriters == null)
				chainedWriters = new List<TextWriter> ();
			
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
			if (context != null)
				context.Post ((o) => ((LogTextWriter)o).OnClosed (), this);
			else
				OnClosed ();
		}

		void OnClosed ()
		{
			Closed?.Invoke (this, null);
		}

		public override void Write (char[] buffer, int index, int count)
		{
			if (context != null)
				context.Post ((o) => ((LogTextWriter)o).OnWrite (buffer, index, count), this);
			else
				OnWrite (buffer, index, count);
		}

		void OnWrite (char[] buffer, int index, int count)
		{
			TextWritten?.Invoke (new string (buffer, index, count));

			if (chainedWriters != null)
				foreach (TextWriter cw in chainedWriters)
					cw.Write (buffer, index, count);
		}
		
		public override void Write (char value)
		{
			if (context != null)
				context.Post ((o) => ((LogTextWriter)o).OnWrite (value), this);
			else
				OnWrite (value);
		}

		void OnWrite (char value)
		{
			TextWritten?.Invoke (value.ToString ());

			if (chainedWriters != null)
				foreach (TextWriter cw in chainedWriters)
					cw.Write (value);
		}
		
		public override void Write (string value)
		{
			if (context != null)
				context.Post ((o) => ((LogTextWriter)o).OnWrite (value), this);
			else
				OnWrite (value);
		}

		void OnWrite (string value)
		{
			TextWritten?.Invoke (value);

			if (chainedWriters != null)
				foreach (TextWriter cw in chainedWriters)
					cw.Write (value);
		}

		public override void Flush ()
		{
			if (context != null)
				context.Post ((o) => base.Flush (), null);
			else
				base.Flush ();
		}

		public event LogTextEventHandler TextWritten;
		public event EventHandler Closed;
	}
}
