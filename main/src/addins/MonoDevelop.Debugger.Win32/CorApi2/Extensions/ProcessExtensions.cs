//
// ProcessExtensions.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Threading;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Samples.Debugging.Extensions
{
	// [Xamarin] Output redirection.
	public class CorTargetOutputEventArgs: EventArgs
	{
		public CorTargetOutputEventArgs (string text, bool isStdError)
		{
			Text = text;
			IsStdError = isStdError;
		}

		public string Text { get; set; }

		public bool IsStdError { get; set; }
	}

	public delegate void CorTargetOutputEventHandler (Object sender, CorTargetOutputEventArgs e);

	public static class CorProcessExtensions
	{
		internal static void TrackStdOutput (this CorProcess proc, SafeFileHandle outputPipe, SafeFileHandle errorPipe)
		{
			var outputReader = new Thread (delegate () {
				proc.ReadOutput (outputPipe, false);
			});
			outputReader.Name = "Debugger output reader";
			outputReader.IsBackground = true;
			outputReader.Start ();

			var errorReader = new Thread (delegate () {
				proc.ReadOutput (errorPipe, true);
			});
			errorReader.Name = "Debugger error reader";
			errorReader.IsBackground = true;
			errorReader.Start ();
		}

		// [Xamarin] Output redirection.
		static void ReadOutput (this CorProcess proc, SafeFileHandle pipe, bool isStdError)
		{
			var buffer = new byte[256];
			int nBytesRead;

			try {
				while (true) {
					if (!DebuggerExtensions.ReadFile (pipe, buffer, buffer.Length, out nBytesRead, IntPtr.Zero) || nBytesRead == 0)
						break; // pipe done - normal exit path.

					string s = System.Text.Encoding.Default.GetString (buffer, 0, nBytesRead);
					if (OnStdOutput != null)
						OnStdOutput (proc, new CorTargetOutputEventArgs (s, isStdError));
				}
			} catch {
			}
		}

		public static void RegisterStdOutput (this CorProcess proc, CorTargetOutputEventHandler handler)
		{
			proc.OnProcessExit += delegate {
				RemoveEventsFor (proc);
			};

			List<CorTargetOutputEventHandler> list;
			if (!events.TryGetValue (proc, out list))
				list = new List<CorTargetOutputEventHandler> ();
			list.Add (handler);

			events [proc] = list;
			OnStdOutput += handler;
		}

		static void RemoveEventsFor (CorProcess proc)
		{
			foreach (CorTargetOutputEventHandler handler in events [proc])
				OnStdOutput -= handler;

			events.Remove (proc);
		}

		// [Xamarin] Output redirection.
		static event CorTargetOutputEventHandler OnStdOutput;
		static readonly Dictionary<CorProcess, List<CorTargetOutputEventHandler>> events = new Dictionary<CorProcess, List<CorTargetOutputEventHandler>> ();
	}
}

