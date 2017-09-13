// TestBase.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using NUnit.Framework;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace UnitTests
{
	public class TestBase
	{
		static bool firstRun = true;
		
		static TestBase ()
		{
			var topPath = LocateTopLevel ();
			LoggingService.AddLogger (new MonoDevelop.Core.Logging.FileLogger (Path.Combine (topPath, "TestResult_LoggingService.log")));
		}

		static string LocateTopLevel ()
		{
			var cwd = typeof (TestBase).Assembly.Location;
			while (!string.IsNullOrEmpty (cwd) && !File.Exists (Path.Combine (cwd, "top_level_monodevelop")))
				cwd = Path.GetDirectoryName (cwd);
			return cwd;
		}

		[TestFixtureSetUp]
		public void Simulate ()
		{
			if (firstRun) {
				string rootDir = Path.Combine (Util.TestsRootDir, "config");
				try {
					firstRun = false;
					InternalSetup (rootDir);
				} catch (Exception) {
					// if we encounter an error, try to re create the configuration directory
					// (This takes much time, therfore it's only done when initialization fails)
					try {
						if (Directory.Exists (rootDir))
							Directory.Delete (rootDir, true);
						InternalSetup (rootDir);
					} catch (Exception) {
					}
				}
			}
		}

		protected virtual void InternalSetup (string rootDir)
		{
			Util.ClearTmpDir ();
			Environment.SetEnvironmentVariable ("MONO_ADDINS_REGISTRY", rootDir);
			Environment.SetEnvironmentVariable ("XDG_CONFIG_HOME", rootDir);
			global::MonoDevelop.Projects.Services.ProjectService.DefaultTargetFramework
				= Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_0);
		}

		
		[TestFixtureTearDown]
		public virtual void TearDown ()
		{
			//Util.ClearTmpDir ();
		}
		
		static int pcount = 0;
		
		public static string GetTempFile (string extension)
		{
			return Path.Combine (Path.GetTempPath (), "test-file-" + (pcount++) + extension);
		}
	}
}
