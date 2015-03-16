//
// NewProjectController.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 
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
using NUnit.Framework;
using System.Threading;

namespace UserInterfaceTests
{
	public class NewProjectController
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public void Open (int delay = 2000)
		{
			Session.ExecuteCommand (FileCommands.NewProject);
			Session.WaitForWindow ("MonoDevelop.Ide.Projects.GtkNewProjectDialogBackend");
			Thread.Sleep (delay);
		}

		public bool SelectTemplateType (string type, string category)
		{
			return Session.SelectTreeviewItem ("templateCategoriesTreeView", type, category);
		}

		public bool SelectTemplate (string templateName)
		{
			return Session.SelectTreeviewItem ("templatesTreeView", templateName, null);
		}

		public bool Next ()
		{
			return Session.SelectWidget ("nextButton") && (bool)Session.Invoke ("Activate");
		}

		public bool Previous ()
		{
			return Session.SelectWidget ("previousButton") && (bool)Session.Invoke ("Activate");
		}

		public bool SetProjectName (string projectName)
		{
			return TypeTextInGtkEntry ("projectNameTextBox", projectName);
		}

		public bool SetSolutionName (string solutionName)
		{
			return TypeTextInGtkEntry ("solutionNameTextBox", solutionName);
		}

		public bool SetSolutionLocation (string solutionLocation)
		{
			return TypeTextInGtkEntry ("locationTextBox", solutionLocation);
		}

		public bool CreateProjectInSolutionDirectory (bool projectInSolution)
		{
			return SetCheckBox ("createProjectWithinSolutionDirectoryCheckBox", projectInSolution);
		}

		public bool UseGit (bool useGit, bool useGitIgnore = true)
		{
			var gitSelectionSuccess = SetCheckBox ("useGitCheckBox", useGit);
			if (gitSelectionSuccess && useGit)
				return gitSelectionSuccess && SetCheckBox ("createGitIgnoreFileCheckBox", useGitIgnore);
			return gitSelectionSuccess;
		}

		bool SetCheckBox (string checkBoxName, bool active)
		{
			var widgetSelected = Session.SelectWidget (checkBoxName);
			if (widgetSelected)
				Session.SetPropertyValue ("Active", active);
			return widgetSelected;
		}

		bool TypeTextInGtkEntry (string widgetName, string text)
		{
			var widgetSelected = Session.SelectWidget (widgetName);
			if (widgetSelected)
				Session.TypeText (text);
			return widgetSelected;
		}
	}
}

