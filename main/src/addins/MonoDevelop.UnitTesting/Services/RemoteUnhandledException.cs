//
// RemoteUnhandledException.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.UnitTesting
{

	/// <summary>
	/// Exception class that can be serialized
	/// </summary>
	public class RemoteUnhandledException: Exception
	{
		string stack;

		public RemoteUnhandledException (string exceptionName, string message, string stack): base (message)
		{
			RemoteExceptionName = exceptionName;
			this.stack = stack;
		}

		public RemoteUnhandledException (Exception ex): base (ex.Message)
		{
			RemoteExceptionName = ex.GetType().Name;
			this.stack = ex.StackTrace;
		}

		public string Serialize ()
		{
			return RemoteExceptionName + "\n" + Message.Replace ('\r',' ').Replace ('\n',' ') + "\n" + StackTrace;
		}

		public static RemoteUnhandledException Parse (string s)
		{
			int i = s.IndexOf ('\n');
			string name = s.Substring (0, i++);
			int i2 = s.IndexOf ('\n', i);
			return new RemoteUnhandledException (name, s.Substring (i, i2 - i), s.Substring (i2 + 1));
		}

		public string RemoteExceptionName { get; set; }

		public override string StackTrace {
			get {
				return stack;
			}
		}
	}
}
