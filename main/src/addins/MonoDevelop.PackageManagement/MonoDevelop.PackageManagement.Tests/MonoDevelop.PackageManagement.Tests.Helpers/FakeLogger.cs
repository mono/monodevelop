//
// FakeLogger.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeLogger : ILogger
	{
		public List<string> FormattedMessagesLogged = new List<string> ();

		public string FirstFormattedMessageLogged {
			get { return FormattedMessagesLogged [0]; }
		}

		public string LastFormattedMessageLogged {
			get {
				int index = FormattedMessagesLogged.Count - 1;
				return FormattedMessagesLogged [index];
			}
		}

		public string NextToLastFormattedMessageLogged {
			get {
				int index = FormattedMessagesLogged.Count - 2;
				return FormattedMessagesLogged [index];
			}
		}

		public string SecondFormattedMessageLogged {
			get { return FormattedMessagesLogged [1]; }
		}

		public void Log (MessageLevel level, string message, params object[] args)
		{
			string formattedMessage = String.Format (message, args);
			FormattedMessagesLogged.Add (formattedMessage);
		}

		public bool FormattedMessagesLoggedContainsText (string text)
		{
			return FormattedMessagesLogged.Any (message => message.Contains (text));
		}

		public FileConflictResolution ResolveFileConflict (string message)
		{
			throw new NotImplementedException ();
		}
	}
}

