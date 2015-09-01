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
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace UserInterfaceTests
{
	[TestFixture]
	public abstract class UITestBase
	{
		string currentWorkingDirectory;
		string testResultFolder;
		string currentTestResultFolder;
		string currentTestResultScreenshotFolder;

		int testScreenshotIndex;

		protected readonly List<string> FoldersToClean = new List<string> ();

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
			testResultFolder = Path.Combine (currentWorkingDirectory, "TestResults");
		}

		[SetUp]
		public virtual void SetUp ()
		{
			SetupTestResultFolder ();
			SetupScreenshotsFolder ();
			SetupIdeLogFolder ();

			var mdProfile = Util.CreateTmpDir ();
			TestService.StartSession (MonoDevelopBinPath, mdProfile);
			TestService.Session.DebugObject = new UITestDebug ();

			FoldersToClean.Add (mdProfile);

			Session.WaitForElement (IdeQuery.DefaultWorkbench);
			TakeScreenShot ("Application-Started");
			CloseIfXamarinUpdateOpen ();
			TakeScreenShot ("Application-Ready");
		}

		[TearDown]
		public virtual void Teardown ()
		{
			try {
				if (TestContext.CurrentContext.Result.Status != TestStatus.Passed && Session.Query (IdeQuery.XamarinUpdate).Any ()) {
					Assert.Inconclusive ("Xamarin Update is blocking the application focus");
				}
				ValidateIdeLogMessages ();
			} finally {
				var testStatus = TestContext.CurrentContext.Result.Status;
				if (testStatus != TestStatus.Passed) {
					try {
						TakeScreenShot (string.Format ("{0}-Test-Failed", TestContext.CurrentContext.Test.Name));
					} catch (Exception e) {
						Session.DebugObject.Debug ("Final Screenshot failed");
					}
				}

				File.WriteAllText (Path.Combine (currentTestResultFolder, "MemoryUsage.json"),
					JsonConvert.SerializeObject (Session.MemoryStats, Formatting.Indented));

				Ide.CloseAll ();
				TestService.EndSession ();

				OnCleanUp ();
				if (testStatus == TestStatus.Passed) {
					if (Directory.Exists (currentTestResultScreenshotFolder))
						Directory.Delete (currentTestResultScreenshotFolder, true);
				}
			}
		}

		static void ValidateIdeLogMessages ()
		{
			LogMessageValidator.Validate (Environment.GetEnvironmentVariable ("MONODEVELOP_LOG_FILE"));
		}

		protected void CloseIfXamarinUpdateOpen ()
		{
			try {
				Session.WaitForElement (IdeQuery.XamarinUpdate, 10 * 1000);
				TakeScreenShot ("Xamarin-Update-Opened");
				Session.ClickElement (c => IdeQuery.XamarinUpdate (c).Children ().Button ().Text ("Close"));
			}
			catch (TimeoutException) {
				TestService.Session.DebugObject.Debug ("Xamarin Update did not open");
			}
		}

		void SetupTestResultFolder ()
		{
			currentTestResultFolder = Path.Combine (testResultFolder, TestContext.CurrentContext.Test.FullName);
			if (Directory.Exists (currentTestResultFolder))
				Directory.Delete (currentTestResultFolder, true);
			Directory.CreateDirectory (currentTestResultFolder);
		}

		void SetupScreenshotsFolder ()
		{
			testScreenshotIndex = 1;
			currentTestResultScreenshotFolder = Path.Combine (currentTestResultFolder, "Screenshots");
			if (Directory.Exists (currentTestResultScreenshotFolder))
				Directory.Delete (currentTestResultScreenshotFolder, true);
			Directory.CreateDirectory (currentTestResultScreenshotFolder);
		}

		void SetupIdeLogFolder ()
		{
			var currentXSIdeLog = Path.Combine (currentTestResultFolder, string.Format ("{0}.Ide.log", TestContext.CurrentContext.Test.FullName));
			Environment.SetEnvironmentVariable ("MONODEVELOP_LOG_FILE", currentXSIdeLog);
			Environment.SetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL", "UpToInfo");
		}

		protected void TakeScreenShot (string stepName)
		{
			stepName = string.Format ("{0:D3}-{1}", testScreenshotIndex++, stepName);
			var screenshotPath = Path.Combine (currentTestResultScreenshotFolder, stepName) + ".png";
			Session.TakeScreenshot (screenshotPath);
		}

		protected virtual void OnCleanUp ()
		{
			foreach (var folder in FoldersToClean) {
				try {
					if (folder != null && Directory.Exists (folder))
						Directory.Delete (folder, true);
				} catch (IOException e) {
					TestService.Session.DebugObject.Debug ("Cleanup failed\n" +e);
				}
			}
		}

		protected string GetSolutionDirectory ()
		{
			try {
				var dirObj = Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.RootFolder.BaseDirectory");
			return dirObj != null ? dirObj.ToString () : null;
			} catch (Exception) {
				TestService.Session.DebugObject.Debug ("GetSolutionDirectory () returns null");
				return null;
			}
		}
	}
}
