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

namespace UserInterfaceTests
{
	public class ProjectOptionsController : OptionsController
	{
		readonly static Func<AppQuery, AppQuery> windowQuery = c => c.Window ().Marked ("MonoDevelop.Ide.Projects.ProjectOptionsDialog");

		public ProjectOptionsController (Action<string> takeScreenshot = null) : base (windowQuery, takeScreenshot) { }

		public void OpenProjectOptions ()
		{
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
	}

	public abstract class OptionsController
	{
		protected static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		protected Action<string> TakeScreenshot;
		readonly Func<AppQuery, AppQuery> windowQuery;

		protected OptionsController (Func<AppQuery, AppQuery> windowQuery, Action<string> takeScreenshot = null)
		{
			this.windowQuery = windowQuery;
			TakeScreenshot = takeScreenshot ?? delegate { };
		}

		public void SelectPane (string name)
		{
			Session.SelectElement (c => windowQuery (c).Children ().Marked (
				"__gtksharp_16_MonoDevelop_Components_HeaderBox").Children ().TreeView ().Model ().Children ().Property ("Label", name));
		}

		public void ClickOK ()
		{
			Session.ClickElement (c => windowQuery (c).Children ().Button ().Text ("OK"));
		}

		public void ClickCancel ()
		{
			Session.ClickElement (c => windowQuery (c).Children ().Button ().Text ("Cancel"));
		}
	}
}

