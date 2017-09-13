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
using MonoDevelop.Core.Logging;
using MonoDevelop.Core;

namespace UserInterfaceTests
{
	[TestFixture]
	public abstract class UITestBase
	{
		string currentWorkingDirectory;
		string testResultFolder;
		string currentTestResultFolder;
		string currentTestResultScreenshotFolder;

		int testScreenshotIndex, reproStepIndex;

		protected readonly List<string> FoldersToClean = new List<string> ();
		protected FileLogger Logger;

		public AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public string MonoDevelopBinPath { get; set; }

		protected UITestBase () {}

		protected UITestBase (string mdBinPath)
		{
			var installedXS = Environment.GetEnvironmentVariable ("USE_INSTALLED_XS");
			if (!string.IsNullOrWhiteSpace(installedXS)) {
				if (Platform.IsMac)
					installedXS = Path.Combine(installedXS, "Contents/MacOS/VisualStudio");
				else if (Platform.IsWindows)
					installedXS = Path.Combine(installedXS, @"bin\VisualStudio.exe");
			}

			if (File.Exists (installedXS)) {
				MonoDevelopBinPath = installedXS;
				Console.WriteLine ("[UITEST] Using installed Visual Studio from this location: " + installedXS);
			}
			else {
				Console.WriteLine ("[UITEST] Installed Visual Studio not found. Falling back to default behavior.");
				MonoDevelopBinPath = mdBinPath;
			}

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
			SetupTestLogger ();
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
				bool isInconclusive = false;
				var testStatus = TestContext.CurrentContext.Result.Status;
				if (testStatus != TestStatus.Passed) {
					try {
						var updateOpened = Session.Query (IdeQuery.XamarinUpdate);
						if (updateOpened != null && updateOpened.Any ())
							isInconclusive = true;
						TakeScreenShot (string.Format ("{0}-Test-Failed", TestContext.CurrentContext.Test.Name));
					} catch (Exception e) {
						Session.DebugObject.Debug ("Final Screenshot failed");
						Session.DebugObject.Debug (e.ToString ());
					}
				}

				File.WriteAllText (Path.Combine (currentTestResultFolder, "MemoryUsage.json"),
				                   JsonConvert.SerializeObject (Session.MemoryStats, Formatting.Indented));

				Ide.CloseAll ();
				TestService.EndSession ();

				ValidateIdeLogMessages ();

				OnCleanUp ();
				if (testStatus == TestStatus.Passed) {
					if (Directory.Exists (currentTestResultScreenshotFolder))
						Directory.Delete (currentTestResultScreenshotFolder, true);
				}

				if (isInconclusive)
					Assert.Inconclusive ("Xamarin Update is blocking the application focus");
			} finally {
				LoggingService.RemoveLogger (Logger.Name);
				Logger.Dispose ();
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
				TestService.Session.DebugObject.Debug ("Visual Studio Update did not open");
			}
		}

		void SetupTestResultFolder ()
		{
			currentTestResultFolder = Path.Combine (testResultFolder, TestContext.CurrentContext.Test.Name);
			if (Directory.Exists (currentTestResultFolder))
				Directory.Delete (currentTestResultFolder, true);
			Directory.CreateDirectory (currentTestResultFolder);
		}

		void SetupTestLogger ()
		{
			reproStepIndex = 0;
			var currentTestLog = Path.Combine (currentTestResultFolder, string.Format ("{0}.Test.log.txt", TestContext.CurrentContext.Test.Name.ToPathSafeString ()));
			Logger = new FileLogger (currentTestLog) {
				Name = TestContext.CurrentContext.Test.Name,
				EnabledLevel = EnabledLoggingLevel.All,
			};
			LoggingService.AddLogger (Logger);
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
			var currentXSIdeLog = Path.Combine (currentTestResultFolder, string.Format ("{0}.Ide.log", TestContext.CurrentContext.Test.Name.ToPathSafeString ()));
			Environment.SetEnvironmentVariable ("MONODEVELOP_LOG_FILE", currentXSIdeLog);
			Environment.SetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL", "UpToInfo");
		}

		public void TakeScreenShot (string stepName)
		{
			stepName = string.Format ("{0:D3}-{1}", testScreenshotIndex++, stepName).ToPathSafeString ();
			var screenshotPath = Path.Combine (currentTestResultScreenshotFolder, stepName) + ".png";
			Session.TakeScreenshot (screenshotPath);
		}

		public void ReproStep (string stepDescription, params object[] info)
		{
			reproStepIndex++;
			stepDescription = string.Format ("@Repro-Step-{0:D2}: {1}", reproStepIndex, stepDescription);
			LoggingService.LogInfo (stepDescription);
			foreach (var obj in info) {
				if (obj != null)
					LoggingService.LogInfo (string.Format("@Repro-Info-{0:D2}: {1}", reproStepIndex, obj.ToString ()));
			}
		}

		public void ReproFailedStep (string expected, string actual, params object [] info)
		{
			ReproStep (string.Format ("Expected: {0}\nActual: {1}", expected, actual), info);
		}

		protected virtual void OnCleanUp ()
		{
			foreach (var folder in FoldersToClean) {
				try {
					if (folder != null && Directory.Exists (folder))
						Directory.Delete (folder, true);
				} catch (IOException e) {
					TestService.Session.DebugObject.Debug ("Cleanup failed\n" +e);
				} catch (UnauthorizedAccessException e) {
					TestService.Session.DebugObject.Debug (string.Format ("Unable to clean directory: {0}\n", folder) + e);
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
