//
// SvnUtilsTest.cs
//
// Author:
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
using System.Diagnostics;
using NUnit.Framework;
using MonoDevelop.Core;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Subversion
{
	[TestFixture]
	public class SvnUtilsTest
	{
		static readonly string url = "svn://localhost";
		static readonly string daemon = "svnserve";
		static readonly string arguments = "-dr ";
		static readonly string port = "3690";

		[Test]
		public void TestThis ()
		{
			Process process = new Process ();
			ProcessStartInfo info = new ProcessStartInfo ();
			FilePath path = new FilePath (FileService.CreateTempDirectory ());
			info.FileName = daemon;
			info.Arguments = arguments + path;
			process.StartInfo = info;
			process.Start ();
/*			VersionControlService service = new VersionControlService ();
			Assert.True (service != null);

			SubversionRepository repo = new SubversionRepository (null,
			                                                      url + ":" + port,
			                                                      null);
			repo.Checkout (path, true, new MessageDialogProgressMonitor ());*/
			System.IO.Directory.Delete (path);
			//			Assert.True (System.IO.Directory.Exists (path + "/.svn"));
			//			repo.Update (repo.RootPath, true, new MonoDevelop.Ide.ProgressMonitoring.BaseProgressMonitor());
		}
	}
}

