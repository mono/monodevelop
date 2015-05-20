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

namespace UserInterfaceTests
{
	[TestFixture]
	public abstract class UITestBase
	{
		string projectScreenshotFolder;

		public string ScreenshotsPath { get; private set; }

		public AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public string MonoDevelopBinPath { get; set; }

		protected UITestBase () {}

		protected UITestBase (string mdBinPath, string screenshotsFolder)
		{
			MonoDevelopBinPath = mdBinPath;
			InitializeScreenShotPath (screenshotsFolder);
		}

		[SetUp]
		public virtual void SetUp ()
		{
			Util.ClearTmpDir ();

			TestService.StartSession (MonoDevelopBinPath);
			TestService.Session.DebugObject = new UITestDebug ();
		}

		[TearDown]
		public virtual void Teardown ()
		{
			OnCleanUp ();
			TestService.EndSession ();
		}

		void InitializeScreenShotPath (string folderName)
		{
			var pictureFolderName = Environment.GetFolderPath (Environment.SpecialFolder.MyPictures);
			folderName = folderName ?? DateTime.Now.ToLongDateString ().Replace (",", "").Replace (' ', '-');
			ScreenshotsPath = Path.Combine (pictureFolderName, "XamarinStudioUITests", folderName, GetType ().Name);
			if (Directory.Exists (ScreenshotsPath))
				Directory.Delete (ScreenshotsPath, true);
			Directory.CreateDirectory (ScreenshotsPath);
		}

		protected void ScreenshotForTestSetup (string testName)
		{
			projectScreenshotFolder = Path.Combine (ScreenshotsPath, testName);
			if (Directory.Exists (projectScreenshotFolder))
				Directory.Delete (projectScreenshotFolder, true);
			Directory.CreateDirectory (projectScreenshotFolder);
		}

		protected void TakeScreenShot (string stepName)
		{
			if (string.IsNullOrEmpty (projectScreenshotFolder))
				throw new InvalidOperationException ("You need to initialize Screenshot functionality by calling 'ScreenshotForTestSetup (string testName)' first");
			
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
