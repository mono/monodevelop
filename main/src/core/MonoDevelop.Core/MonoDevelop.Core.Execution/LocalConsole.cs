// 
// SimpleConsole.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MonoDevelop.Core.Execution
{
	/// <summary>
	/// This is an implementation of the IConsole interface which allows reading
	/// the output generated from a process, and writing its input.
	/// </summary>
	public class LocalConsole: OperationConsole
	{
		InternalWriter cin;
		InternalWriter cout;
		InternalWriter cerror;
		InternalWriter clog;
		
		public LocalConsole ()
		{
			cout = new InternalWriter ();
			cerror = new InternalWriter ();
			clog = new InternalWriter ();
			cin = new InternalWriter ();
		}
		
		public override void Dispose ()
		{
			cout.Dispose ();
			cerror.Dispose ();
			clog.Dispose ();
			cin.Dispose ();
			base.Dispose ();
		}

		/// <summary>
		/// Flushes and closes the readers and writers
		/// </summary>
		public void SetDone ()
		{
			cout.SetDone ();
			cerror.SetDone ();
			clog.SetDone ();
			cin.SetDone ();
		}
		
		/// <summary>
		/// This writer can be used to provide the input of the console.
		/// </summary>
		public TextWriter InWriter {
			get { return cin; }
		}
		
		/// <summary>
		/// Output of the process.
		/// </summary>
		public TextReader OutReader {
			get {
				return cout.DataReader;
			}
		}
		
		/// <summary>
		/// Error log of the process
		/// </summary>
		public TextReader ErrorReader {
			get {
				return cerror.DataReader;
			}
		}
		
		/// <summary>
		/// Log of the process
		/// </summary>
		public TextReader LogReader {
			get {
				return clog.DataReader;
			}
		}
		
		public override TextReader In {
			get {
				return cin.DataReader;
			}
		}
		
		public override TextWriter Out {
			get {
				return cout;
			}
		}
		
		public override TextWriter Error {
			get {
				return cerror;
			}
		}
		
		public override TextWriter Log {
			get {
				return clog;
			}
		}
	}
	
	class InternalReader: TextReader
	{
		Queue<string> queue = new Queue<string> ();
		string current;
		int idx;
		bool disposed;
		bool done;
		
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			lock (queue) {
				queue.Clear ();
				current = null;
				disposed = true;
				done = true;
				Monitor.PulseAll (queue);
			}
		}

		
		internal void PushString (string s)
		{
			lock (queue) {
				if (disposed || string.IsNullOrEmpty (s))
					return;
				queue.Enqueue (s);
				Monitor.PulseAll (queue);
			}
		}
		
		internal void SetDone ()
		{
			lock (queue) {
				done = true;
				Monitor.PulseAll (queue);
			}
		}
		
		bool LoadCurrent (bool block)
		{
			if (current != null && idx < current.Length)
				return true;
			
			lock (queue) {
				while (queue.Count == 0 && !done) {
					if (block)
						Monitor.Wait (queue);
					else
						return false;
				}
				if (queue.Count == 0)
					return false;
				current = queue.Dequeue ();
				idx = 0;
			}
			return true;
		}
		
		public override int Peek ()
		{
			if (LoadCurrent (true))
				return (int) current [idx];
			else
				return -1;
		}
		
		public override int Read ()
		{
			if (LoadCurrent (true))
				return current [idx];
			else
				return -1;
		}

		public override int Read (char[] buffer, int index, int count)
		{
			int nread = 0;
			while (count > 0 && LoadCurrent (nread == 0)) {
				int len = Math.Min (current.Length - idx, count);
				current.CopyTo (idx, buffer, index, len);
				index += len;
				idx += len;
				count -= len;
				nread += len;
			}
			return nread;
		}

		public override string ReadLine ()
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			while (LoadCurrent (true)) {
				for (int i=idx; i < current.Length; i++) {
					if (current[i] == '\n') {
						idx = i + 1;
						sb.Append (current, 0, i);
						return StringBuilderCache.ReturnAndFree (sb);
					}
					if (current[i] == '\r') {
						idx = i + 1;
						sb.Append (current, 0, i);
						if (LoadCurrent (true) && current [idx] == '\n')
							idx++;
						return StringBuilderCache.ReturnAndFree (sb);
					}
				}
				sb.Append (current, idx, current.Length - idx);
				current = null;
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}

		public override string ReadToEnd ()
		{
			StringBuilder sb = StringBuilderCache.Allocate ();
			while (LoadCurrent (true)) {
				sb.Append (current, idx, current.Length - idx);
				current = null;
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}
	}
	
	class InternalWriter: TextWriter
	{
		InternalReader data = new InternalReader ();
		
		public InternalReader DataReader {
			get { return data; }
		}
		
		public override Encoding Encoding {
			get {
				return Encoding.UTF8;
			}
		}

		public void SetDone ()
		{
			data.SetDone ();
		}

		public override void Write (char value)
		{
			data.PushString (value.ToString ());
		}
		
		public override void Write (string value)
		{
			data.PushString (value);
		}

		public override void Write (char[] buffer, int index, int count)
		{
			data.PushString (new string (buffer, index, count));
		}
		
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			data.SetDone ();
		}
	}
}
