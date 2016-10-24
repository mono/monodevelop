//
// DotNetCoreOperationConsole.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using System.IO;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	class DotNetCoreOperationConsole : OperationConsole
	{
		IPackageManagementEvents packageEvents;
		StringReader reader = new StringReader ("");
		LogTextWriter logWriter = new LogTextWriter ();
		LogTextWriter outWriter = new LogTextWriter ();
		LogTextWriter errorWriter = new LogTextWriter ();

		public DotNetCoreOperationConsole ()
		{
			packageEvents = PackageManagementServices.PackageManagementEvents;

			outWriter.TextWritten += TextWritten;
			logWriter.TextWritten += TextWritten;
			errorWriter.TextWritten += TextWritten;
		}

		public override TextWriter Error {
			get { return errorWriter; }
		}

		public override TextReader In {
			get { return reader; }
		}

		public override TextWriter Log {
			get { return logWriter; }
		}

		public override TextWriter Out {
			get { return outWriter; }
		}

		public override void Dispose ()
		{
			base.Dispose ();

			if (outWriter != null) {
				outWriter.TextWritten -= TextWritten;
				logWriter.TextWritten -= TextWritten;
				errorWriter.TextWritten -= TextWritten;

				outWriter.Dispose ();
				logWriter.Dispose ();
				errorWriter.Dispose ();

				outWriter = null;
				logWriter = null;
				errorWriter = null;
			}
		}

		void TextWritten (string writtenText)
		{
			packageEvents.OnPackageOperationMessageLogged (MessageLevel.Info, writtenText);
		}
	}
}
