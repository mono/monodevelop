//
// XspBrowserLauncherConsole.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc.
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
using MonoDevelop.Core.Execution;

namespace MonoDevelop.AspNet.Execution
{
	class XspBrowserLauncherConsole : OperationConsole
	{
		readonly OperationConsole real;
		LineInterceptingTextWriter outWriter;
		Action <string> launchBrowser;
		IDisposable cancelReg;
		
		const int MAX_WATCHED_LINES = 30;
		
		public XspBrowserLauncherConsole (OperationConsole real, Action <string> launchBrowser)
		{
			this.real = real;
			this.launchBrowser = launchBrowser;
			cancelReg = real.CancellationToken.Register (CancellationSource.Cancel);
		}
		
		public override void Dispose ()
		{
			cancelReg.Dispose ();
			real.Dispose ();
		}

		public override TextReader In {
			get { return real.In; }
		}
		
		public override TextWriter Out {
			get {
				if (outWriter == null)
					outWriter = new LineInterceptingTextWriter (real.Out, delegate {
						string line = outWriter.GetLine();
						if (line.Contains ("Listening on port: ")) {
							string port = System.Text.RegularExpressions.Regex.Match (line, "(?<=port: )[0-9]*(?= )").Value;
							launchBrowser (port);
							outWriter.FinishedIntercepting = true;
						} else if (outWriter.LineCount > MAX_WATCHED_LINES) {
							outWriter.FinishedIntercepting = true;
						}
					});
				return outWriter;
			}
		}
		
		public override TextWriter Error {
			get { return real.Error; }
		}
		
		public override TextWriter Log {
			get { return real.Log; }
		}
	}
}
