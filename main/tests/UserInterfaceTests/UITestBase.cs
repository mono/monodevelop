//
// UserInterfaceTest.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
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

using System.IO;
using NUnit.Framework;
using MonoDevelop.Components.AutoTest;
using System;
using System.Linq;

namespace UserInterfaceTests
{
	[TestFixture]
	public abstract class UITestBase
	{
		string projectScreenshotFolder;
		string currentWorkingDirectory;
		string ideLogPath;
		int testScreenshotIndex;

		public string ScreenshotsPath { get; private set; }

		public string CurrentXSIdeLog { get; private set; }

		public AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public string MonoDevelopBinPath { get; set; }

		protected UITestBase () {}

		protected UITestBase (string mdBinPath)
		{
			MonoDevelopBinPath = mdBinPath;
			currentWorkingDirectory = Directory.GetCurrentDirectory ();
		}

		[TestFixtureSetUp]
		public virtual void FixtureSetup ()
		{
			InitializeScreenShotPath ();
			ideLogPath = Path.Combine (currentWorkingDirectory, "Idelogs");
			if (!Directory.Exists (ideLogPath))
				Directory.CreateDirectory (ideLogPath);
		}

		[SetUp]
		public virtual void SetUp ()
		{
			CurrentXSIdeLog = Path.Combine (ideLogPath,string.Format ("{0}.Ide.log", TestContext.CurrentContext.Test.FullName) );
			Environment.SetEnvironmentVariable ("MONODEVELOP_LOG_FILE", CurrentXSIdeLog);
			Environment.SetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL", "All");

			TestService.StartSession (MonoDevelopBinPath);
			TestService.Session.DebugObject = new UITestDebug ();

			ScreenshotForTestSetup (TestContext.CurrentContext.Test.Name);
		}

		[TearDown]
		public virtual void Teardown ()
		{
			OnCleanUp ();
			TestService.EndSession ();

			if (TestContext.CurrentContext.Result.Status == TestStatus.Passed) {
				if (Directory.Exists (projectScreenshotFolder))
					Directory.Delete (projectScreenshotFolder, true);
				File.Delete (CurrentXSIdeLog);
			}
		}

		void InitializeScreenShotPath ()
		{
			ScreenshotsPath = Path.Combine (currentWorkingDirectory, "Screenshots", GetType ().Name);
			if (Directory.Exists (ScreenshotsPath)) {
				var lastAccess = Directory.GetLastAccessTime (ScreenshotsPath).ToString ("u").Replace (' ', '-').Replace (':', '-');
				var newLocation = string.Format ("{0}-{1}", ScreenshotsPath, lastAccess);
				Directory.Move (ScreenshotsPath, newLocation);
			}

			Directory.CreateDirectory (ScreenshotsPath);
		}

		void ScreenshotForTestSetup (string testName)
		{
			testScreenshotIndex = 1;
			projectScreenshotFolder = Path.Combine (ScreenshotsPath, testName);
			if (Directory.Exists (projectScreenshotFolder))
				Directory.Delete (projectScreenshotFolder, true);
			Directory.CreateDirectory (projectScreenshotFolder);
		}

		protected void TakeScreenShot (string stepName)
		{
			stepName = string.Format ("{0:D3}-{1}", testScreenshotIndex++, stepName);
			var screenshotPath = Path.Combine (projectScreenshotFolder, stepName) + ".png";
			Session.TakeScreenshot (screenshotPath);
		}

		protected virtual void OnCleanUp ()
		{
			var actualSolutionDirectory = GetSolutionDirectory ();
			Ide.CloseAll ();
			try {
				if (Directory.Exists (actualSolutionDirectory))
					Directory.Delete (actualSolutionDirectory, true);
			} catch (IOException) { }
		}

		protected string GetSolutionDirectory ()
		{
			return Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.RootFolder.BaseDirectory").ToString ();
		}
	}
}
