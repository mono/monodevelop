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

namespace UserInterfaceTests
{
	[TestFixture]
	public abstract class UITestBase
	{
		string currentWorkingDirectory;
		string testResultFolder;
		string memoryUsageFolder;
		string currentTestResultFolder;

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
			memoryUsageFolder = Path.Combine (testResultFolder, "MemoryUsage");
			if (!Directory.Exists (memoryUsageFolder))
				Directory.CreateDirectory (memoryUsageFolder);
		}

		[SetUp]
		public virtual void SetUp ()
		{
			SetupTestResultFolder (TestContext.CurrentContext.Test.FullName);
			var currentXSIdeLog = Path.Combine (currentTestResultFolder,string.Format ("{0}.Ide.log", TestContext.CurrentContext.Test.FullName) );
			Environment.SetEnvironmentVariable ("MONODEVELOP_LOG_FILE", currentXSIdeLog);
			Environment.SetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL", "All");

			TestService.StartSession (MonoDevelopBinPath);
			TestService.Session.DebugObject = new UITestDebug ();
		}

		[TearDown]
		public virtual void Teardown ()
		{
			FoldersToClean.Add (GetSolutionDirectory ());
			File.WriteAllText (Path.Combine (memoryUsageFolder, TestContext.CurrentContext.Test.FullName),
			                   JsonConvert.SerializeObject (Session.MemoryStats, Formatting.Indented));

			Ide.CloseAll ();
			TestService.EndSession ();

			OnCleanUp ();
			if (TestContext.CurrentContext.Result.Status == TestStatus.Passed) {
				if (Directory.Exists (currentTestResultFolder))
					Directory.Delete (currentTestResultFolder, true);
			}
		}

		void SetupTestResultFolder (string testName)
		{
			testScreenshotIndex = 1;
			currentTestResultFolder = Path.Combine (testResultFolder, testName);
			if (Directory.Exists (currentTestResultFolder))
				Directory.Delete (currentTestResultFolder, true);
			Directory.CreateDirectory (currentTestResultFolder);
		}

		protected void TakeScreenShot (string stepName)
		{
			stepName = string.Format ("{0:D3}-{1}", testScreenshotIndex++, stepName);
			var screenshotPath = Path.Combine (currentTestResultFolder, stepName) + ".png";
			Session.TakeScreenshot (screenshotPath);
		}

		protected virtual void OnCleanUp ()
		{
			foreach (var folder in FoldersToClean) {
				try {
					if (folder != null && Directory.Exists (folder))
						Directory.Delete (folder, true);
				} catch (IOException e) {
					Console.WriteLine ("Cleanup failed\n" +e.ToString ());
				}
			}
		}

		protected string GetSolutionDirectory ()
		{
			return Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.RootFolder.BaseDirectory").ToString ();
		}
	}
}
