//
// AssertLoggingTraceListener.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
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
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;

namespace MonoDevelop.Core.Logging
{
	class AssertLoggingTraceListener : DefaultTraceListener
	{
		public override void Fail (string message, string detailMessage)
		{
			var frames = new StackTrace (1, true).GetFrames ();

			//find the stack frame that actually called into the trace system
			int callerFrame = 0;
			for (; (callerFrame < frames.Length && IsInfrastructureMethod (frames [callerFrame])); callerFrame++)
				continue;
			if (callerFrame == frames.Length - 1)
				callerFrame = 0;

			var sb = StringBuilderCache.Allocate();
			if (IsRealMessage (message)) {
				if (!string.IsNullOrEmpty (detailMessage)) {
					sb.AppendFormat ("Failed assertion: {0} - {1}", message, detailMessage);
				} else {
					sb.AppendFormat ("Failed assertion: {0}", message);
				}
			} else {
				sb.Append ("Failed assertion at ");
				FormatStackFrame (sb, frames [callerFrame]);
				callerFrame++;
			}

			sb.Append ("\n");
			FormatStackTrace (sb, frames, callerFrame);

			LoggingService.LogError (StringBuilderCache.ReturnAndFree(sb));
		}

		static bool IsRealMessage (string message)
		{
			if (string.IsNullOrEmpty (message))
				return false;
			//HACK: if the message is empty, Mono replaces it with a stacktrace. Ignore it, we can do better.
			if (message.StartsWith ("   at System.Diagnostics.TraceImpl.Assert", StringComparison.Ordinal))
				return false;
			return true;
		}

		static readonly string mscorlibName = typeof (int).Assembly.FullName;
		static readonly string systemName = typeof (TraceListener).Assembly.FullName;

		static bool IsInfrastructureMethod (StackFrame frame)
		{
			var method = frame.GetMethod ();
			if (method == null)
				return true;
			var asmName = method.DeclaringType.Assembly.FullName;
			return asmName == mscorlibName || asmName == systemName;
		}

		//based on Mono's StackTrace.ToString()
		static void FormatStackTrace (StringBuilder sb, StackFrame[] frames, int startIndex = 0)
		{
			for (int i = startIndex; i < frames.Length; i++) {
				var frame = frames [i];
				if (i > startIndex)
					sb.Append ("\n");
				sb.Append ("   at ");
				FormatStackFrame (sb, frame);
			}
		}

		static void FormatStackFrame (StringBuilder sb, StackFrame frame)
		{
			MethodBase method = frame.GetMethod ();
			if (method != null) {
				// Method information available
				sb.AppendFormat ("{0}.{1}", method.DeclaringType.FullName, method.Name);
				/* Append parameter information */sb.Append ("(");
				ParameterInfo[] p = method.GetParameters ();
				for (int j = 0; j < p.Length; ++j) {
					if (j > 0)
						sb.Append (", ");
					Type pt = p [j].ParameterType;
					bool byref = pt.IsByRef;
					if (byref)
						pt = pt.GetElementType ();
					if (pt.IsClass && pt.Namespace != String.Empty) {
						sb.Append (pt.Namespace);
						sb.Append (".");
					}
					sb.Append (pt.Name);
					if (byref)
						sb.Append (" ByRef");
					sb.AppendFormat (" {0}", p [j].Name);
				}
				sb.Append (")");
			}
			else {
				// Method information not available
				sb.Append ("<unknown method>");
			}
			// we were asked for debugging informations
			// but that doesn't mean we have the debug information available
			string fname = frame.GetFileName ();
			if (!string.IsNullOrEmpty (fname) && fname != "<filename unknown>")
				sb.AppendFormat (" in {0}:line {1}", fname, frame.GetFileLineNumber ());
		}
	}
}
