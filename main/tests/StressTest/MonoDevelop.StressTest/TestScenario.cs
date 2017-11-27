//
// TestScenario.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using UserInterfaceTests;

namespace MonoDevelop.StressTest
{
	public class TestScenario : ITestScenario
	{
		List<FilePath> filesToOpen;

		public TestScenario (FilePath solutionFileName)
			: this (solutionFileName, Enumerable.Empty<string> ())
		{
			SolutionFileName = solutionFileName;
		}

		public TestScenario (FilePath solutionFileName, IEnumerable<string> filesToOpen)
		{
			SolutionFileName = solutionFileName;

			this.filesToOpen = filesToOpen
				.Select (fileToOpen => solutionFileName.ParentDirectory.Combine (fileToOpen))
				.ToList ();
		}

		public FilePath SolutionFileName { get; set; }

		public IEnumerable<FilePath> FilesToOpen {
			get { return filesToOpen; }
		}

		public IEnumerable<string> TextToEnter { get; set; } = Enumerable.Empty<string> ();

		bool firstRun = true;

		public void Run ()
		{
			if (firstRun) {
				Workbench.OpenWorkspace (SolutionFileName);
				firstRun = false;
			}

			// Open files.
			WorkbenchExtensions.CloseAllOpenFiles ();
			WorkbenchExtensions.OpenFiles (FilesToOpen);
			UserInterfaceTests.Ide.WaitForIdeIdle ();

			// Make some changes to the active file and then remove the changes.
			TextEditor.MoveCaretToDocumentStart ();
			UserInterfaceTests.Ide.WaitForIdeIdle ();

			if (TextToEnter.Any ()) {
				TextEditor.EnterText (TextToEnter);
				UserInterfaceTests.Ide.WaitForIdeIdle ();

				WorkbenchExtensions.SaveFile ();
				UserInterfaceTests.Ide.WaitForIdeIdle ();

				TextEditor.DeleteToLineStart ();
				UserInterfaceTests.Ide.WaitForIdeIdle ();

				WorkbenchExtensions.SaveFile ();
				UserInterfaceTests.Ide.WaitForIdeIdle ();
			}

			// Rebuild.
			WorkbenchExtensions.RebuildSolution ();

			// Debug.
			WorkbenchExtensions.Debug ();

			// Force focus of text editor. Otherwise the text editor is not
			// properly focused after the debug session finishes - the caret
			// does not blink and TextEditorCommands no longer work.
			// AutoTestClientSession.EnterText works without needing to
			// focus the workbench.
			WorkbenchExtensions.GrabDesktopFocus ();
		}
	}
}
