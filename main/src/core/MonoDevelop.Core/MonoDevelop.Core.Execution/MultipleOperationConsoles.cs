//
// MultipleOperationConsoles.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MonoDevelop.Core.Execution
{
	public class MultipleOperationConsoles : OperationConsole
	{
		readonly OperationConsole [] consoles;
		readonly TextWriter OutWriter;
		readonly TextWriter ErrorWriter;
		readonly TextWriter LogWriter;

		public MultipleOperationConsoles (params OperationConsole [] consoles)
		{
			if (consoles == null)
				throw new ArgumentNullException (nameof (consoles));
			if (consoles.Length == 0)
				throw new ArgumentOutOfRangeException (nameof (consoles));

			this.consoles = consoles;
			this.ErrorWriter = new MultipleTextWriters (consoles.Select (c => c.Error).ToArray ());
			this.OutWriter = new MultipleTextWriters (consoles.Select (c => c.Out).ToArray ());
			this.LogWriter = new MultipleTextWriters (consoles.Select (c => c.Log).ToArray ());
		}

		public override void Debug (int level, string category, string message)
		{
			foreach (var console in consoles) {
				console.Debug (level, category, message);
			}
		}

		public override TextWriter Error {
			get {
				return ErrorWriter;
			}
		}

		public override TextReader In {
			get {
				return consoles [0].In;
			}
		}

		public override TextWriter Log {
			get {
				return LogWriter;
			}
		}

		public override TextWriter Out {
			get {
				return OutWriter;
			}
		}

		public override void Dispose ()
		{
			foreach (var console in consoles) {
				console.Dispose ();
			}

			base.Dispose ();
		}

		class MultipleTextWriters : TextWriter
		{
			readonly Encoding encoding;
			readonly TextWriter [] writers;

			public MultipleTextWriters (params TextWriter [] writers)
			{
				if (writers == null)
					throw new ArgumentNullException (nameof (writers));
				if (writers.Length == 0)
					throw new ArgumentOutOfRangeException (nameof (writers));

				this.writers = writers;
				encoding = writers [0].Encoding;
			}

			public override Encoding Encoding {
				get {
					return encoding;
				}
			}

			public override void Close ()
			{
				foreach (var writer in writers) {
					writer.Close ();
				}
			}

			protected override void Dispose (bool disposing)
			{
				foreach (var writer in writers) {
					writer.Dispose ();
				}
				base.Dispose (disposing);
			}

			public override void Flush ()
			{
				foreach (var writer in writers) {
					writer.Flush ();
				}
			}

			public override Task FlushAsync ()
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.FlushAsync ());
				}
				return Task.WhenAll (tasks);
			}

			public override IFormatProvider FormatProvider {
				get {
					return writers [0].FormatProvider;
				}
			}

			public override string NewLine {
				get {
					return writers [0].NewLine;
				}
				set {
					foreach (var writer in writers) {
						writer.NewLine = value;
					}
				}
			}

			public override void Write (bool value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (char value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (char [] buffer)
			{
				foreach (var writer in writers) {
					writer.Write (buffer);
				}
			}

			public override void Write (char [] buffer, int index, int count)
			{
				foreach (var writer in writers) {
					writer.Write (buffer, index, count);
				}
			}

			public override void Write (decimal value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (double value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (float value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (int value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (long value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (object value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (string format, object arg0)
			{
				foreach (var writer in writers) {
					writer.Write (format, arg0);
				}
			}

			public override void Write (string format, object arg0, object arg1)
			{
				foreach (var writer in writers) {
					writer.Write (format, arg0, arg1);
				}
			}

			public override void Write (string format, object arg0, object arg1, object arg2)
			{
				foreach (var writer in writers) {
					writer.Write (format, arg0, arg1, arg2);
				}
			}

			public override void Write (string format, params object [] arg)
			{
				foreach (var writer in writers) {
					writer.Write (format, arg);
				}
			}

			public override void Write (string value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (uint value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override void Write (ulong value)
			{
				foreach (var writer in writers) {
					writer.Write (value);
				}
			}

			public override Task WriteAsync (char value)
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteAsync (value));
				}
				return Task.WhenAll (tasks);
			}

			public override Task WriteAsync (char [] buffer, int index, int count)
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteAsync (buffer, index, count));
				}
				return Task.WhenAll (tasks);
			}

			public override Task WriteAsync (string value)
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteAsync (value));
				}
				return Task.WhenAll (tasks);
			}


			public override Task WriteLineAsync ()
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteLineAsync ());
				}
				return Task.WhenAll (tasks);
			}

			public override void WriteLine ()
			{
				foreach (var writer in writers) {
					writer.WriteLine ();
				}
			}

			public override void WriteLine (bool value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (char value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (char [] buffer)
			{
				foreach (var writer in writers) {
					writer.WriteLine (buffer);
				}
			}

			public override void WriteLine (char [] buffer, int index, int count)
			{
				foreach (var writer in writers) {
					writer.WriteLine (buffer, index, count);
				}
			}

			public override void WriteLine (decimal value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (double value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (float value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (int value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (long value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (object value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (string format, object arg0)
			{
				foreach (var writer in writers) {
					writer.WriteLine (format, arg0);
				}
			}

			public override void WriteLine (string format, object arg0, object arg1)
			{
				foreach (var writer in writers) {
					writer.WriteLine (format, arg0, arg1);
				}
			}

			public override void WriteLine (string format, object arg0, object arg1, object arg2)
			{
				foreach (var writer in writers) {
					writer.WriteLine (format, arg0, arg1, arg2);
				}
			}

			public override void WriteLine (string format, params object [] arg)
			{
				foreach (var writer in writers) {
					writer.WriteLine (format, arg);
				}
			}

			public override void WriteLine (string value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (uint value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override void WriteLine (ulong value)
			{
				foreach (var writer in writers) {
					writer.WriteLine (value);
				}
			}

			public override Task WriteLineAsync (char [] buffer, int index, int count)
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteLineAsync (buffer, index, count));
				}
				return Task.WhenAll (tasks);
			}

			public override Task WriteLineAsync (char value)
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteLineAsync (value));
				}
				return Task.WhenAll (tasks);
			}

			public override Task WriteLineAsync (string value)
			{
				var tasks = new List<Task> (writers.Length);
				foreach (var writer in writers) {
					tasks.Add (writer.WriteLineAsync (value));
				}
				return Task.WhenAll (tasks);
			}
		}
	}
}

