//
// ProjectOptionsController.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.UserInterfaceTesting.Controllers
{
	public class ProjectOptionsController : OptionsController
	{
		readonly static Func<AppQuery, AppQuery> windowQuery = c => c.Window ().Marked ("MonoDevelop.Ide.Projects.ProjectOptionsDialog");

		readonly string solutionName;
		readonly string projectName;

		public ProjectOptionsController (string solutionName, string projectName, UITestBase testContext = null) : base (windowQuery,testContext)
		{
			this.solutionName = solutionName;
			this.projectName = projectName;
		}

		public ProjectOptionsController (UITestBase testContext = null) : base (windowQuery, testContext) { }

		public void OpenProjectOptions ()
		{
			ReproStep (string.Format ("In Solution Explorer, right click '{0}' and select 'Options'", projectName));
			SolutionExplorerController.SelectProject (solutionName, projectName);

			Session.Query (IdeQuery.TextArea);
			Session.ExecuteCommand (ProjectCommands.ProjectOptions);
			Session.WaitForElement (windowQuery);
			TakeScreenshot ("Opened-ProjectOptionsDialog");
		}
	}

	public class PreferencesController : OptionsController
	{
		readonly static Func<AppQuery, AppQuery> windowQuery = c => c.Window ().Marked ("Preferences");

		public PreferencesController (Action<string> takeScreenshot = null) : base (windowQuery, takeScreenshot) {}

		public void Open ()
		{
			Session.ExecuteCommand (EditCommands.MonodevelopPreferences);
			Session.WaitForElement (windowQuery);
			TakeScreenshot ("Opened-Preferences-Window");
		}

		public static void SetAuthorInformation (string name = null, string email = null , string copyright = null,
			string company = null, string trademark = null, Action<string> takeScreenshot = null)
		{
			takeScreenshot = takeScreenshot ?? new Action<string> (delegate {});

			if (name == null && email == null && copyright == null && company == null && trademark == null)
				throw new ArgumentNullException ("Atleast one of these arguments need to be not null: name, email, copyright, company, trademark");

			var prefs = new PreferencesController ();
			prefs.Open ();
			prefs.SelectPane ("Author Information");
			prefs.SetEntry ("nameEntry", name, "Name", takeScreenshot);
			prefs.SetEntry ("emailEntry", email, "Email", takeScreenshot);
			prefs.SetEntry ("copyrightEntry", copyright, "Copyright", takeScreenshot);
			prefs.SetEntry ("companyEntry", company, "Company", takeScreenshot);
			prefs.SetEntry ("trademarkEntry", trademark, "Trademark", takeScreenshot);
			prefs.ClickOK ();
		}
	}

	public abstract class OptionsController
	{
		protected static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		protected Action<string> TakeScreenshot;
		readonly UITestBase testContext;
		readonly Func<AppQuery, AppQuery> windowQuery;

		protected OptionsController (Func<AppQuery, AppQuery> windowQuery, UITestBase testContext) : this (windowQuery, testContext.TakeScreenShot)
		{
			this.testContext = testContext;
		}

		protected OptionsController (Func<AppQuery, AppQuery> windowQuery, Action<string> takeScreenshot = null)
		{
			this.windowQuery = windowQuery;
			TakeScreenshot = Util.GetNonNullAction (takeScreenshot);
		}

		protected void ReproStep (string stepDescription, params object[] info)
		{
			testContext.ReproStep (stepDescription, info);
		}

		public void SelectPane (string name)
		{
			ReproStep (string.Format ("Select pane: '{0}'", name));
			string.Format ("Selected Pane :{0}", name).PrintData ();
			Session.SelectElement (c => windowQuery (c).Children ().Marked (
				"MonoDevelop.Components.HeaderBox").Children ().TreeView ().Model ().Children ().Property ("Label", name));
		}

		protected void SetEntry (string entryName, string entryValue, string stepName,  Action<string> takeScreenshot)
		{
			if (entryValue != null) {
				Session.EnterText (c => c.Marked (entryName), entryValue);
				Session.WaitForElement (c => c.Marked (entryName).Text (entryValue));
				takeScreenshot (string.Format("{0}-Entry-Set", stepName));
			}
		}

		public void ClickOK ()
		{
			ReproStep ("Click OK");
			Session.ClickElement (c => windowQuery (c).Children ().Button ().Text ("OK"));
		}

		public void ClickCancel ()
		{
			ReproStep ("Click Cancel");
			Session.ClickElement (c => windowQuery (c).Children ().Button ().Text ("Cancel"));
		}
	}
}

