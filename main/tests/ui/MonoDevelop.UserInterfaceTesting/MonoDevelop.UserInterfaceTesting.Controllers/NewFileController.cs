//
// NewFileController.cs
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
	public class NewFileController
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		Action<string> takeScreenshot;

		Func<AppQuery, AppQuery> categoryViewQuery = c => c.TreeView ().Marked ("catView").Model ();
		Func<AppQuery, AppQuery> fileTypeQuery = c => c.TreeView ().Marked ("newFileTemplateTreeView").Model ("templateStore__Name");

		public NewFileController (Action<string> takeScreenshot = null)
		{
			this.takeScreenshot = Util.GetNonNullAction (takeScreenshot);
		}

		public static void Create (NewFileOptions options, UITestBase testContext = null)
		{
			options.PrintData ();
			Action<string> screenshotAction = (s) => {};
			if (testContext != null) {
				testContext.ReproStep (string.Format ("Add a new file of type '{0}' named '{1}'", 
					options.FileType, options.FileName), options);
				screenshotAction = testContext.TakeScreenShot;
			}

			var ctrl = new NewFileController (screenshotAction);
			ctrl.Open ();
			ctrl.ConfigureAddToProject (!string.IsNullOrEmpty (options.AddToProjectName), options.AddToProjectName);
			ctrl.SelectFileTypeCategory (options.FileTypeCategory, options.FileTypeCategoryRoot);
			ctrl.SelectFileType (options.FileType);
			ctrl.EnterFileName (options.FileName);
			ctrl.Done ();
		}

		public void Open ()
		{
			Session.WaitForElement (IdeQuery.DefaultWorkbench);
			Session.ExecuteCommand (FileCommands.NewFile);
			Session.WaitForElement (IdeQuery.NewFileDialog);
			takeScreenshot ("NewFileDialog-Opened");
		}

		public bool SelectFileTypeCategory (string fileTypeCategory, string fileTypeCategoryRoot = "C#")
		{
			Session.ClickElement (c => categoryViewQuery (c).Text (fileTypeCategoryRoot));
			var resultParent = Session.SelectElement (c => categoryViewQuery (c).Text (fileTypeCategoryRoot).Children ().Text (fileTypeCategory));
			var result = Session.SelectElement (c => categoryViewQuery (c).Text (fileTypeCategory)) &&
				Session.WaitForElement (c => categoryViewQuery (c).Text (fileTypeCategory).Selected ()).Length > 0;
			takeScreenshot ("FileTypeCategory-Selected");
			return resultParent || result;
		}

		public bool SelectFileType (string fileType)
		{
			var result = Session.SelectElement (c => fileTypeQuery (c).Contains (fileType)) &&
				Session.WaitForElement (c => fileTypeQuery (c).Contains (fileType).Selected ()).Length > 0;
			takeScreenshot ("FileType-Selected");
			return result;
		}

		public bool EnterFileName (string fileName)
		{
			var result = Session.EnterText (c => c.Textfield ().Marked ("nameEntry"), fileName);
			takeScreenshot ("FileName-Entered");
			return result;
		}

		public void ConfigureAddToProject (bool addToProject, string projectName = null)
		{
			Session.ToggleElement (c => c.CheckButton ().Marked ("projectAddCheckbox"), addToProject);
			if (addToProject && projectName != null)
				Session.SelectElement (c => c.Marked ("projectAddCombo").Model ().Text (projectName));
			takeScreenshot ("AddToProject-Configured");
		}

		public bool Done ()
		{
			return Session.ClickElement (c => IdeQuery.NewFileDialog (c).Children ().Button ().Marked ("okButton"));
		}
	}
}
