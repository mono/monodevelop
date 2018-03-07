//
// UserPreferencesTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide
{
	public class UserPreferencesTests: IdeTestBase
	{
		[Test]
		public async Task LoadUserPreferences()
		{
			string wsFile = Util.GetSampleProject("workspace-userprefs", "workspace.mdw");
			using (var wsi = await Services.ProjectService.ReadWorkspaceItem (new ProgressMonitor (), wsFile)) {
				Assert.IsInstanceOf<Workspace> (wsi);
				var ws = (Workspace)wsi;

				var userData = ws.UserProperties.GetValue<WorkspaceUserData> ("MonoDevelop.Ide.Workspace");

				Assert.IsFalse (ws.UserProperties.IsEmpty);
				Assert.IsNotNull (userData);
				Assert.AreEqual ("Release", userData.ActiveConfiguration);
			}
		}

		[Test]
		public async Task UserProperties ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (DotNetProject)sol.Items [0];
				sol.UserProperties.SetValue ("SolProp", "foo");
				p.UserProperties.SetValue ("ProjectProp", "bar");
				await sol.SaveUserProperties ();
				sol.Dispose ();
			}

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (DotNetProject)sol.Items [0];
				Assert.AreEqual ("foo", sol.UserProperties.GetValue<string> ("SolProp"));
				Assert.AreEqual ("bar", p.UserProperties.GetValue<string> ("ProjectProp"));
			}
		}

		[Test]
		public void UserPreferencesFileName ()
		{
			FilePath directory = Util.CreateTmpDir ("MySolution");
			var fileName = directory.Combine ("MySolution.sln");
			using (var solution = new Solution ()) {
				solution.FileName = fileName;
				var paths = new [] { ".vs", "MySolution", "xs", "UserPrefs.xml" };
				string expectedFileName = solution.BaseDirectory.Combine (paths);

				string userPreferencesFileName = solution.GetPreferencesFileName ();

				Assert.AreEqual (expectedFileName, userPreferencesFileName);
			}
		}

		[Test]
		public async Task UserPreferencesAreMigratedToNewLocation ()
		{
			FilePath directory = Util.CreateTmpDir ("MigrateUserPreferences");
			var fileName = directory.Combine ("MigrateUserPreferences.sln");
			string userPreferencesOldLocationFileName;

			using (var solution = new Solution ()) {
				solution.FileName = fileName;
				solution.UserProperties.SetValue ("Test", "Test-Value");

				// Create a user prefs file.
				await solution.SaveAsync (Util.GetMonitor ());

				Assert.IsTrue (File.Exists (solution.GetPreferencesFileName ()));

				userPreferencesOldLocationFileName = solution.FileName.ChangeExtension (".userprefs");
				Assert.IsFalse (File.Exists (userPreferencesOldLocationFileName));

				// Create a legacy user prefs file.
				FilePath preferencesFileName = solution.GetPreferencesFileName ();
				File.Move (preferencesFileName, userPreferencesOldLocationFileName);

				// Ensure migration handles the missing directory for the new prefs file.
				Directory.Delete (preferencesFileName.ParentDirectory);
			}

			using (var solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), fileName)) {

				Assert.AreEqual ("Test-Value", solution.UserProperties.GetValue<string> ("Test"));

				// Change user property and save user prefs.
				solution.UserProperties.SetValue ("Test", "Test-Value-Updated");
				await solution.SaveUserProperties ();

				Assert.IsFalse (File.Exists (userPreferencesOldLocationFileName));
			}

			using (var solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), fileName))
				Assert.AreEqual ("Test-Value-Updated", solution.UserProperties.GetValue<string> ("Test"));
		}
	}
}
