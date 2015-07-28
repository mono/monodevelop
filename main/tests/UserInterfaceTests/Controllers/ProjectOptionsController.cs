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
	public class ProjectOptionsController
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		Action<string> takeScreenshot;

		public ProjectOptionsController (Action<string> takeScreenshot = null)
		{
			this.takeScreenshot = takeScreenshot ?? delegate { };
		}

		public void OpenProjectOptions ()
		{
			Session.Query (IdeQuery.TextArea);
			Session.ExecuteCommand (ProjectCommands.ProjectOptions);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Projects.ProjectOptionsDialog"));
			takeScreenshot ("Opened-ProjectOptionsDialog");
		}

		public void OpenSolutionOptions ()
		{
			throw new NotImplementedException ();
		}

		public void SelectPane (string name)
		{
			Session.SelectElement (c => c.Window ().Marked ("MonoDevelop.Ide.Projects.ProjectOptionsDialog").Children ().Marked (
				"__gtksharp_16_MonoDevelop_Components_HeaderBox").Children ().TreeView ().Model ().Children ().Property ("Label", name));
		}

		public void ClickOK ()
		{
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.Ide.Projects.ProjectOptionsDialog").Children ().Button ().Text ("OK"));
		}

		public void ClickCancel ()
		{
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.Ide.Projects.ProjectOptionsDialog").Children ().Button ().Text ("Cancel"));
		}
	}
}

