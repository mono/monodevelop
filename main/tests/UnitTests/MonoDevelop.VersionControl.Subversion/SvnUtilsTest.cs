//
// SvnUtilsTest.cs
//
// Author:
//       root <${AuthorEmail}>
//
// Copyright (c) 2013 root
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
using NUnit.Framework;

namespace MonoDevelop.VersionControl.Subversion
{
	[TestFixture]
	public class SvnUtilsTest
	{
		[Test]
		public void TestThis ()
		{
			System.Diagnostics.Process process = new System.Diagnostics.Process ();
			System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo ();
			info.FileName = "svnserve";
			info.Arguments = "-d --listen-port 1234";
			process.StartInfo = info;
//			process.Start ();
			SubversionRepository repo = new SubversionRepository (new Unix.SvnClient(),
			                                                 "file:///home/therzok/work/svnrepo",
			                                                 "file:///home/therzok/work/svnrepo");
			repo.Update (repo.RootPath, true, new MonoDevelop.Ide.ProgressMonitoring.BaseProgressMonitor());
		}
	}
}

